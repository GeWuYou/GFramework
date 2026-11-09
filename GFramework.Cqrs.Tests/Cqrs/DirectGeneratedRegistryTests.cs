// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>Exercises compiled generated-style delegates and explicit instance registration through real scopes.</summary>
[TestFixture]
public sealed class DirectGeneratedRegistryTests
{
    private static readonly int[] ExpectedStream = [2, 3];

    /// <summary>A registry with no parameterless constructor works without assembly discovery or runtime activation.</summary>
    [Test]
    public async Task DirectRegistryIsIdempotentAndNeverCapturesScopedHandlers()
    {
        var services = new ServiceCollection();
        services.AddScopedCqrsRuntime();
        services.AddScoped<ScopeProbe>();
        var registry = new TypedRegistry("consumer");
        services.AddCqrsHandlers(registry).AddCqrsHandlers(new TypedRegistry("duplicate"));
        Assert.That(registry.Registrations, Is.EqualTo(1));
        Assert.That(services.Count(item => item.ServiceType == typeof(IRequestHandler<ReadScope, int>)), Is.EqualTo(1));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var providerLifetime = provider.ConfigureAwait(false);
        ScopeProbe? previous = null;
        for (var index = 0; index < 2; index++)
        {
            var scope = provider.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var runtime = scope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
                var probe = scope.ServiceProvider.GetRequiredService<ScopeProbe>();
                Assert.That(await runtime.SendAsync(new ReadScope()), Is.EqualTo(1));
                var values = new List<int>();
                await foreach (var value in runtime.CreateStream(new ReadStream()).ConfigureAwait(false))
                    values.Add(value);
                Assert.That(values, Is.EqualTo(ExpectedStream));
                Assert.That(probe.Disposed, Is.False);
                if (previous is not null)
                {
                    Assert.That(probe, Is.Not.SameAs(previous));
                    Assert.That(previous.Disposed, Is.True);
                }
                previous = probe;
            }
        }
        Assert.That(previous!.Disposed, Is.True);
    }

    /// <summary>Both registration routes share identity while assembly registration still supplies fallback handlers.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task MixedRegistrationPublishesEachHandlerOnce(bool assemblyFirst)
    {
        var assembly = new Mock<Assembly>();
        assembly.SetupGet(static item => item.FullName).Returns(typeof(FallbackHandler).Assembly.FullName);
        assembly.Setup(static item => item.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(MixedRegistry))]);
        assembly.Setup(static item => item.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), inherit: false))
            .Returns([new CqrsReflectionFallbackAttribute(typeof(FallbackHandler))]);
        var services = new ServiceCollection();
        services.AddCqrsRuntime();
        services.AddSingleton<NotificationProbe>();
        if (assemblyFirst)
            services.AddCqrsHandlersFromAssembly(assembly.Object);
        services.AddCqrsHandlers(new MixedRegistry());
        services.AddCqrsHandlersFromAssembly(assembly.Object);
        services.AddCqrsHandlers(new MixedRegistry());
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<ICqrsRuntime>()
            .PublishAsync(new ObservedNotification(), CancellationToken.None).ConfigureAwait(false);
        var probe = provider.GetRequiredService<NotificationProbe>();
        Assert.Multiple(() =>
        {
            Assert.That(probe.GeneratedCount, Is.EqualTo(1));
            Assert.That(probe.FallbackCount, Is.EqualTo(1));
        });
    }

    /// <summary>Registers one notification handler through unconditional generated-style DI calls.</summary>
    private sealed class MixedRegistry : ICqrsHandlerRegistry
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger) =>
            services.AddTransient<INotificationHandler<ObservedNotification>, GeneratedHandler>();
    }

    /// <summary>A notification whose handlers expose duplicate side effects.</summary>
    private sealed record ObservedNotification : INotification;

    /// <summary>Counts executions across the generated and explicit fallback paths.</summary>
    private sealed class NotificationProbe
    {
        /// <summary>Generated handler side effects.</summary>
        public int GeneratedCount
        {
            get; set;
        }
        /// <summary>Fallback handler side effects.</summary>
        public int FallbackCount
        {
            get; set;
        }
    }

    /// <summary>A generated notification handler.</summary>
    private sealed class GeneratedHandler(NotificationProbe probe) : INotificationHandler<ObservedNotification>
    {
        /// <inheritdoc />
        public ValueTask Handle(ObservedNotification notification, CancellationToken cancellationToken)
        {
            probe.GeneratedCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A handler reached only by targeted assembly fallback metadata.</summary>
    private sealed class FallbackHandler(NotificationProbe probe) : INotificationHandler<ObservedNotification>
    {
        /// <inheritdoc />
        public ValueTask Handle(ObservedNotification notification, CancellationToken cancellationToken)
        {
            probe.FallbackCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Generated metadata cannot capture a consumer scope inside the process-wide invocation cache.</summary>
    [Test]
    public void CompiledDescriptorsRejectCapturedApplicationState()
    {
        var captured = new ScopeProbe();
        Assert.Throws<ArgumentException>(() => CqrsRequestInvokerDescriptor.Create<int>(
            typeof(IRequestHandler<ReadScope, int>), (_, _, _) => new ValueTask<int>(captured.Next())));
        Assert.Throws<ArgumentNullException>(() => CqrsRequestInvokerDescriptor.Create<int>(
            typeof(IRequestHandler<ReadScope, int>), null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCqrsHandlers(null!));
    }

    /// <summary>A consumer-created stateless registry with an explicit construction contract.</summary>
    private sealed class TypedRegistry(string name) : ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider, IEnumeratesCqrsRequestInvokerDescriptors,
        ICqrsStreamInvokerProvider, IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptorEntry RequestEntry = new(
            typeof(ReadScope), typeof(int),
            CqrsRequestInvokerDescriptor.Create<int>(typeof(IRequestHandler<ReadScope, int>), InvokeRequest));
        private static readonly CqrsStreamInvokerDescriptorEntry StreamEntry = new(
            typeof(ReadStream), typeof(int),
            CqrsStreamInvokerDescriptor.Create<int>(typeof(IStreamRequestHandler<ReadStream, int>), InvokeStream));

        /// <summary>The number of calls to required registration.</summary>
        public int Registrations
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Registrations++;
            services.AddTransient<IRequestHandler<ReadScope, int>, ScopeHandler>();
            services.AddTransient<IStreamRequestHandler<ReadStream, int>, StreamHandler>();
        }
        /// <inheritdoc />
        public bool TryGetDescriptor(Type requestType, Type responseType, out CqrsRequestInvokerDescriptor? descriptor)
        {
            descriptor = requestType == typeof(ReadScope) && responseType == typeof(int) ? RequestEntry.Descriptor : null;
            return descriptor is not null;
        }
        /// <inheritdoc />
        bool ICqrsStreamInvokerProvider.TryGetDescriptor(Type requestType, Type responseType, out CqrsStreamInvokerDescriptor? descriptor)
        {
            descriptor = requestType == typeof(ReadStream) && responseType == typeof(int) ? StreamEntry.Descriptor : null;
            return descriptor is not null;
        }
        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors() => [RequestEntry];
        /// <inheritdoc />
        IReadOnlyList<CqrsStreamInvokerDescriptorEntry> IEnumeratesCqrsStreamInvokerDescriptors.GetDescriptors() => [StreamEntry];

        private static ValueTask<int> InvokeRequest(object handler, object request, CancellationToken token) =>
            ((IRequestHandler<ReadScope, int>)handler).Handle((ReadScope)request, token);
        private static IAsyncEnumerable<int> InvokeStream(object handler, object request, CancellationToken token) =>
            ((IStreamRequestHandler<ReadStream, int>)handler).Handle((ReadStream)request, token);
    }

    /// <summary>A request whose response exposes scope-local execution.</summary>
    private sealed record ReadScope : IRequest<int>;
    /// <summary>A streaming request whose elements expose the same scope.</summary>
    private sealed record ReadStream : IStreamRequest<int>;
    /// <summary>A stateful caller-owned scope dependency.</summary>
    private sealed class ScopeProbe : IDisposable
    {
        private int _count;
        /// <summary>Whether the owning DI scope released this dependency.</summary>
        public bool Disposed
        {
            get; private set;
        }
        /// <summary>Increments scope-local work.</summary>
        public int Next() => ++_count;
        /// <inheritdoc />
        public void Dispose() => Disposed = true;
    }
    /// <summary>A transient request handler with a scoped dependency.</summary>
    private sealed class ScopeHandler(ScopeProbe scope) : IRequestHandler<ReadScope, int>
    {
        /// <inheritdoc />
        public ValueTask<int> Handle(ReadScope request, CancellationToken cancellationToken) => new(scope.Next());
    }
    /// <summary>A transient stream handler; enumeration remains inside the caller's scope.</summary>
    private sealed class StreamHandler(ScopeProbe scope) : IStreamRequestHandler<ReadStream, int>
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<int> Handle(ReadStream request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return scope.Next();
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return scope.Next();
        }
    }
}

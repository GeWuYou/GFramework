// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>Pipeline registration supports native DI lifetimes and validates open generic mappings.</summary>
[TestFixture]
public sealed class CqrsPipelineRegistrationTests
{
    /// <summary>Closed and open request/stream behaviors honor every requested service lifetime.</summary>
    [TestCase(ServiceLifetime.Transient, false)]
    [TestCase(ServiceLifetime.Scoped, false)]
    [TestCase(ServiceLifetime.Singleton, false)]
    [TestCase(ServiceLifetime.Transient, true)]
    [TestCase(ServiceLifetime.Scoped, true)]
    [TestCase(ServiceLifetime.Singleton, true)]
    public void RegistrationHonorsLifetimes(ServiceLifetime lifetime, bool open)
    {
        var services = new ServiceCollection();
        services.AddCqrsPipelineBehavior(open ? typeof(RequestBehavior<,>) : typeof(RequestBehavior<Ping, int>), lifetime);
        services.AddCqrsStreamPipelineBehavior(open ? typeof(StreamBehavior<,>) : typeof(StreamBehavior<StreamPing, int>), lifetime);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        AssertLifetime<IPipelineBehavior<Ping, int>>(first.ServiceProvider, second.ServiceProvider, lifetime);
        AssertLifetime<IStreamPipelineBehavior<StreamPing, int>>(first.ServiceProvider, second.ServiceProvider, lifetime);
    }

    /// <summary>Scoped dependencies reach both pipeline forms and stay alive until complete stream enumeration.</summary>
    [Test]
    public async Task ScopedBehaviorsUseTheCallerScopeAndPreserveOrder()
    {
        var services = new ServiceCollection();
        services.AddScopedCqrsRuntime();
        services.AddScoped<ScopeState>();
        services.AddTransient<IRequestHandler<Ping, int>, Handler>();
        services.AddTransient<IStreamRequestHandler<StreamPing, int>, StreamHandler>();
        services.AddCqrsPipelineBehavior<Outer>(ServiceLifetime.Scoped);
        services.AddCqrsPipelineBehavior<Inner>(ServiceLifetime.Scoped);
        services.AddCqrsStreamPipelineBehavior<ScopedStream>(ServiceLifetime.Scoped);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var scope = provider.CreateAsyncScope();
        var state = scope.ServiceProvider.GetRequiredService<ScopeState>();
        var runtime = scope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
        Assert.That(await runtime.SendAsync(new Ping()), Is.EqualTo(7));
        Assert.That(state.Calls, Is.EqualTo(["outer-in", "inner-in", "handler", "inner-out", "outer-out"]));
        await foreach (var item in runtime.CreateStream(new StreamPing()))
            Assert.That(item, Is.EqualTo(7));
        Assert.That(state.Disposed, Is.False);
        await scope.DisposeAsync();
        Assert.That(state.Disposed, Is.True);
        await using var otherScope = provider.CreateAsyncScope();
        Assert.That(otherScope.ServiceProvider.GetRequiredService<ScopeState>(), Is.Not.SameAs(state));
    }

    /// <summary>Default registration is transient and invalid inputs leave no partial descriptors.</summary>
    [Test]
    public void InvalidMappingsFailBeforeRegistration()
    {
        var services = new ServiceCollection();
        services.AddCqrsPipelineBehavior<RequestBehavior<Ping, int>>();
        Assert.That(services[0].Lifetime, Is.EqualTo(ServiceLifetime.Transient));
        foreach (var type in new[] { typeof(string), typeof(IPipelineBehavior<,>), typeof(AbstractBehavior<,>), typeof(Reordered<,>) })
        {
            var invalid = new ServiceCollection();
            Assert.Throws<ArgumentException>(() => invalid.AddCqrsPipelineBehavior(type));
            Assert.That(invalid, Is.Empty);
        }
        Assert.Throws<ArgumentNullException>(() => services.AddCqrsPipelineBehavior(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddCqrsPipelineBehavior(typeof(RequestBehavior<,>), (ServiceLifetime)99));
    }

    /// <summary>Standard DI presence probing must not create an extra transient behavior on the first dispatch.</summary>
    [Test]
    public async Task TransientBehaviorActivatesOncePerDispatch()
    {
        var services = new ServiceCollection();
        services.AddCqrsRuntime();
        services.AddSingleton<ScopeState>();
        services.AddSingleton<ActivationCount>();
        services.AddTransient<IRequestHandler<Ping, int>, Handler>();
        services.AddCqrsPipelineBehavior<CountingBehavior>();
        await using var provider = services.BuildServiceProvider();
        var count = provider.GetRequiredService<ActivationCount>();
        var runtime = provider.GetRequiredService<ICqrsRuntime>();
        await runtime.SendAsync(new Ping()).ConfigureAwait(false);
        Assert.That(count.Value, Is.EqualTo(1));
        await runtime.SendAsync(new Ping()).ConfigureAwait(false);
        Assert.That(count.Value, Is.EqualTo(2));
    }

    /// <summary>Provider-local observation of transient construction.</summary>
    private sealed class ActivationCount
    {
        /// <summary>Actual constructor calls.</summary>
        public int Value
        {
            get; set;
        }
    }

    /// <summary>A default-lifetime behavior with an observable constructor.</summary>
    private sealed class CountingBehavior : IPipelineBehavior<Ping, int>
    {
        /// <summary>Records actual activation without introducing shared static state.</summary>
        public CountingBehavior(ActivationCount count) => count.Value++;
        /// <inheritdoc />
        public ValueTask<int> Handle(Ping message, MessageHandlerDelegate<Ping, int> next,
            CancellationToken cancellationToken) => next(message, cancellationToken);
    }

    /// <summary>Checks same-scope and cross-scope identity without depending on implementation internals.</summary>
    private static void AssertLifetime<T>(IServiceProvider first, IServiceProvider second, ServiceLifetime lifetime) where T : notnull
    {
        var initial = first.GetRequiredService<T>();
        Assert.That(ReferenceEquals(initial, first.GetRequiredService<T>()), Is.EqualTo(lifetime != ServiceLifetime.Transient));
        Assert.That(ReferenceEquals(initial, second.GetRequiredService<T>()), Is.EqualTo(lifetime == ServiceLifetime.Singleton));
    }

    /// <summary>A closed request fixture.</summary>
    private sealed record Ping : IRequest<int>;
    /// <summary>A closed stream fixture.</summary>
    private sealed record StreamPing : IStreamRequest<int>;
    /// <summary>A no-op generic request behavior.</summary>
    private class RequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        /// <inheritdoc />
        public ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken) => next(message, cancellationToken);
    }
    /// <summary>A no-op generic stream behavior.</summary>
    private sealed class StreamBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where TRequest : IStreamRequest<TResponse>
    {
        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> Handle(TRequest message, StreamMessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken) => next(message, cancellationToken);
    }
    /// <summary>An invalid abstract implementation.</summary>
    private abstract class AbstractBehavior<TRequest, TResponse> : RequestBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>;
    /// <summary>An invalid reordered generic mapping.</summary>
    private sealed class Reordered<TResponse, TRequest> : RequestBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>;
    /// <summary>Caller-owned scope state used by request and stream paths.</summary>
    private sealed class ScopeState : IDisposable
    {
        /// <summary>Ordered request callbacks.</summary>
        public List<string> Calls { get; } = [];
        /// <summary>Whether the owning scope released this state.</summary>
        public bool Disposed
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Dispose() => Disposed = true;
    }
    /// <summary>The request handler sharing pipeline scope state.</summary>
    private sealed class Handler(ScopeState state) : IRequestHandler<Ping, int>
    {
        /// <inheritdoc />
        public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)
        {
            state.Calls.Add("handler");
            return ValueTask.FromResult(7);
        }
    }
    /// <summary>The outer request behavior.</summary>
    private sealed class Outer(ScopeState state) : RequestBehavior<Ping, int>, IPipelineBehavior<Ping, int>
    {
        /// <inheritdoc />
        public new async ValueTask<int> Handle(Ping message, MessageHandlerDelegate<Ping, int> next, CancellationToken cancellationToken)
        {
            state.Calls.Add("outer-in");
            var value = await next(message, cancellationToken).ConfigureAwait(false);
            state.Calls.Add("outer-out");
            return value;
        }
    }
    /// <summary>The inner request behavior.</summary>
    private sealed class Inner(ScopeState state) : RequestBehavior<Ping, int>, IPipelineBehavior<Ping, int>
    {
        /// <inheritdoc />
        public new async ValueTask<int> Handle(Ping message, MessageHandlerDelegate<Ping, int> next, CancellationToken cancellationToken)
        {
            state.Calls.Add("inner-in");
            var value = await next(message, cancellationToken).ConfigureAwait(false);
            state.Calls.Add("inner-out");
            return value;
        }
    }
    /// <summary>A scoped stream behavior retaining its dependency until enumeration completes.</summary>
    private sealed class ScopedStream(ScopeState state) : IStreamPipelineBehavior<StreamPing, int>
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<int> Handle(StreamPing message, StreamMessageHandlerDelegate<StreamPing, int> next,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var value in next(message, cancellationToken).ConfigureAwait(false))
            {
                Assert.That(state.Disposed, Is.False);
                yield return value;
            }
        }
    }
    /// <summary>A scoped streaming handler.</summary>
    private sealed class StreamHandler(ScopeState state) : IStreamRequestHandler<StreamPing, int>
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<int> Handle(StreamPing request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            Assert.That(state.Disposed, Is.False);
            cancellationToken.ThrowIfCancellationRequested();
            yield return 7;
        }
    }
}

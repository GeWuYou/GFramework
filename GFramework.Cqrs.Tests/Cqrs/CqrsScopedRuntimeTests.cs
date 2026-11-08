// Copyright (c) 2025-2026 GeWuYou
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

/// <summary>Explicit scoped runtime registration preserves caller scope across request, notification and stream dispatch.</summary>
[TestFixture]
public sealed class CqrsScopedRuntimeTests
{
    /// <summary>Two scopes isolate handlers and dependencies, while each scope owns and disposes its own dependency exactly once.</summary>
    [Test]
    public async Task ScopedRuntime_BindsAllDispatchModesToCallerScope()
    {
        var services = new ServiceCollection();
        services.AddScopedCqrsRuntime();
        services.AddScoped<ScopeState>();
        services.AddScoped<IRequestHandler<ScopeRequest, Guid>, ScopeRequestHandler<object>>();
        services.AddScoped<INotificationHandler<ScopeNotice>, ScopeNoticeHandler<object>>();
        services.AddScoped<IStreamRequestHandler<ScopeStream, Guid>, ScopeStreamHandler<object>>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
        var second = secondScope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
        var firstState = firstScope.ServiceProvider.GetRequiredService<ScopeState>();
        var secondState = secondScope.ServiceProvider.GetRequiredService<ScopeState>();
        Assert.That(firstScope.ServiceProvider.GetRequiredService<ICqrsRuntime>(), Is.SameAs(first));
        Assert.That(first, Is.Not.SameAs(second));
        await AssertDispatchesInScope(first, firstState).ConfigureAwait(false);
        await AssertDispatchesInScope(second, secondState).ConfigureAwait(false);
        Assert.That(firstState.Id, Is.Not.EqualTo(secondState.Id));
        await firstScope.DisposeAsync().ConfigureAwait(false);
        Assert.That(firstState.DisposeCalls, Is.EqualTo(1));
        Assert.That(secondState.DisposeCalls, Is.Zero);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await first.SendAsync(new ScopeRequest()).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(await second.SendAsync(new ScopeRequest()).ConfigureAwait(false), Is.EqualTo(secondState.Id));
        await secondScope.DisposeAsync().ConfigureAwait(false);
        Assert.That(secondState.DisposeCalls, Is.EqualTo(1));
    }

    /// <summary>The existing root-runtime mode keeps its singleton identity; selecting scope mode never overrides it implicitly.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void RuntimeModes_RejectDuplicateInstallation(bool scopedFirst)
    {
        var services = new ServiceCollection();
        if (scopedFirst)
        {
            services.AddScopedCqrsRuntime();
            Assert.Throws<InvalidOperationException>(() => services.AddCqrsRuntime());
        }
        else
        {
            services.AddCqrsRuntime();
            Assert.Throws<InvalidOperationException>(() => services.AddScopedCqrsRuntime());
            using var provider = services.BuildServiceProvider();
            Assert.That(provider.GetRequiredService<ICqrsRuntime>(),
                Is.SameAs(provider.GetRequiredService<ICqrsRuntime>()));
        }
    }

    /// <summary>Caller scopes are mandatory when validation is enabled, preventing accidental root-scope dependency capture.</summary>
    [Test]
    public void ScopedRuntime_RejectsRootResolution()
    {
        var services = new ServiceCollection();
        services.AddScopedCqrsRuntime();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICqrsRuntime>());
    }

    private static async Task AssertDispatchesInScope(ICqrsRuntime runtime, ScopeState state)
    {
        Assert.That(await runtime.SendAsync(new ScopeRequest()).ConfigureAwait(false), Is.EqualTo(state.Id));
        await runtime.PublishAsync(new ScopeNotice()).ConfigureAwait(false);
        Assert.That(state.NotificationIds, Is.EqualTo([state.Id]));
        var streamIds = new List<Guid>();
        await foreach (var id in runtime.CreateStream(new ScopeStream()).ConfigureAwait(false))
        {
            streamIds.Add(id);
        }
        Assert.That(streamIds, Is.EqualTo([state.Id]));
    }

    private sealed record ScopeRequest : IRequest<Guid>;
    private sealed record ScopeNotice : INotification;
    private sealed record ScopeStream : IStreamRequest<Guid>;

    private sealed class ScopeState : IDisposable
    {
        /// <summary>The dependency identity observed by all handlers inside one scope.</summary>
        public Guid Id { get; } = Guid.NewGuid();
        /// <summary>Actual notification dispatches observed by this dependency.</summary>
        public List<Guid> NotificationIds { get; } = [];
        /// <summary>Number of scope-owned resource releases.</summary>
        public int DisposeCalls
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Dispose() => DisposeCalls++;
    }

    // Open generic fixture doubles are excluded by both existing assembly scanners. Only this test closes them explicitly.
    private sealed class ScopeRequestHandler<TFixture>(ScopeState state) : IRequestHandler<ScopeRequest, Guid>
    {
        /// <inheritdoc />
        public ValueTask<Guid> Handle(ScopeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(state.Id);
    }

    private sealed class ScopeNoticeHandler<TFixture>(ScopeState state) : INotificationHandler<ScopeNotice>
    {
        /// <inheritdoc />
        public ValueTask Handle(ScopeNotice notification, CancellationToken cancellationToken)
        {
            state.NotificationIds.Add(state.Id);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ScopeStreamHandler<TFixture>(ScopeState state) : IStreamRequestHandler<ScopeStream, Guid>
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<Guid> Handle(ScopeStream request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return state.Id;
        }
    }
}

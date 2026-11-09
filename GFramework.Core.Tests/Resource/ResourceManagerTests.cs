// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Resource;
using NUnit.Framework;

namespace GFramework.Core.Tests.Resource;

/// <summary>Verifies observable acquisition, cancellation and cache contracts without native resource dependencies.</summary>
[TestFixture]
public sealed class ResourceManagerTests
{
    /// <summary>Each acquisition gets an independent lease while the cache loads one resource ownership.</summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042",
        Justification = "This mixed sync/async resource regression intentionally calls Acquire to retain synchronous lease and loader coverage.")]
    public async Task Acquire_SharesCacheAcrossSyncAndAsyncLeases()
    {
        await using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        using var first = manager.Acquire<TestResource>("asset");
        using var second = await manager.AcquireAsync<TestResource>("asset", CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(first.Resource, Is.SameAs(second.Resource));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(loader.LoadCount, Is.EqualTo(1));
            Assert.That(manager.LoadedResourceCount, Is.EqualTo(1));
            Assert.That(manager.IsLoaded<TestResource>("asset"), Is.True);
            Assert.That(first.Path, Is.EqualTo("asset"));
            Assert.That(manager.Unload<TestResource>("asset"), Is.False);
        });
    }

    /// <summary>Independent retained leases outlive disposal of their source lease; repeated disposal does nothing.</summary>
    [Test]
    public void Retain_CreatesIndependentIdempotentLease()
    {
        using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        var first = manager.Acquire<TestResource>("asset");
        var resource = first.Resource;
        var second = first.Retain();
        first.Dispose();
        first.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.False);
            Assert.That(second.IsValid, Is.True);
            Assert.That(second.Resource, Is.SameAs(resource));
            Assert.That(resource.IsDisposed, Is.False);
            Assert.That(loader.UnloadCount, Is.Zero);
        });
        Assert.Throws<ObjectDisposedException>(() => _ = first.Resource);
        Assert.Throws<ObjectDisposedException>(() => first.Retain());
        second.Dispose();
        second.Dispose();
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
    }

    /// <summary>Manual release retains an unleased entry until explicit eviction.</summary>
    [Test]
    public void ManualPolicy_RetainsUntilExplicitUnload()
    {
        using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        var lease = manager.Acquire<TestResource>("asset");
        lease.Dispose();
        Assert.That(loader.UnloadCount, Is.Zero);
        Assert.That(manager.Unload<TestResource>("asset"), Is.True);
        Assert.That(manager.Unload<TestResource>("asset"), Is.False);
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
    }

    /// <summary>A preload intentionally pins a cache entry even under automatic release.</summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042",
        Justification = "This mixed sync/async resource regression intentionally calls Acquire to retain synchronous lease and loader coverage.")]
    public async Task Preload_PinsUntilExplicitUnload()
    {
        await using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        await manager.PreloadAsync<TestResource>("asset");
        var lease = manager.Acquire<TestResource>("asset");
        lease.Dispose();
        Assert.That(loader.LoadCount, Is.EqualTo(1));
        Assert.That(loader.UnloadCount, Is.Zero);
        Assert.That(manager.Unload<TestResource>("asset"), Is.True);
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
    }

    /// <summary>Every same-key waiter shares one underlying load and receives its own lease.</summary>
    [Test]
    public async Task ConcurrentAcquireAsync_PerformsOneLoad()
    {
        await using var manager = new ResourceManager();
        var loader = new ControlledLoader();
        manager.RegisterLoader(loader);
        var requests = Enumerable.Range(0, 32).Select(_ => manager.AcquireAsync<TestResource>("asset", CancellationToken.None)).ToArray();
        Assert.That(loader.LoadCount, Is.EqualTo(1));
        loader.Completion.SetResult(new TestResource());
        var leases = await Task.WhenAll(requests);
        Assert.That(leases.Select(lease => lease.Resource).Distinct().Count(), Is.EqualTo(1));
        Assert.That(leases.Distinct().Count(), Is.EqualTo(32));
        foreach (var lease in leases)
            lease.Dispose();
        manager.UnloadAll();
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
    }

    /// <summary>Canceling one wait cannot cancel the shared loader or another acquisition.</summary>
    [Test]
    public async Task Cancellation_IsIsolatedPerWaiter()
    {
        await using var manager = new ResourceManager();
        using var cancellation = new CancellationTokenSource();
        var loader = new ControlledLoader();
        manager.RegisterLoader(loader);
        var canceled = manager.AcquireAsync<TestResource>("asset", cancellation.Token);
        var successful = manager.AcquireAsync<TestResource>("asset", CancellationToken.None);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => canceled);
        Assert.That(successful.IsCompleted, Is.False);
        Assert.That(loader.Token.CanBeCanceled, Is.False);
        loader.Completion.SetResult(new TestResource());
        using var lease = await successful;
        Assert.That(lease.IsValid, Is.True);
        Assert.That(loader.LoadCount, Is.EqualTo(1));
    }

    /// <summary>Canceled waiters leave no lease reservation preventing automatic release after loading finishes.</summary>
    [Test]
    public async Task Cancellation_AllWaitersCanceled_AllowsRelease()
    {
        var manager = new ResourceManager();
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        var loader = new ControlledLoader();
        manager.RegisterLoader(loader);
        using var cancellation = new CancellationTokenSource();
        var request = manager.AcquireAsync<TestResource>("asset", cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => request);
        var shutdown = manager.DisposeAsync();
        loader.Completion.SetResult(new TestResource());
        await shutdown;
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
    }

    /// <summary>Closing waits for loading, invalidates pending acquisitions, and releases an arriving unleased result.</summary>
    [Test]
    public async Task DisposeAsync_WaitsForPendingLoadAndRejectsItsAcquisition()
    {
        var manager = new ResourceManager();
        var loader = new ControlledLoader();
        manager.RegisterLoader(loader);
        var request = manager.AcquireAsync<TestResource>("asset", CancellationToken.None);
        var first = manager.DisposeAsync().AsTask();
        var second = manager.DisposeAsync().AsTask();
        Assert.That(first, Is.SameAs(second));
        Assert.That(first.IsCompleted, Is.False);
        loader.Completion.SetResult(new TestResource());
        await first;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => request);
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
    }

    /// <summary>Synchronous APIs refuse to block an in-flight asynchronous loader's host context.</summary>
    [Test]
    public async Task SynchronousCalls_DoNotBlockAsynchronousLoad()
    {
        var manager = new ResourceManager();
        var loader = new ControlledLoader();
        manager.RegisterLoader(loader);
        var pending = manager.AcquireAsync<TestResource>("asset", CancellationToken.None);
        Assert.Throws<InvalidOperationException>(() => manager.Acquire<TestResource>("asset"));
        Assert.Throws<InvalidOperationException>(() => manager.Dispose());
        loader.Completion.SetResult(new TestResource());
        using var lease = await pending;
        await manager.DisposeAsync();
        Assert.That(lease.IsValid, Is.True);
    }

    /// <summary>Successful pre-cancellation never starts a loader or inserts an entry.</summary>
    [Test]
    public async Task PreCanceledCall_DoesNotStartWork()
    {
        await using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        await Assert.CatchAsync<OperationCanceledException>(() => manager.AcquireAsync<TestResource>("asset", new CancellationToken(canceled: true)));
        await Assert.CatchAsync<OperationCanceledException>(() => manager.PreloadAsync<TestResource>("asset", new CancellationToken(canceled: true)));
        Assert.That(loader.LoadCount, Is.Zero);
    }

    /// <summary>Invalid paths fail before any cache or loader mutation.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void InvalidPaths_AreRejected(string? path)
    {
        using var manager = new ResourceManager();
        Assert.Catch<ArgumentException>(() => manager.Acquire<TestResource>(path!));
        Assert.Catch<ArgumentException>(() => manager.Unload<TestResource>(path!));
        Assert.Catch<ArgumentException>(() => manager.IsLoaded<TestResource>(path!));
    }

    /// <summary>Missing loaders and unsupported paths fail explicitly rather than returning a null resource.</summary>
    [Test]
    public void InvalidLoaderConfiguration_IsVisible()
    {
        using var manager = new ResourceManager();
        Assert.Throws<InvalidOperationException>(() => manager.Acquire<TestResource>("asset"));
        Assert.Throws<ArgumentNullException>(() => manager.RegisterLoader<TestResource>(null!));
        Assert.Throws<ArgumentNullException>(() => manager.SetReleaseStrategy(null!));
        manager.RegisterLoader(CreateLoader());
        Assert.Throws<InvalidOperationException>(() => manager.Acquire<TestResource>("unsupported"));
        manager.UnregisterLoader<TestResource>();
        Assert.Throws<InvalidOperationException>(() => manager.Acquire<TestResource>("asset"));
    }

    /// <summary>Closure rejects every operation that could begin new resource work.</summary>
    [Test]
    public async Task ClosedManager_RejectsNewOperations()
    {
        var manager = new ResourceManager();
        await manager.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => manager.Acquire<TestResource>("asset"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AcquireAsync<TestResource>("asset", CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.PreloadAsync<TestResource>("asset"));
        Assert.Throws<ObjectDisposedException>(() => manager.RegisterLoader(CreateLoader()));
        Assert.Throws<ObjectDisposedException>(() => manager.UnregisterLoader<TestResource>());
        Assert.Throws<ObjectDisposedException>(() => manager.Unload<TestResource>("asset"));
        Assert.Throws<ObjectDisposedException>(() => manager.UnloadAll());
        Assert.Throws<ObjectDisposedException>(() => manager.IsLoaded<TestResource>("asset"));
        Assert.Throws<ObjectDisposedException>(() => manager.SetReleaseStrategy(new AutoReleaseStrategy()));
    }

    /// <summary>Switching release policy changes the decision when a lease is subsequently released.</summary>
    [Test]
    public void ReleasePolicy_CanBeChangedBeforeLastLeaseEnds()
    {
        using var manager = new ResourceManager();
        var loader = CreateLoader();
        manager.RegisterLoader(loader);
        var lease = manager.Acquire<TestResource>("asset");
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        lease.Dispose();
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
    }

    /// <summary>Creates a deterministic in-memory loader.</summary>
    private static TestResourceLoader CreateLoader()
    {
        var loader = new TestResourceLoader();
        loader.AddTestData("asset", "content");
        return loader;
    }

    /// <summary>Exposes loading completion so concurrency assertions do not rely on sleeps.</summary>
    private sealed class ControlledLoader : IResourceLoader<TestResource>
    {
        /// <summary>Gets the shared native-load stand-in.</summary>
        public TaskCompletionSource<TestResource> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets the number of load invocations.</summary>
        public int LoadCount
        {
            get; private set;
        }

        /// <summary>Gets the number of releases.</summary>
        public int UnloadCount
        {
            get; private set;
        }

        /// <summary>Gets the token supplied to the shared loader.</summary>
        public CancellationToken Token
        {
            get; private set;
        }

        /// <inheritdoc />
        public TestResource Load(string path) => throw new InvalidOperationException("Use async loading in this probe.");

        /// <inheritdoc />
        public Task<TestResource> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            Token = cancellationToken;
            return Completion.Task;
        }

        /// <inheritdoc />
        public void Unload(TestResource resource)
        {
            UnloadCount++;
            resource.IsDisposed = true;
        }

        /// <inheritdoc />
        public bool CanLoad(string path) => true;
    }
}

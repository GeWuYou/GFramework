// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Resource;
using NUnit.Framework;

namespace GFramework.Core.Tests.Resource;

/// <summary>Verifies original loader ownership, closing leases and release failure behavior.</summary>
[TestFixture]
public sealed class ResourceOwnershipTests
{
    /// <summary>A base-type load of a derived object returns to the original loader without a second Dispose.</summary>
    [Test]
    public void DerivedResource_UsesOriginalLoaderExactlyOnce()
    {
        using var manager = new ResourceManager();
        var resource = new DerivedResource();
        var loader = new OwnedLoader<OwnedResource>(() => resource);
        manager.RegisterLoader(loader);
        var lease = manager.Acquire<OwnedResource>("derived");
        lease.Dispose();
        Assert.That(manager.Unload<OwnedResource>("derived"), Is.True);
        Assert.That(loader.UnloadCount, Is.EqualTo(1));
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>Replacement or removal changes future loads without transferring ownership of an existing entry.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void LoaderRegistrationChanges_PreserveOriginalOwner(bool unregister)
    {
        using var manager = new ResourceManager();
        var original = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        var replacement = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(original);
        var lease = manager.Acquire<OwnedResource>("asset");
        var resource = lease.Resource;
        manager.RegisterLoader(replacement);
        if (unregister)
            manager.UnregisterLoader<OwnedResource>();
        lease.Dispose();
        manager.UnloadAll();
        Assert.That(original.UnloadCount, Is.EqualTo(1));
        Assert.That(replacement.UnloadCount, Is.Zero);
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>Bulk eviction preserves leased ownership and releases only unused entries.</summary>
    [Test]
    public void UnloadAll_PreservesActiveLeases()
    {
        using var manager = new ResourceManager();
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(loader);
        var retained = manager.Acquire<OwnedResource>("retained");
        var unused = manager.Acquire<OwnedResource>("unused");
        var unusedResource = unused.Resource;
        unused.Dispose();
        manager.UnloadAll();
        Assert.That(manager.LoadedResourceCount, Is.EqualTo(1));
        Assert.That(retained.Resource.DisposeCount, Is.Zero);
        Assert.That(unusedResource.DisposeCount, Is.EqualTo(1));
        var retainedResource = retained.Resource;
        retained.Dispose();
        manager.UnloadAll();
        Assert.That(manager.LoadedResourceCount, Is.Zero);
        Assert.That(retainedResource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>Shutdown never invalidates active leases, but prevents them from creating new leases.</summary>
    [Test]
    public async Task DisposeAsync_DefersLeasedReleaseUntilLeaseDisposal()
    {
        var manager = new ResourceManager();
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(loader);
        var lease = manager.Acquire<OwnedResource>("asset");
        var resource = lease.Resource;
        await manager.DisposeAsync();
        Assert.That(lease.Resource, Is.SameAs(resource));
        Assert.That(lease.IsValid, Is.True);
        Assert.That(resource.DisposeCount, Is.Zero);
        Assert.Throws<ObjectDisposedException>(() => lease.Retain());
        lease.Dispose();
        lease.Dispose();
        await manager.DisposeAsync();
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
    }

    /// <summary>Type is part of identity, so equal paths for two loader types do not overwrite one another.</summary>
    [Test]
    public void SamePath_DifferentTypesKeepSeparateOwnership()
    {
        using var manager = new ResourceManager();
        var baseLoader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        var derivedLoader = new OwnedLoader<DerivedResource>(() => new DerivedResource());
        manager.RegisterLoader(baseLoader);
        manager.RegisterLoader(derivedLoader);
        using var first = manager.Acquire<OwnedResource>("same");
        using var second = manager.Acquire<DerivedResource>("same");
        Assert.That(first.Resource, Is.Not.SameAs(second.Resource));
        Assert.That(manager.LoadedResourceCount, Is.EqualTo(2));
    }

    /// <summary>The single-flight design prevents duplicate backend loads instead of releasing race losers afterward.</summary>
    [Test]
    public async Task ConcurrentAcquireAsync_DoesNotCreateLosingResource()
    {
        var pending = new TaskCompletionSource<OwnedResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = new OwnedLoader<OwnedResource>(() => throw new InvalidOperationException())
        {
            LoadAsyncCallback = () => pending.Task,
        };
        await using var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        var first = manager.AcquireAsync<OwnedResource>("shared");
        var second = manager.AcquireAsync<OwnedResource>("shared");
        pending.SetResult(new OwnedResource());
        var leases = await Task.WhenAll(first, second);
        Assert.That(loader.LoadCount, Is.EqualTo(1));
        Assert.That(leases[0].Resource, Is.SameAs(leases[1].Resource));
        var resource = leases[0].Resource;
        foreach (var lease in leases)
            lease.Dispose();
        manager.UnloadAll();
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>All current waiters observe the original failure, and a later acquisition starts a fresh attempt.</summary>
    [Test]
    public async Task LoadFailure_PropagatesAndAllowsRetry()
    {
        var failure = new InvalidOperationException("backend failed");
        var pending = new TaskCompletionSource<OwnedResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource())
        {
            LoadAsyncCallback = () => pending.Task,
        };
        await using var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        var first = manager.AcquireAsync<OwnedResource>("asset");
        var second = manager.AcquireAsync<OwnedResource>("asset");
        pending.SetException(failure);
        var firstError = await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        var secondError = await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.That(firstError, Is.SameAs(failure));
        Assert.That(secondError, Is.SameAs(failure));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
        using var retried = manager.Acquire<OwnedResource>("asset");
        Assert.That(retried.IsValid, Is.True);
        Assert.That(loader.LoadCount, Is.EqualTo(2));
    }

    /// <summary>Detaching before release lets a loader reenter acquisition without exposing the old ownership.</summary>
    [Test]
    public void Release_ReentrantReloadAndOldLeaseDisposalAreIndependent()
    {
        using var manager = new ResourceManager();
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(loader);
        var first = manager.Acquire<OwnedResource>("asset");
        var original = first.Resource;
        IResourceHandle<OwnedResource>? replacement = null;
        loader.UnloadCallback = () => replacement = manager.Acquire<OwnedResource>("asset");
        first.Dispose();
        first.Dispose();
        Assert.That(original.DisposeCount, Is.EqualTo(1));
        Assert.That(replacement, Is.Not.Null);
        Assert.That(replacement!.Resource, Is.Not.SameAs(original));
        Assert.That(replacement.Resource.DisposeCount, Is.Zero);
        loader.UnloadCallback = null;
        replacement.Dispose();
        Assert.That(loader.UnloadCount, Is.EqualTo(2));
    }

    /// <summary>A failing release does not prevent attempts to release other cache entries or cause a retry on disposal.</summary>
    [Test]
    public void UnloadAll_AttemptsEveryReleaseAndAggregatesFailures()
    {
        using var manager = new ResourceManager();
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource())
        {
            UnloadCallback = () => throw new InvalidOperationException("release failed"),
        };
        manager.RegisterLoader(loader);
        manager.Acquire<OwnedResource>("a").Dispose();
        manager.Acquire<OwnedResource>("b").Dispose();
        var error = Assert.Throws<AggregateException>(() => manager.UnloadAll());
        Assert.That(error!.InnerExceptions, Has.Count.EqualTo(2));
        Assert.That(loader.UnloadCount, Is.EqualTo(2));
        Assert.That(manager.LoadedResourceCount, Is.Zero);
    }

    /// <summary>Manager shutdown reports release failures while still attempting every resource exactly once.</summary>
    [Test]
    public async Task DisposeAsync_ReportsCleanupFailuresAndSharesItsResult()
    {
        var manager = new ResourceManager();
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource())
        {
            UnloadCallback = () => throw new InvalidOperationException("release failed"),
        };
        manager.RegisterLoader(loader);
        manager.Acquire<OwnedResource>("a").Dispose();
        manager.Acquire<OwnedResource>("b").Dispose();
        var first = manager.DisposeAsync().AsTask();
        var second = manager.DisposeAsync().AsTask();
        Assert.That(first, Is.SameAs(second));
        var error = await Assert.ThrowsAsync<AggregateException>(() => first);
        Assert.That(error!.InnerExceptions, Has.Count.EqualTo(2));
        Assert.That(loader.UnloadCount, Is.EqualTo(2));
    }

    /// <summary>Policy callbacks cannot invalidate a lease acquired while the policy is deciding.</summary>
    [Test]
    public void ReentrantReleasePolicy_DoesNotEvictNewLease()
    {
        using var manager = new ResourceManager();
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(loader);
        var first = manager.Acquire<OwnedResource>("asset");
        var resource = first.Resource;
        IResourceHandle<OwnedResource>? replacement = null;
        var policy = new ReentrantPolicy();
        policy.Callback = () =>
        {
            policy.Callback = null;
            replacement = manager.Acquire<OwnedResource>("asset");
        };
        manager.SetReleaseStrategy(policy);
        first.Dispose();
        Assert.That(resource.DisposeCount, Is.Zero);
        var retained = replacement ?? throw new InvalidOperationException("Policy did not acquire its lease.");
        Assert.That(retained.Resource, Is.SameAs(resource));
        retained.Dispose();
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>One-shot callback probes defensive ownership revalidation around application policies.</summary>
    private sealed class ReentrantPolicy : IResourceReleaseStrategy
    {
        /// <summary>Gets or sets the callback executed during the next decision.</summary>
        public Action? Callback
        {
            get; set;
        }

        /// <inheritdoc />
        public bool ShouldRelease(string path, int refCount)
        {
            Callback?.Invoke();
            return true;
        }
    }

    /// <summary>Exposes disposal counting to prove that the manager does not add its own Dispose call.</summary>
    private class OwnedResource : IDisposable
    {
        /// <summary>Gets the number of release attempts.</summary>
        public int DisposeCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public void Dispose() => DisposeCount++;
    }

    /// <summary>Allows declared load type to differ from runtime resource type.</summary>
    private sealed class DerivedResource : OwnedResource;

    /// <summary>Implements externally owned loader behavior with deterministic failure and reentrancy hooks.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    private sealed class OwnedLoader<T>(Func<T> create) : IResourceLoader<T> where T : OwnedResource
    {
        /// <summary>Gets the number of backend loads.</summary>
        public int LoadCount
        {
            get; private set;
        }

        /// <summary>Gets the number of matching release attempts.</summary>
        public int UnloadCount
        {
            get; private set;
        }

        /// <summary>Gets the optional asynchronous backend behavior.</summary>
        public Func<Task<T>>? LoadAsyncCallback
        {
            get; init;
        }

        /// <summary>Gets or sets a callback executed after each resource is disposed.</summary>
        public Action? UnloadCallback
        {
            get; set;
        }

        /// <inheritdoc />
        public T Load(string path)
        {
            LoadCount++;
            return create();
        }

        /// <inheritdoc />
        public Task<T> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return LoadAsyncCallback?.Invoke() ?? Task.FromResult(create());
        }

        /// <inheritdoc />
        public void Unload(T resource)
        {
            UnloadCount++;
            resource.Dispose();
            UnloadCallback?.Invoke();
        }

        /// <inheritdoc />
        public bool CanLoad(string path) => true;
    }
}

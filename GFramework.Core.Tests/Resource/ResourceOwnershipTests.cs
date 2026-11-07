// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Resource;

namespace GFramework.Core.Tests.Resource;

/// <summary>
///     验证资源实例与原始加载器、活跃句柄之间的所有权边界。
/// </summary>
[TestFixture]
public class ResourceOwnershipTests
{
    /// <summary>
    ///     基类加载器创建的派生对象仍交回原始加载器，不再次调用 Dispose。
    /// </summary>
    [Test]
    public void Unload_DerivedResource_UsesOriginalLoaderAndDisposesOnce()
    {
        var resource = new DerivedResource();
        var loader = new OwnedLoader<OwnedResource>(() => resource);
        var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        manager.Load<OwnedResource>("derived");

        var unloaded = manager.Unload("derived");

        Assert.Multiple(() =>
        {
            Assert.That(unloaded, Is.True);
            Assert.That(loader.UnloadCount, Is.EqualTo(1));
            Assert.That(resource.DisposeCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     替换和注销注册项都不能把既有资源转交给另一个加载器。
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void Unload_AfterLoaderRegistrationChanges_RetainsOriginalOwner(bool unregister)
    {
        var original = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        var replacement = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        var manager = new ResourceManager();
        manager.RegisterLoader(original);
        var resource = manager.Load<OwnedResource>("owned");
        manager.RegisterLoader(replacement);
        if (unregister)
            manager.UnregisterLoader<OwnedResource>();

        manager.Unload("owned");

        Assert.Multiple(() =>
        {
            Assert.That(original.UnloadCount, Is.EqualTo(1));
            Assert.That(replacement.UnloadCount, Is.Zero);
            Assert.That(resource!.DisposeCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     批量卸载仅移除无句柄资源，句柄释放后可在下一次批量卸载中清理。
    /// </summary>
    [Test]
    public void UnloadAll_WithActiveHandle_PreservesResourceUntilHandleReleased()
    {
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        var retained = manager.Load<OwnedResource>("retained");
        var unused = manager.Load<OwnedResource>("unused");
        var handle = manager.GetHandle<OwnedResource>("retained")!;

        manager.UnloadAll();

        Assert.Multiple(() =>
        {
            Assert.That(manager.GetLoadedResourcePaths(), Is.EquivalentTo(new[] { "retained" }));
            Assert.That(handle.IsValid, Is.True);
            Assert.That(retained!.DisposeCount, Is.Zero);
            Assert.That(unused!.DisposeCount, Is.EqualTo(1));
        });

        handle.Dispose();
        manager.UnloadAll();

        Assert.Multiple(() =>
        {
            Assert.That(manager.LoadedResourceCount, Is.Zero);
            Assert.That(retained!.DisposeCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     异步加载器返回同一内部缓存对象时，失败竞争者不能释放获胜条目仍在使用的对象。
    /// </summary>
    [Test]
    public async Task ConcurrentLoadAsync_SharedLoaderInstance_DoesNotUnloadCachedResource()
    {
        var pending = new TaskCompletionSource<OwnedResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = new OwnedLoader<OwnedResource>(() => throw new InvalidOperationException())
        {
            LoadAsyncCallback = () => pending.Task
        };
        var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        var first = manager.LoadAsync<OwnedResource>("shared");
        var second = manager.LoadAsync<OwnedResource>("shared");
        var resource = new OwnedResource();

        pending.SetResult(resource);
        var loaded = await Task.WhenAll(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(loaded[0], Is.SameAs(resource));
            Assert.That(loaded[1], Is.SameAs(resource));
            Assert.That(loader.UnloadCount, Is.Zero);
            Assert.That(resource.DisposeCount, Is.Zero);
        });

        manager.UnloadAll();
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     独立的异步加载结果发生竞争时，仅释放未进入缓存的实例。
    /// </summary>
    [Test]
    public async Task ConcurrentLoadAsync_DistinctInstances_UnloadsOnlyLosingResource()
    {
        var pending = new Queue<TaskCompletionSource<OwnedResource>>();
        var firstSource = new TaskCompletionSource<OwnedResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSource = new TaskCompletionSource<OwnedResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending.Enqueue(firstSource);
        pending.Enqueue(secondSource);
        var loader = new OwnedLoader<OwnedResource>(() => throw new InvalidOperationException())
        {
            LoadAsyncCallback = () => pending.Dequeue().Task
        };
        var manager = new ResourceManager();
        manager.RegisterLoader(loader);
        var first = manager.LoadAsync<OwnedResource>("distinct");
        var second = manager.LoadAsync<OwnedResource>("distinct");
        var winner = new OwnedResource();
        var loser = new OwnedResource();
        firstSource.SetResult(winner);
        await first;
        secondSource.SetResult(loser);
        var secondResult = await second;

        Assert.Multiple(() =>
        {
            Assert.That(secondResult, Is.SameAs(winner));
            Assert.That(winner.DisposeCount, Is.Zero);
            Assert.That(loser.DisposeCount, Is.EqualTo(1));
        });

        manager.UnloadAll();
        Assert.That(winner.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     卸载期间加载器重入不能取得正在释放的旧实例，旧句柄的重复释放不影响新条目。
    /// </summary>
    [Test]
    public void HandleRelease_WithReentrantReload_DoesNotExposeOldEntryOrReleaseReplacement()
    {
        var manager = new ResourceManager();
        manager.SetReleaseStrategy(new AutoReleaseStrategy());
        var loader = new OwnedLoader<OwnedResource>(() => new OwnedResource());
        manager.RegisterLoader(loader);
        var original = manager.Load<OwnedResource>("reloaded")!;
        var handle = manager.GetHandle<OwnedResource>("reloaded")!;
        IResourceHandle<OwnedResource>? reentrantHandle = null;
        OwnedResource? replacement = null;
        loader.UnloadCallback = () =>
        {
            reentrantHandle = manager.GetHandle<OwnedResource>("reloaded");
            replacement = manager.Load<OwnedResource>("reloaded");
        };

        handle.Dispose();
        handle.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(reentrantHandle, Is.Null);
            Assert.That(original.DisposeCount, Is.EqualTo(1));
            Assert.That(replacement, Is.Not.Null.And.Not.SameAs(original));
            Assert.That(replacement!.DisposeCount, Is.Zero);
            Assert.That(manager.LoadedResourceCount, Is.EqualTo(1));
        });

        loader.UnloadCallback = null;
        manager.UnloadAll();
    }

    /// <summary>
    ///     提供可计数释放的资源，加载器必须对其 Dispose 负责。
    /// </summary>
    private class OwnedResource : IDisposable
    {
        /// <summary>获取 Dispose 调用次数。</summary>
        public int DisposeCount { get; private set; }

        /// <summary>记录资源释放次数。</summary>
        public void Dispose() => DisposeCount++;
    }

    /// <summary>用于验证加载类型与资源实际类型不同的合法场景。</summary>
    private sealed class DerivedResource : OwnedResource
    {
    }

    /// <summary>
    ///     持有资源释放责任的测试加载器，支持可控异步结果和重入清理。
    /// </summary>
    /// <typeparam name="T">资源类型。</typeparam>
    /// <param name="create">同步创建操作。</param>
    private sealed class OwnedLoader<T>(Func<T> create) : IResourceLoader<T> where T : OwnedResource
    {
        /// <summary>获取卸载次数。</summary>
        public int UnloadCount { get; private set; }

        /// <summary>获取或设置可控异步加载操作。</summary>
        public Func<Task<T>>? LoadAsyncCallback { get; init; }

        /// <summary>获取或设置卸载过程中的回调。</summary>
        public Action? UnloadCallback { get; set; }

        /// <inheritdoc />
        public T Load(string path) => create();

        /// <inheritdoc />
        public Task<T> LoadAsync(string path) => LoadAsyncCallback?.Invoke() ?? Task.FromResult(create());

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

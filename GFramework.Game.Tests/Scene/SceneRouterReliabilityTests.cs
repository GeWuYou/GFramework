// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using GFramework.Game.Abstractions.Routing;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Scene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.Tests.Scene;

/// <summary>验证场景导航失败、守卫拒绝与停止时的栈和节点所有权。</summary>
[TestFixture]
public class SceneRouterReliabilityTests
{
    /// <summary>拒绝离开时 Clear 和 Replace 都立即结束，且不破坏现有场景。</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task DestructiveNavigation_WhenLeaveGuardRejects_RetainsPreviousScene(bool replace)
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        var guard = CreateGuard();
        var leaveCalls = 0;
        // 第二次允许离开，使旧的无限重试实现也能终止，并通过调用次数和状态断言失败。
        guard.Setup(value => value.CanLeaveAsync("current"))
            .Returns(() => ValueTask.FromResult(++leaveCalls > 1));
        router.AddGuard(guard.Object);

        if (replace)
            await router.ReplaceAsync("target");
        else
            await router.ClearAsync();

        Assert.Multiple(() =>
        {
            Assert.That(leaveCalls, Is.EqualTo(1));
            Assert.That(router.Current, Is.SameAs(current.Object));
            Assert.That(router.IsTransitioning, Is.False);
        });
        current.Verify(value => value.OnExitAsync(), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>()), Times.Never);
        await router.DestroyAsync();
    }

    /// <summary>目标进入守卫拒绝时不卸载旧场景，也不创建或加载目标。</summary>
    [Test]
    public async Task Replace_WhenEnterGuardRejects_RetainsPreviousScene()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        var guard = CreateGuard();
        guard.Setup(value => value.CanEnterAsync("target", It.IsAny<IRouteContext?>()))
            .Returns(ValueTask.FromResult(false));
        router.AddGuard(guard.Object);

        await router.ReplaceAsync("target");

        Assert.That(router.Current, Is.SameAs(current.Object));
        current.Verify(value => value.OnUnloadAsync(), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>()), Times.Never);
        await router.DestroyAsync();
    }

    /// <summary>创建或加载目标失败时保留旧场景，已附加的新实例得到卸载和移除。</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Replace_WhenPreparationFails_PreservesPreviousScene(bool failDuringLoad)
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        if (failDuringLoad)
            target.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>()))
                .Returns(() => ValueTask.FromException(new InvalidOperationException("load failed")));
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");

        await Assert.CatchAsync<Exception>(() =>
            router.ReplaceAsync(failDuringLoad ? "target" : "missing").AsTask());

        Assert.Multiple(() =>
        {
            Assert.That(router.Current, Is.SameAs(current.Object));
            Assert.That(router.Count, Is.EqualTo(1));
            Assert.That(router.IsTransitioning, Is.False);
        });
        current.Verify(value => value.OnExitAsync(), Times.Never);
        current.Verify(value => value.OnPauseAsync(), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        if (failDuringLoad)
        {
            target.Verify(value => value.OnUnloadAsync(), Times.Once);
            root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        }
        await router.DestroyAsync();
    }

    /// <summary>新场景进入失败时清理其节点，并恢复被暂停的原场景。</summary>
    [Test]
    public async Task Push_WhenEnterFails_CleansTargetAndResumesPreviousScene()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        target.Setup(value => value.OnEnterAsync())
            .Returns(() => ValueTask.FromException(new InvalidOperationException("enter failed")));
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");

        await Assert.CatchAsync<AggregateException>(() => router.PushAsync("target").AsTask());

        Assert.Multiple(() =>
        {
            Assert.That(router.Current, Is.SameAs(current.Object));
            Assert.That(router.Count, Is.EqualTo(1));
            Assert.That(router.IsTransitioning, Is.False);
        });
        current.Verify(value => value.OnPauseAsync(), Times.Once);
        current.Verify(value => value.OnResumeAsync(), Times.Once);
        target.Verify(value => value.OnExitAsync(), Times.Once);
        target.Verify(value => value.OnUnloadAsync(), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        await router.DestroyAsync();
    }

    /// <summary>暂停旧场景失败时只卸载准备好的目标，不调用尚未开始的进入/退出回调。</summary>
    [Test]
    public async Task Push_WhenPreviousPauseFails_DoesNotExitUnenteredTarget()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        current.Setup(value => value.OnPauseAsync())
            .Returns(() => ValueTask.FromException(new InvalidOperationException("pause failed")));

        await Assert.CatchAsync<AggregateException>(() => router.PushAsync("target").AsTask());

        Assert.That(router.Current, Is.SameAs(current.Object));
        target.Verify(value => value.OnEnterAsync(), Times.Never);
        target.Verify(value => value.OnExitAsync(), Times.Never);
        target.Verify(value => value.OnUnloadAsync(), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        current.Verify(value => value.OnResumeAsync(), Times.Once);
        await router.DestroyAsync();
    }

    /// <summary>退出回调失败仍会卸载节点、移除节点并恢复下层场景。</summary>
    [Test]
    public async Task Pop_WhenExitFails_AttemptsRemainingCleanupAndResume()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        await router.PushAsync("target");
        target.Setup(value => value.OnExitAsync())
            .Returns(() => ValueTask.FromException(new InvalidOperationException("exit failed")));

        await Assert.CatchAsync<AggregateException>(() => router.PopAsync().AsTask());

        Assert.That(router.Current, Is.SameAs(current.Object));
        target.Verify(value => value.OnUnloadAsync(), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        current.Verify(value => value.OnResumeAsync(), Times.Once);
        await router.DestroyAsync();
    }

    /// <summary>停止绕过守卫，清理错误不阻断其他场景；重复停止共享结果而不再次清理。</summary>
    [Test]
    public async Task DestroyAsync_WithRejectedGuardAndCleanupFailure_CleansEverySceneOnce()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        await router.PushAsync("target");
        var guard = CreateGuard();
        guard.Setup(value => value.CanLeaveAsync(It.IsAny<string>())).Returns(ValueTask.FromResult(false));
        router.AddGuard(guard.Object);
        target.Setup(value => value.OnExitAsync())
            .Returns(() => ValueTask.FromException(new InvalidOperationException("exit failed")));

        var firstStop = router.DestroyAsync().AsTask();
        var secondStop = router.DestroyAsync().AsTask();
        await Assert.CatchAsync<AggregateException>(() => firstStop);

        Assert.Multiple(() =>
        {
            Assert.That(secondStop, Is.SameAs(firstStop));
            Assert.That(router.Current, Is.Null);
            Assert.That(router.Count, Is.Zero);
        });
        guard.Verify(value => value.CanLeaveAsync(It.IsAny<string>()), Times.Never);
        foreach (var scene in new[] { current, target })
        {
            scene.Verify(value => value.OnExitAsync(), Times.Once);
            scene.Verify(value => value.OnUnloadAsync(), Times.Once);
            root.Verify(value => value.RemoveScene(scene.Object), Times.Once);
        }
        Assert.Throws<ObjectDisposedException>(() => { _ = router.PushAsync("target"); });
    }

    /// <summary>创建只替代宿主和工厂的路由器，保留真实路由初始化与生命周期逻辑。</summary>
    private static (TestSceneRouter Router, Mock<ISceneRoot> Root) CreateRouter(params Mock<ISceneBehavior>[] scenes)
    {
        var factory = new Mock<ISceneFactory>();
        factory.Setup(value => value.Create(It.IsAny<string>()))
            .Returns((string key) => scenes.Single(scene => string.Equals(scene.Object.Key, key, StringComparison.Ordinal)).Object);
        var context = new Mock<IArchitectureContext>();
        context.Setup(value => value.GetUtility<ISceneFactory>()).Returns(factory.Object);
        context.Setup(value => value.GetService<ILoggerFactory>()).Returns(NullLoggerFactory.Instance);
        var router = new TestSceneRouter();
        ((IContextAware)router).SetContext(context.Object);
        router.Initialize();
        var root = new Mock<ISceneRoot>();
        router.BindRoot(root.Object);
        return (router, root);
    }

    /// <summary>创建默认全部生命周期成功的场景替身。</summary>
    private static Mock<ISceneBehavior> CreateScene(string key)
    {
        var scene = new Mock<ISceneBehavior>();
        scene.SetupGet(value => value.Key).Returns(key);
        scene.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnEnterAsync()).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnPauseAsync()).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnResumeAsync()).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnExitAsync()).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnUnloadAsync()).Returns(ValueTask.CompletedTask);
        return scene;
    }

    /// <summary>创建默认允许导航的守卫替身。</summary>
    private static Mock<IRouteGuard<ISceneBehavior>> CreateGuard()
    {
        var guard = new Mock<IRouteGuard<ISceneBehavior>>();
        guard.Setup(value => value.CanEnterAsync(It.IsAny<string>(), It.IsAny<IRouteContext?>()))
            .Returns(ValueTask.FromResult(true));
        guard.Setup(value => value.CanLeaveAsync(It.IsAny<string>())).Returns(ValueTask.FromResult(true));
        return guard;
    }

    /// <summary>使用真实场景路由，只关闭无关的扩展处理器注册。</summary>
    private sealed class TestSceneRouter : SceneRouterBase
    {
        /// <inheritdoc />
        protected override void RegisterHandlers()
        {
        }
    }
}

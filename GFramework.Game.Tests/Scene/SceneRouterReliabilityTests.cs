// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Routing;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Scene;
using GFramework.Game.Runtime;
using System.Threading;

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
        guard.Setup(value => value.CanLeaveAsync("current", It.IsAny<CancellationToken>()))
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
        current.Verify(value => value.OnExitAsync(It.IsAny<CancellationToken>()), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()), Times.Never);
        await router.StopAsync();
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
        guard.Setup(value => value.CanEnterAsync("target", It.IsAny<IRouteContext?>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromResult(false));
        router.AddGuard(guard.Object);

        await router.ReplaceAsync("target");

        Assert.That(router.Current, Is.SameAs(current.Object));
        current.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()), Times.Never);
        await router.StopAsync();
    }

    /// <summary>创建或加载目标失败时保留旧场景，已附加的新实例得到卸载和移除。</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Replace_WhenPreparationFails_PreservesPreviousScene(bool failDuringLoad)
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        if (failDuringLoad)
            target.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromException(new InvalidOperationException("load failed")));
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");

        await Assert.CatchAsync<Exception>(() =>
            router.ReplaceAsync(failDuringLoad ? "target" : "missing"));

        Assert.Multiple(() =>
        {
            Assert.That(router.Current, Is.SameAs(current.Object));
            Assert.That(router.Count, Is.EqualTo(1));
            Assert.That(router.IsTransitioning, Is.False);
        });
        current.Verify(value => value.OnExitAsync(It.IsAny<CancellationToken>()), Times.Never);
        current.Verify(value => value.OnPauseAsync(It.IsAny<CancellationToken>()), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        if (failDuringLoad)
        {
            target.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
            root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        }
        await router.StopAsync();
    }

    /// <summary>新场景进入失败时清理其节点，并恢复被暂停的原场景。</summary>
    [Test]
    public async Task Push_WhenEnterFails_CleansTargetAndResumesPreviousScene()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        target.Setup(value => value.OnEnterAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("enter failed")));
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");

        await Assert.CatchAsync<AggregateException>(() => router.PushAsync("target"));

        Assert.Multiple(() =>
        {
            Assert.That(router.Current, Is.SameAs(current.Object));
            Assert.That(router.Count, Is.EqualTo(1));
            Assert.That(router.IsTransitioning, Is.False);
        });
        current.Verify(value => value.OnPauseAsync(It.IsAny<CancellationToken>()), Times.Once);
        current.Verify(value => value.OnResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        target.Verify(value => value.OnExitAsync(It.IsAny<CancellationToken>()), Times.Once);
        target.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        await router.StopAsync();
    }

    /// <summary>暂停旧场景失败时只卸载准备好的目标，不调用尚未开始的进入/退出回调。</summary>
    [Test]
    public async Task Push_WhenPreviousPauseFails_DoesNotExitUnenteredTarget()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        current.Setup(value => value.OnPauseAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("pause failed")));

        await Assert.CatchAsync<AggregateException>(() => router.PushAsync("target"));

        Assert.That(router.Current, Is.SameAs(current.Object));
        target.Verify(value => value.OnEnterAsync(It.IsAny<CancellationToken>()), Times.Never);
        target.Verify(value => value.OnExitAsync(It.IsAny<CancellationToken>()), Times.Never);
        target.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        current.Verify(value => value.OnResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        await router.StopAsync();
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
        target.Setup(value => value.OnExitAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("exit failed")));

        await Assert.CatchAsync<AggregateException>(() => router.PopAsync());

        Assert.That(router.Current, Is.SameAs(current.Object));
        target.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        current.Verify(value => value.OnResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        await router.StopAsync();
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
        guard.Setup(value => value.CanLeaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(() => ValueTask.FromResult(false));
        router.AddGuard(guard.Object);
        target.Setup(value => value.OnExitAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("exit failed")));

        var firstStop = router.StopAsync();
        var secondStop = router.StopAsync();
        await Assert.CatchAsync<AggregateException>(() => firstStop);

        Assert.Multiple(() =>
        {
            Assert.That(secondStop, Is.SameAs(firstStop));
            Assert.That(router.Current, Is.Null);
            Assert.That(router.Count, Is.Zero);
        });
        guard.Verify(value => value.CanLeaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        foreach (var scene in new[] { current, target })
        {
            scene.Verify(value => value.OnExitAsync(It.IsAny<CancellationToken>()), Times.Once);
            scene.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
            root.Verify(value => value.RemoveScene(scene.Object), Times.Once);
        }
        Assert.Throws<ObjectDisposedException>(() => { _ = router.PushAsync("target"); });
    }

    /// <summary>Cancellation during preparation reclaims only the new scene and keeps the old one active.</summary>
    [Test]
    public async Task Push_WhenCanceledDuringLoad_CleansTargetWithNonCancelableToken()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        target.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return new ValueTask(release.Task); });
        using var cancellation = new CancellationTokenSource();

        var navigation = router.PushAsync("target", cancellationToken: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        release.TrySetResult();
        await Assert.CatchAsync<OperationCanceledException>(() => navigation);

        Assert.That(router.Current, Is.SameAs(current.Object));
        target.Verify(value => value.OnUnloadAsync(CancellationToken.None), Times.Once);
        current.Verify(value => value.OnPauseAsync(It.IsAny<CancellationToken>()), Times.Never);
        root.Verify(value => value.RemoveScene(target.Object), Times.Once);
        await router.StopAsync();
    }

    /// <summary>The result identifies empty operations and refused transitions without requiring log inspection.</summary>
    [Test]
    public async Task Navigation_ReturnsAppliedRejectedAndNoOp()
    {
        var scene = CreateScene("scene");
        var (router, _) = CreateRouter(scene);
        Assert.That(await router.PopAsync(), Is.EqualTo(SceneOperationResult.NoOp));
        Assert.That(await router.PushAsync("scene"), Is.EqualTo(SceneOperationResult.Applied));
        Assert.That(await router.PushAsync("scene"), Is.EqualTo(SceneOperationResult.NoOp));
        var guard = CreateGuard();
        guard.Setup(value => value.CanLeaveAsync("scene", It.IsAny<CancellationToken>())).Returns(() => ValueTask.FromResult(false));
        router.AddGuard(guard.Object);
        Assert.That(await router.ClearAsync(), Is.EqualTo(SceneOperationResult.Rejected));
        await router.StopAsync();
    }

    /// <summary>Shutdown joins an accepted native callback before removing its scene.</summary>
    [Test]
    public async Task StopAsync_WaitsForInFlightNavigation()
    {
        var scene = CreateScene("scene");
        var (router, root) = CreateRouter(scene);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scene.Setup(value => value.OnEnterAsync(It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return new ValueTask(release.Task); });

        var navigation = router.PushAsync("scene");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = router.StopAsync();
        Assert.That(stop.IsCompleted, Is.False);
        root.Verify(value => value.RemoveScene(scene.Object), Times.Never);
        release.TrySetResult();
        await Task.WhenAll(navigation, stop).WaitAsync(TimeSpan.FromSeconds(5));

        root.Verify(value => value.RemoveScene(scene.Object), Times.Once);
        Assert.That(router.Count, Is.Zero);
    }

    /// <summary>Both enter and leave guards observe the navigation token without mutating the retained stack.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Guard_WhenCanceled_CompletesAndLeavesNavigationUsable(bool leaveGuard)
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var guard = CreateGuard();
        if (leaveGuard)
            guard.Setup(value => value.CanLeaveAsync("current", It.IsAny<CancellationToken>()))
                .Returns((string _, CancellationToken token) => WaitForCancellationAsync(entered, token));
        else
            guard.Setup(value => value.CanEnterAsync("target", It.IsAny<IRouteContext?>(), It.IsAny<CancellationToken>()))
                .Returns((string _, IRouteContext? _, CancellationToken token) => WaitForCancellationAsync(entered, token));
        router.AddGuard(guard.Object);
        using var cancellation = new CancellationTokenSource();

        var navigation = leaveGuard
            ? router.ReplaceAsync("target", cancellationToken: cancellation.Token)
            : router.PushAsync("target", cancellationToken: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        await Assert.CatchAsync<OperationCanceledException>(() => navigation.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));

        Assert.That(router.Current, Is.SameAs(current.Object));
        Assert.That(router.Count, Is.EqualTo(1));
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()), Times.Never);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        router.RemoveGuard(guard.Object);
        Assert.That(await router.PushAsync("target", cancellationToken: global::System.Threading.CancellationToken.None), Is.EqualTo(SceneOperationResult.Applied));
        await router.StopAsync();
    }

    /// <summary>A guard that has not completed retains operation ownership even after its token is canceled.</summary>
    [Test]
    public async Task StopAsync_WhenGuardIgnoresCancellation_JoinsCallbackBeforeCleanup()
    {
        var current = CreateScene("current");
        var target = CreateScene("target");
        var (router, root) = CreateRouter(current, target);
        await router.PushAsync("current");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var guard = CreateGuard();
        guard.Setup(value => value.CanEnterAsync("target", It.IsAny<IRouteContext?>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.TrySetResult(); return new ValueTask<bool>(release.Task); });
        router.AddGuard(guard.Object);
        using var cancellation = new CancellationTokenSource();

        var navigation = router.PushAsync("target", cancellationToken: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        var stop = router.StopAsync();
        Assert.That(navigation.IsCompleted, Is.False);
        Assert.That(stop.IsCompleted, Is.False);
        root.Verify(value => value.RemoveScene(current.Object), Times.Never);
        release.TrySetResult(true);
        await Assert.CatchAsync<OperationCanceledException>(() => navigation.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None));
        await stop.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);

        root.Verify(value => value.RemoveScene(current.Object), Times.Once);
        target.Verify(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Waits cooperatively so missing token propagation becomes a bounded failed regression.</summary>
    private static async ValueTask<bool> WaitForCancellationAsync(TaskCompletionSource entered, CancellationToken cancellationToken)
    {
        entered.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>创建只替代宿主和工厂的路由器，保留真实路由初始化与生命周期逻辑。</summary>
    private static (SceneRouter Router, Mock<ISceneRoot> Root) CreateRouter(params Mock<ISceneBehavior>[] scenes)
    {
        var factory = new Mock<ISceneFactory>();
        factory.Setup(value => value.Create(It.IsAny<string>()))
            .Returns((string key) => scenes.Single(scene => string.Equals(scene.Object.Key, key, StringComparison.Ordinal)).Object);
        var root = new Mock<ISceneRoot>();
        var router = new SceneRouter(factory.Object, root.Object, new InlineGameDispatcher());
        return (router, root);
    }

    /// <summary>创建默认全部生命周期成功的场景替身。</summary>
    private static Mock<ISceneBehavior> CreateScene(string key)
    {
        var scene = new Mock<ISceneBehavior>();
        scene.SetupGet(value => value.Key).Returns(key);
        scene.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnEnterAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnPauseAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnResumeAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnExitAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        scene.Setup(value => value.OnUnloadAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        return scene;
    }

    /// <summary>创建默认允许导航的守卫替身。</summary>
    private static Mock<IRouteGuard<ISceneBehavior>> CreateGuard()
    {
        var guard = new Mock<IRouteGuard<ISceneBehavior>>();
        guard.Setup(value => value.CanEnterAsync(It.IsAny<string>(), It.IsAny<IRouteContext?>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromResult(true));
        guard.Setup(value => value.CanLeaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(() => ValueTask.FromResult(true));
        return guard;
    }

}

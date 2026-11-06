// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.Architectures;

namespace GFramework.Godot.Tests.Architectures;

/// <summary>
///     使用纯托管队列验证宿主派发器的完成屏障，不宣称已执行真实 Godot 节点操作。
/// </summary>
[TestFixture]
public sealed class GodotMainThreadDispatcherTests
{
    /// <summary>
    ///     已在宿主线程时动作直接执行，不创建额外队列项。
    /// </summary>
    /// <returns>动作完成任务。</returns>
    [Test]
    public async Task InvokeAsync_ShouldExecuteInlineOnMainThread()
    {
        var calls = 0;
        var dispatcher = new GodotMainThreadDispatcher(
            () => true,
            _ => throw new InvalidOperationException("No deferred work expected."));

        await dispatcher.InvokeAsync(() => calls++);

        Assert.That(calls, Is.EqualTo(1));
    }

    /// <summary>
    ///     非宿主线程调用只有在队列动作实际完成后才结束。
    /// </summary>
    /// <returns>调度任务。</returns>
    [Test]
    public async Task InvokeAsync_ShouldAwaitDeferredAction()
    {
        var queued = new List<Action>();
        var calls = 0;
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);

        var pending = dispatcher.InvokeAsync(() => calls++);
        Assert.That(pending.IsCompleted, Is.False);
        Assert.That(calls, Is.Zero);
        queued.Single()();
        await pending;

        Assert.That(calls, Is.EqualTo(1));
    }

    /// <summary>
    ///     排队期间取消后，晚到的宿主回调不得再触碰节点。
    /// </summary>
    [Test]
    public async Task InvokeAsync_ShouldSkipCancelledQueuedAction()
    {
        using var cancellation = new CancellationTokenSource();
        var queued = new List<Action>();
        var calls = 0;
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);
        var pending = dispatcher.InvokeAsync(() => calls++, cancellation.Token);

        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(() => pending);
        queued.Single()();
        Assert.That(calls, Is.Zero);
    }

    /// <summary>
    ///     动作已经开始后，取消不能提前发布完成，否则资源清理会与节点动作并发。
    /// </summary>
    /// <returns>调度完成任务。</returns>
    [Test]
    public async Task InvokeAsync_ShouldWaitForRunningActionWhenCancellationArrives()
    {
        using var cancellation = new CancellationTokenSource();
        var queued = new List<Action>();
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);
        Task? pending = null;
        pending = dispatcher.InvokeAsync(() =>
        {
            cancellation.Cancel();
            Assert.That(pending!.IsCompleted, Is.False);
        }, cancellation.Token);

        queued.Single()();
        await pending;
    }

    /// <summary>
    ///     队列动作读取调用方的实例清理上下文，执行完成后恢复宿主帧自身的上下文。
    /// </summary>
    /// <returns>跨执行上下文的调度任务。</returns>
    [Test]
    public async Task InvokeAsync_ShouldFlowAndRestoreCallersExecutionContext()
    {
        var marker = new AsyncLocal<string?> { Value = "owning-cleanup" };
        var queued = new List<Action>();
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);
        string? observed = null;
        var pending = dispatcher.InvokeAsync(() => observed = marker.Value);

        marker.Value = "host-frame";
        queued.Single()();

        Assert.That(marker.Value, Is.EqualTo("host-frame"));
        await pending;
        Assert.That(observed, Is.EqualTo("owning-cleanup"));
    }

    /// <summary>
    ///     调用方显式抑制 ExecutionContext 流动时，派发器不重新制造已经撤销的环境状态。
    /// </summary>
    /// <returns>抑制上下文流动的调度任务。</returns>
    [Test]
    public async Task InvokeAsync_ShouldRespectSuppressedExecutionContextFlow()
    {
        var marker = new AsyncLocal<string?> { Value = "suppressed-caller" };
        var queued = new List<Action>();
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);
        string? observed = null;
        Task pending;
        using (ExecutionContext.SuppressFlow())
        {
            pending = dispatcher.InvokeAsync(() => observed = marker.Value);
        }

        marker.Value = "host-frame";
        queued.Single()();
        await pending;

        Assert.That(observed, Is.EqualTo("host-frame"));
    }

    /// <summary>
    ///     动作异常回传给等待者，而非落入未观察的后台任务。
    /// </summary>
    [Test]
    public async Task InvokeAsync_ShouldPropagateActionFailure()
    {
        var queued = new List<Action>();
        var dispatcher = new GodotMainThreadDispatcher(() => false, queued.Add);
        var pending = dispatcher.InvokeAsync(() => throw new InvalidOperationException("Expected node failure."));

        queued.Single()();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pending);

        Assert.That(exception!.Message, Is.EqualTo("Expected node failure."));
    }
}

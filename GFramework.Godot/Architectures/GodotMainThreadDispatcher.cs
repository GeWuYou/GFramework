// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Architectures;

/// <summary>
///     将短小的节点操作交给宿主主线程。取消只撤销尚未开始的操作；已经执行的操作必须完成，
///     以免调用方在节点操作仍运行时开始释放同一对象。
/// </summary>
/// <param name="isMainThread">宿主主线程判断。</param>
/// <param name="defer">把动作排入主线程的宿主队列。</param>
internal sealed class GodotMainThreadDispatcher(Func<bool> isMainThread, Action<Action> defer)
{
    /// <summary>
    ///     执行动作并观察结果，传播队列错误和动作异常。
    /// </summary>
    /// <param name="action">只在宿主线程执行的短操作。</param>
    /// <param name="cancellationToken">仅允许撤销尚未开始的调度。</param>
    /// <returns>动作执行结束或未开始时已取消的任务。</returns>
    public async Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (isMainThread())
        {
            action();
            return;
        }

        var pending = new PendingOperation(action, ExecutionContext.Capture(), cancellationToken);
        var registration = cancellationToken.Register(pending.Cancel);
        await using (registration.ConfigureAwait(false))
        {
            try
            {
                defer(pending.Execute);
            }
            catch (Exception exception)
            {
                pending.Fail(exception);
            }

            await pending.Completion.ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     用原子状态连接宿主队列和取消回调，避免取消完成与正在执行的动作竞态。
    /// </summary>
    /// <param name="action">被调度的动作。</param>
    /// <param name="executionContext">调用方的清理访问和重入标记；显式抑制流动时为空。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    private sealed class PendingOperation(
        Action action,
        ExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _state;

        /// <summary>
        ///     获取调度操作的完成任务。
        /// </summary>
        public Task Completion => _completion.Task;

        /// <summary>
        ///     仅队列尚未开始时发布取消完成；开始后的动作仍有同一完成屏障。
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                _completion.TrySetCanceled(cancellationToken);
        }

        /// <summary>
        ///     执行一次宿主动作，随后发布成功或原始异常。
        /// </summary>
        public void Execute()
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                return;

            try
            {
                if (executionContext is null)
                    ExecuteAction();
                else
                    ExecutionContext.Run(executionContext, static state => ((PendingOperation)state!).ExecuteAction(), this);

                _completion.TrySetResult();
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                _completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                _completion.TrySetException(exception);
            }
            finally
            {
                Interlocked.Exchange(ref _state, 2);
            }
        }

        /// <summary>
        ///     在调用方执行上下文内进入节点动作，使 AsyncLocal 的清理权限和重入守卫一致。
        /// </summary>
        private void ExecuteAction()
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
        }

        /// <summary>
        ///     把队列拒绝错误交给正在等待的调用方。
        /// </summary>
        /// <param name="exception">宿主调度器错误。</param>
        public void Fail(Exception exception)
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                _completion.TrySetException(exception);
        }
    }
}

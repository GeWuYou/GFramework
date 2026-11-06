// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     顺序执行当前实例的阶段钩子和监听器。监听器在 provider 存活时快照，
///     因而终态广播不需要重新解析已释放容器，也不会把进程级注册表当作运行态。
/// </summary>
internal sealed class ArchitecturePhaseCoordinator(
    IArchitecture architecture,
    IArchitectureConfiguration configuration,
    ILogger logger)
{
    private readonly List<IArchitectureLifecycleHook> _hooks = [];
    private IReadOnlyList<IArchitecturePhaseListener> _listeners = [];

    /// <summary>当前阶段；先更新状态再广播，使失败的回调也不能隐藏已开始的阶段。</summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>在配置阶段登记钩子，外层架构负责注册窗口约束。</summary>
    /// <param name="hook">当前实例的钩子。</param>
    /// <returns>传入钩子。</returns>
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
        return hook;
    }

    /// <summary>在组件全部激活后确定阶段消费者，按引用去重。</summary>
    /// <param name="listeners">显式组件及容器中的额外监听器。</param>
    public void SetListeners(IEnumerable<IArchitecturePhaseListener> listeners)
    {
        _listeners = listeners.Distinct<IArchitecturePhaseListener>(ReferenceEqualityComparer.Instance).ToArray();
    }

    /// <summary>进入阶段，等待异步钩子；同步模式遇到异步钩子明确拒绝。</summary>
    /// <param name="next">目标阶段。</param>
    /// <param name="asyncMode">是否允许异步钩子。</param>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>全部阶段观察者完成。</returns>
    public async ValueTask EnterPhaseAsync(
        ArchitecturePhase next,
        bool asyncMode,
        CancellationToken cancellationToken = default)
    {
        ValidatePhaseTransition(next);
        CurrentPhase = next;
        logger.LogDebug($"Architecture phase: {next}");
        foreach (var hook in _hooks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hook is IAsyncArchitectureLifecycleHook asyncHook)
            {
                if (!asyncMode)
                    throw new InvalidOperationException("An asynchronous lifecycle hook requires InitializeAsync().");
                await asyncHook.OnPhaseAsync(next, architecture, cancellationToken).ConfigureAwait(false);
            }
            else
                hook.OnPhase(next, architecture);
        }
        foreach (var listener in _listeners)
            listener.OnArchitecturePhase(next);
    }

    /// <summary>失败和停止可从任意非终态进入；正常阶段仍按既有顺序验证。</summary>
    /// <param name="next">下一阶段。</param>
    private void ValidatePhaseTransition(ArchitecturePhase next)
    {
        if (!configuration.ArchitectureProperties.StrictPhaseValidation)
            return;
        if (next == ArchitecturePhase.FailedInitialization ||
            (next == ArchitecturePhase.Destroying && CurrentPhase != ArchitecturePhase.Destroyed))
            return;
        if (ArchitectureConstants.PhaseTransitions.TryGetValue(CurrentPhase, out var allowed) && allowed.Contains(next))
            return;
        throw new InvalidOperationException($"Invalid phase transition: {CurrentPhase} -> {next}");
    }
}

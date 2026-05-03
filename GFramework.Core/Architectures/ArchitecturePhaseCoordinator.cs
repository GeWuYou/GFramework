// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     负责架构阶段流转的验证与通知。
///     该类型集中管理阶段值、生命周期钩子和阶段监听器，避免 <see cref="ArchitectureLifecycle" />
///     同时承担阶段广播与组件初始化队列管理两类职责。
/// </summary>
internal sealed class ArchitecturePhaseCoordinator(
    IArchitecture architecture,
    IArchitectureConfiguration configuration,
    IArchitectureServices services,
    ILogger logger)
{
    private readonly List<IArchitectureLifecycleHook> _lifecycleHooks = [];

    /// <summary>
    ///     获取当前架构阶段。
    /// </summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>
    ///     注册一个生命周期钩子。
    ///     就绪后是否允许追加注册由架构配置控制，以保证阶段回调的一致性。
    /// </summary>
    /// <param name="hook">要注册的生命周期钩子。</param>
    /// <returns>原样返回注册的钩子实例，便于链式调用或测试断言。</returns>
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !configuration.ArchitectureProperties.AllowLateRegistration)
            throw new InvalidOperationException("Cannot register lifecycle hook after architecture is Ready");

        _lifecycleHooks.Add(hook);
        return hook;
    }

    /// <summary>
    ///     进入指定阶段并广播给所有阶段消费者。
    ///     顺序保持为“更新阶段值 → 生命周期钩子 → 容器中的阶段监听器”，
    ///     以保证框架扩展与运行时组件看到一致的阶段视图。
    /// </summary>
    /// <param name="next">目标阶段。</param>
    public void EnterPhase(ArchitecturePhase next)
    {
        ValidatePhaseTransition(next);

        var previousPhase = CurrentPhase;
        CurrentPhase = next;

        if (previousPhase != next)
            logger.Info($"Architecture phase changed: {previousPhase} -> {next}");

        NotifyLifecycleHooks(next);
        NotifyPhaseListeners(next);
    }

    /// <summary>
    ///     根据配置验证阶段迁移是否合法。
    ///     在关闭严格校验时直接放行，以兼容对阶段流转有特殊需求的宿主。
    /// </summary>
    /// <param name="next">目标阶段。</param>
    /// <exception cref="InvalidOperationException">当迁移不在允许集合中时抛出。</exception>
    private void ValidatePhaseTransition(ArchitecturePhase next)
    {
        if (!configuration.ArchitectureProperties.StrictPhaseValidation)
            return;

        if (next == ArchitecturePhase.FailedInitialization)
            return;

        if (ArchitectureConstants.PhaseTransitions.TryGetValue(CurrentPhase, out var allowed) && allowed.Contains(next))
            return;

        var errorMessage = $"Invalid phase transition: {CurrentPhase} -> {next}";
        logger.Fatal(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    ///     通知所有生命周期钩子当前阶段已变更。
    ///     生命周期钩子通常承载注册表装配等框架扩展逻辑，因此优先于普通阶段监听器执行。
    /// </summary>
    /// <param name="phase">当前阶段。</param>
    private void NotifyLifecycleHooks(ArchitecturePhase phase)
    {
        foreach (var hook in _lifecycleHooks)
        {
            hook.OnPhase(phase, architecture);
            logger.Trace($"Notifying lifecycle hook {hook.GetType().Name} of phase {phase}");
        }
    }

    /// <summary>
    ///     通知容器中的阶段监听器当前阶段已变更。
    ///     这些对象通常是系统、模型或工具本身，依赖容器解析保证通知范围与运行时实例一致。
    /// </summary>
    /// <param name="phase">当前阶段。</param>
    private void NotifyPhaseListeners(ArchitecturePhase phase)
    {
        foreach (var listener in services.Container.GetAll<IArchitecturePhaseListener>())
        {
            logger.Trace($"Notifying phase-aware object {listener.GetType().Name} of phase change to {phase}");
            listener.OnArchitecturePhase(phase);
        }
    }
}

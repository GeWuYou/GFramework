// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     统一处理架构内可销毁对象的登记与释放。
///     该类型封装逆序销毁、异常隔离和服务模块清理规则，
///     让 <see cref="ArchitectureLifecycle" /> 可以专注于初始化流程本身。
/// </summary>
internal sealed class ArchitectureDisposer(
    IArchitectureServices services,
    ILogger logger)
{
    /// <summary>
    ///     保留注册顺序的可销毁对象列表。
    ///     销毁时按逆序遍历，以尽量匹配组件间的依赖方向。
    /// </summary>
    private readonly List<object> _disposables = [];

    /// <summary>
    ///     用于去重的可销毁对象集合。
    /// </summary>
    private readonly HashSet<object> _disposableSet = [];

    /// <summary>
    ///     注册一个需要参与架构销毁流程的对象。
    ///     只有实现 <see cref="IDestroyable" /> 或 <see cref="IAsyncDestroyable" /> 的对象会被跟踪。
    /// </summary>
    /// <param name="component">待检查的组件实例。</param>
    public void Register(object component)
    {
        if (component is not (IDestroyable or IAsyncDestroyable))
            return;

        if (!_disposableSet.Add(component))
            return;

        _disposables.Add(component);
        logger.Trace($"Registered {component.GetType().Name} for destruction");
    }

    /// <summary>
    ///     执行架构销毁流程。
    ///     该方法会根据当前阶段决定是否进入 Destroying/Destroyed 阶段，并负责服务模块与容器清理。
    /// </summary>
    /// <param name="currentPhase">销毁开始前的架构阶段。</param>
    /// <param name="enterPhase">用于推进架构阶段的回调。</param>
    public async ValueTask DestroyAsync(ArchitecturePhase currentPhase, Action<ArchitecturePhase> enterPhase)
    {
        if (currentPhase is ArchitecturePhase.Destroying or ArchitecturePhase.Destroyed)
        {
            logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        if (currentPhase == ArchitecturePhase.None)
        {
            logger.Debug("Architecture destroy called but never initialized, cleaning up registered components");
            await CleanupComponentsAsync().ConfigureAwait(false);
            return;
        }

        logger.Info("Starting architecture destruction");
        enterPhase(ArchitecturePhase.Destroying);

        await CleanupComponentsAsync().ConfigureAwait(false);
        await services.ModuleManager.DestroyAllAsync().ConfigureAwait(false);

        // Destroyed 广播依赖容器中的阶段监听器，必须在清空容器前完成。
        enterPhase(ArchitecturePhase.Destroyed);
        services.Container.Clear();
        logger.Info("Architecture destruction completed");
    }

    /// <summary>
    ///     逆序销毁当前已注册的所有可销毁组件。
    ///     单个组件失败不会中断后续清理，避免在销毁阶段留下半清理状态。
    /// </summary>
    private async ValueTask CleanupComponentsAsync()
    {
        logger.Info($"Destroying {_disposables.Count} disposable components");

        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            var component = _disposables[i];

            try
            {
                logger.Debug($"Destroying component: {component.GetType().Name}");

                if (component is IAsyncDestroyable asyncDestroyable)
                {
                    await asyncDestroyable.DestroyAsync().ConfigureAwait(false);
                }
                else if (component is IDestroyable destroyable)
                {
                    destroyable.Destroy();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error destroying {component.GetType().Name}", ex);
            }
        }

        _disposables.Clear();
        _disposableSet.Clear();
    }
}

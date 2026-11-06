// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Logging;
using GFramework.Core.Rule;

namespace GFramework.Core.Systems;

/// <summary>
///     抽象系统基类，实现系统接口的基本功能
///     提供架构关联、初始化和销毁机制
/// </summary>
public abstract class AbstractSystem : ContextAwareBase, ISystem
{
    private ILogger _logger = null!;

    /// <summary>
    ///     系统初始化方法，调用抽象初始化方法
    /// </summary>
    public void Initialize()
    {
        var name = GetType().Name;
        _logger = CreateInstanceLogger(name);
        _logger.Debug($"Initializing system: {name}");

        OnInit();

        _logger.Info($"System initialized: {name}");
    }

    /// <summary>
    ///     系统销毁方法，调用抽象销毁方法
    /// </summary>
    public void Destroy()
    {
        _logger ??= CreateInstanceLogger(GetType().Name);
        _logger.Debug($"Destroying system: {GetType().Name}");

        OnDestroy();

        _logger.Info($"System destroyed: {GetType().Name}");
    }

    /// <summary>
    ///     处理架构阶段事件的虚拟方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    ///     抽象销毁方法，由子类实现具体的资源清理逻辑
    /// </summary>
    protected virtual void OnDestroy()
    {
    }
    /// <summary>绑定架构时使用其实例工厂，独立使用时创建本地工厂，不读取进程级 resolver。</summary>
    /// <param name="name">日志名称。</param>
    /// <returns>当前组件日志。</returns>
    private ILogger CreateInstanceLogger(string name)
    {
        var factory = Context == null
            ? new ConsoleLoggerFactoryProvider()
            : Context.GetService<ILoggerFactoryProvider>();
        return factory.CreateLogger(name);
    }

}
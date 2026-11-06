// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    /// <summary>当前系统实例日志的底层缓存。</summary>
    private ILogger? _loggerInstance;

    /// <summary>
    ///     当前系统实例日志；首次访问时经由上下文或空输出解析并缓存，
    ///     不依赖初始化时机，也不读取进程级状态。
    /// </summary>
    protected ILogger _logger => _loggerInstance ??= CreateInstanceLogger(GetType().Name);

    /// <summary>
    ///     系统初始化方法，调用抽象初始化方法
    /// </summary>
    public void Initialize()
    {
        var name = GetType().Name;
        _logger.LogDebug($"Initializing system: {name}");

        OnInit();

        _logger.LogInformation($"System initialized: {name}");
    }

    /// <summary>
    ///     系统销毁方法，调用抽象销毁方法
    /// </summary>
    public void Destroy()
    {
        _logger.LogDebug($"Destroying system: {GetType().Name}");

        OnDestroy();

        _logger.LogInformation($"System destroyed: {GetType().Name}");
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
    /// <summary>绑定架构时通过其实例工厂解析标准日志工厂；未绑定时使用空输出。</summary>
    /// <param name="name">日志类别名。</param>
    /// <returns>当前组件日志。</returns>
    protected ILogger CreateInstanceLogger(string name)
    {
        var factory = Context == null
            ? NullLoggerFactory.Instance
            : Context.GetService<ILoggerFactory>();
        return factory.CreateLogger(name);
    }

}
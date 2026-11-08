// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Ioc;
using GFramework.Core.Services;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构服务类，提供依赖注入容器、事件系统等核心服务。
///     该类负责管理架构运行所需的核心组件，并提供统一的服务访问接口。
/// </summary>
public class ArchitectureServices : IArchitectureServices
{
    private readonly IServiceModuleManager _moduleManager;
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     构造函数，初始化架构服务。
    ///     初始化依赖注入容器，并创建事件总线的实例，然后将这些服务注册到容器中。
    /// </summary>
    /// <param name="loggerFactory">当前实例的标准日志工厂；缺省使用空输出工厂。</param>
    public ArchitectureServices(ILoggerFactory? loggerFactory = null)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;
        Container = new MicrosoftDiContainer(loggerFactory: factory);
        _moduleManager = new ServiceModuleManager(factory);
    }

    /// <summary>
    ///     获取服务模块管理器实例。
    ///     服务模块管理器用于管理架构中的服务模块，支持模块的动态加载和卸载。
    /// </summary>
    public IServiceModuleManager ModuleManager => _moduleManager;

    /// <summary>
    ///     获取依赖注入容器。
    ///     该容器用于管理架构中所有服务的生命周期和依赖关系。
    /// </summary>
    public IIocContainer Container
    {
        get;
    }

    /// <summary>
    ///     获取事件总线实例。
    ///     事件总线用于在架构中发布和订阅事件，实现组件间的松耦合通信。
    /// </summary>
    public IEventBus EventBus => Container.Get<IEventBus>()!;

    /// <summary>
    ///     设置架构上下文。
    /// </summary>
    /// <param name="context">要设置的架构上下文实例。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
        Container.SetContext(context);
    }

    /// <summary>
    ///     获取当前架构上下文。
    /// </summary>
    /// <returns>当前的架构上下文实例。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

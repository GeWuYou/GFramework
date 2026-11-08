// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;

namespace GFramework.Cqrs;

/// <summary>
///     CQRS runtime 架构集成模块。
///     该模块实现 <see cref="GFramework.Core.Abstractions.Architectures.IServiceModule" />，
///     把默认请求分发器接入当前架构的服务集合；仅在用户显式安装时生效，
///     Core 不再默认内置 CQRS 基础设施。
/// </summary>
/// <param name="handlerAssemblies">
///     模块安装时即扫描接入的处理器程序集；
///     缺省只覆盖宿主架构所在程序集之外显式传入的集合，更多程序集可在
///     <c>ConfigureServices</c> 中通过 <see cref="CqrsServiceCollectionExtensions.AddCqrsHandlersFromAssembly" /> 补充。
/// </param>
public sealed class CqrsRuntimeModule(params Assembly[] handlerAssemblies) : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(CqrsRuntimeModule);

    /// <summary>
    ///     获取模块优先级。
    ///     CQRS runtime 需要先于用户初始化阶段可用，因此放在基础总线模块之后注册。
    /// </summary>
    public int Priority => 15;

    /// <summary>
    ///     获取模块启用状态，默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     在配置阶段接入默认 CQRS runtime 与声明的处理器程序集。
    ///     所有注册只写入服务描述，不在 provider 构建前解析服务。
    ///     日志沿用容器已登记的标准 <see cref="Microsoft.Extensions.Logging.ILoggerFactory" />；
    ///     容器未提供时 CQRS 不注册任何日志基础设施，诊断日志回退到空实现。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var services = container.GetServicesUnsafe;
        // 注册阶段可能持有容器写锁；只登记描述，日志在正式 provider 构建后按标准 DI 解析。
        services.AddCqrsRuntime();
        if (handlerAssemblies.Length > 0)
        {
            services.AddCqrsHandlersFromAssemblies(handlerAssemblies);
        }
    }

    /// <summary>
    ///     初始化模块。CQRS runtime 在首次分发时由标准 provider 惰性构建，无需额外启动动作。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块。runtime 是 provider 拥有的单例，资源随根 provider 释放，无需额外清理。
    /// </summary>
    /// <returns>已完成的值任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Query;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     查询执行器模块，用于注册和管理查询执行器服务。
///     该模块负责将查询执行器注册到依赖注入容器中，并提供初始化和销毁功能。
/// </summary>
public sealed class QueryExecutorModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(QueryExecutorModule);

    /// <summary>
    ///     获取模块优先级，数值越小优先级越高。
    /// </summary>
    public int Priority => 30;

    /// <summary>
    ///     获取模块启用状态，始终返回 true 表示该模块默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册查询执行器到依赖注入容器。
    ///     创建查询执行器实例并将其注册为多例服务。
    /// </summary>
    /// <param name="container">依赖注入容器实例。</param>
    public void Register(IIocContainer container)
    {
        container.RegisterPlurality(new QueryExecutor());
    }

    /// <summary>
    ///     初始化模块。
    ///     当前实现为空，因为查询执行器无需额外初始化逻辑。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块。
    ///     当前实现为空，因为查询执行器无需特殊销毁逻辑。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}
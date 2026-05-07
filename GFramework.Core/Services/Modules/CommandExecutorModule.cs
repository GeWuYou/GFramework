// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Command;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     命令执行器模块，用于注册和管理命令执行器服务。
///     该模块负责将命令执行器注册到依赖注入容器中，并在销毁时释放相关资源。
/// </summary>
public sealed class CommandExecutorModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(CommandExecutorModule);

    /// <summary>
    ///     获取模块优先级，数值越小优先级越高。
    /// </summary>
    public int Priority => 20;

    /// <summary>
    ///     获取模块启用状态，始终返回 true 表示该模块默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册命令执行器到依赖注入容器。
    ///     创建命令执行器实例并将其注册为多例服务。
    /// </summary>
    /// <param name="container">承载命令执行器与 CQRS runtime 的依赖注入容器实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="container" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     容器中尚未注册唯一的 <see cref="ICqrsRuntime" /> 实例，无法构建统一 runtime 版本的命令执行器。
    /// </exception>
    /// <remarks>
    ///     该模块会在注册阶段立即解析 <see cref="ICqrsRuntime" />，因此
    ///     <see cref="CqrsRuntimeModule" /> 必须先于当前模块完成注册。
    /// </remarks>
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        container.RegisterPlurality(new CommandExecutor(container.GetRequired<ICqrsRuntime>()));
    }

    /// <summary>
    ///     初始化模块。
    ///     当前实现为空，因为命令执行器无需额外初始化逻辑。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块并释放资源。
    ///     将命令执行器引用置空以允许垃圾回收。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}

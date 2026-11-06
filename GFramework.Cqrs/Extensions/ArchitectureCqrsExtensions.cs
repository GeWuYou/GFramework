// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs.Extensions;

/// <summary>
///     提供 <see cref="IArchitecture" /> 的 CQRS 便捷接线扩展。
///     这是 Architecture 与 CQRS runtime 之间的明确集成边界：Core 不再默认内置 CQRS，
///     需要该能力的架构在配置阶段显式安装。
/// </summary>
public static class ArchitectureCqrsExtensions
{
    /// <summary>
    ///     为当前架构安装 <see cref="CqrsRuntimeModule" />。
    /// </summary>
    /// <param name="architecture">目标架构。</param>
    /// <param name="additionalHandlerAssemblies">
    ///     除宿主架构所在程序集之外需要接入的处理器程序集；
    ///     处理器来自其他程序集时，也可以在 <c>ConfigureServices</c> 中改用
    ///     <see cref="CqrsServiceCollectionExtensions.AddCqrsHandlersFromAssembly" />。
    /// </param>
    /// <returns>已安装的模块实例。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="architecture" /> 为 <see langword="null" />。</exception>
    public static CqrsRuntimeModule UseCqrsRuntime(
        this IArchitecture architecture,
        params Assembly[] additionalHandlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(architecture);

        // 与既有启动语义保持一致：默认扫描宿主架构所在程序集，
        // 使生成注册表或反射扫描能覆盖随架构一起发布的处理器。
        var handlerAssemblies = new List<Assembly>(additionalHandlerAssemblies.Length + 1)
        {
            architecture.GetType().Assembly
        };
        handlerAssemblies.AddRange(additionalHandlerAssemblies);

        var module = new CqrsRuntimeModule([.. handlerAssemblies]);
        architecture.RegisterServiceModule(module);
        return module;
    }
}

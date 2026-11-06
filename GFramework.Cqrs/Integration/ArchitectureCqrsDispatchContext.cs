// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs;

/// <summary>
///     架构集成专用的 CQRS 分发上下文。
///     该类型把宿主的 <see cref="IArchitectureContext" /> 绑定为
///     <see cref="ICqrsContext" />，使 dispatcher 在分发期间能把架构上下文
///     注入到声明 <c>IContextAware</c> 的处理器与 pipeline 行为。
/// </summary>
/// <param name="architectureContext">当前架构上下文；分发期间保持存活。</param>
/// <remarks>
///     该类型属于明确的架构集成边界：独立使用 CQRS runtime 时不创建它，
///     <c>IContextAware</c> 处理器应改为构造函数注入获取依赖。
/// </remarks>
public sealed class ArchitectureCqrsDispatchContext(IArchitectureContext architectureContext) : ICqrsContext
{
    /// <summary>
    ///     获取当前分发绑定的架构上下文。
    /// </summary>
    public IArchitectureContext ArchitectureContext { get; } =
        architectureContext ?? throw new ArgumentNullException(nameof(architectureContext));
}

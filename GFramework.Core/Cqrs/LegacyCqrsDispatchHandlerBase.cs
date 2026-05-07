// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Rule;

namespace GFramework.Core.Cqrs;

/// <summary>
///     为 legacy Core CQRS bridge handler 提供共享的上下文注入辅助逻辑。
/// </summary>
internal abstract class LegacyCqrsDispatchHandlerBase : ContextAwareBase
{
    /// <summary>
    ///     在执行 legacy 命令或查询前，把当前架构上下文显式注入给支持 <see cref="IContextAware" /> 的目标对象。
    /// </summary>
    /// <param name="target">即将执行的 legacy 目标对象。</param>
    /// <exception cref="ArgumentNullException"><paramref name="target" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     目标对象实现了 <see cref="IContextAware" />，但当前 handler 还没有可用的架构上下文。
    /// </exception>
    protected void PrepareTarget(object target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target is IContextAware contextAware)
        {
            var context = Context ?? throw new InvalidOperationException(
                "Legacy CQRS bridge handler requires an active architecture context before executing a context-aware target.");
            contextAware.SetContext(context);
        }
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Cqrs.Cqrs;

/// <summary>
///     为 CQRS 处理器提供最小化的上下文感知基类实现。
/// </summary>
/// <remarks>
///     该基类只承接 CQRS runtime 在分发前注入的 <see cref="IArchitectureContext" />，
///     不再像 <c>ContextAwareBase</c> 那样回退到 <c>GameContext</c> 全局查找。
///     这样可以让 <c>GFramework.Cqrs</c> 保持对 <c>GFramework.Core</c> 运行时实现的零依赖，
///     同时在处理器被错误地脱离 dispatcher 使用时以显式异常快速失败。
/// </remarks>
public abstract class CqrsContextAwareHandlerBase : IContextAware
{
    private IArchitectureContext? _context;

    /// <summary>
    ///     获取当前分发周期内已注入的架构上下文。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     当前处理器尚未被 CQRS runtime 注入上下文。
    /// </exception>
    protected IArchitectureContext Context => _context ?? throw new InvalidOperationException(
        "The CQRS handler context has not been initialized. Ensure the handler is invoked through the CQRS runtime.");

    /// <summary>
    ///     由 runtime 在分发前注入当前架构上下文。
    /// </summary>
    /// <param name="context">当前架构上下文。</param>
    void IContextAware.SetContext(IArchitectureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        OnContextReady();
    }

    /// <summary>
    ///     获取当前处理器实例已绑定的架构上下文。
    /// </summary>
    /// <returns>当前分发周期内的架构上下文。</returns>
    IArchitectureContext IContextAware.GetContext()
    {
        return Context;
    }

    /// <summary>
    ///     当上下文注入完成后执行额外初始化。
    /// </summary>
    /// <remarks>
    ///     该钩子保留与旧 <c>ContextAwareBase</c> 相近的扩展点，
    ///     便于处理器在迁移后继续承接分发前的派生类初始化逻辑。
    /// </remarks>
    protected virtual void OnContextReady()
    {
    }
}

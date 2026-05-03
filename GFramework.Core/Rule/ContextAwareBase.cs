// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;

namespace GFramework.Core.Rule;

/// <summary>
///     上下文感知基类，实现了 <see cref="IContextAware" />，为需要感知架构上下文的类提供基础实现。
/// </summary>
/// <remarks>
///     该基类面向手动继承场景，使用简单的实例字段缓存上下文，不提供额外同步保护。
///     与 <c>ContextAwareGenerator</c> 生成的实现不同，它不会维护静态共享的
///     <see cref="IArchitectureContextProvider" />，也不会在 <see cref="IContextAware.SetContext" /> /
///     <see cref="IContextAware.GetContext" /> 上加锁。
///     若调用方需要跨实例共享 provider、在惰性初始化期间协调 provider 切换，或希望生成代码自动补齐这些约束，应优先使用
///     <c>[ContextAware]</c> 生成路径；若场景本身由框架主线程驱动，且只需要最小化的实例级上下文缓存，则该基类更直接。
/// </remarks>
public abstract class ContextAwareBase : IContextAware
{
    /// <summary>
    ///     获取或设置当前实例缓存的架构上下文。
    /// </summary>
    /// <remarks>
    ///     该属性不执行同步；调用方应保证对同一实例的访问遵循其自身线程模型。
    /// </remarks>
    protected IArchitectureContext? Context { get; set; }

    /// <summary>
    ///     设置架构上下文的实现方法，由框架调用。
    /// </summary>
    /// <param name="context">要设置的架构上下文实例。</param>
    /// <remarks>
    ///     该实现只做简单赋值，然后调用 <see cref="OnContextReady" />；不与 <see cref="IContextAware.GetContext" /> 共享锁。
    /// </remarks>
    void IContextAware.SetContext(IArchitectureContext context)
    {
        Context = context;
        OnContextReady();
    }

    /// <summary>
    ///     获取架构上下文。
    /// </summary>
    /// <returns>当前架构上下文对象。</returns>
    /// <remarks>
    ///     当 <see cref="Context" /> 为空时，该实现会直接回退到 <see cref="GameContext.GetFirstArchitectureContext" />。
    ///     该回退过程不执行额外同步，也不支持替换 provider；如需这些能力，请改用生成的 ContextAware 实现。
    /// </remarks>
    IArchitectureContext IContextAware.GetContext()
    {
        Context ??= GameContext.GetFirstArchitectureContext();
        return Context;
    }

    /// <summary>
    ///     当上下文准备就绪时调用的虚方法，子类可以重写此方法来执行上下文相关的初始化逻辑。
    /// </summary>
    protected virtual void OnContextReady()
    {
    }
}

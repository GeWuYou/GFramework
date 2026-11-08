// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Rule;

/// <summary>
///     为手动继承的组件提供显式、实例级的架构上下文绑定。
/// </summary>
/// <remarks>
///     组件由所属架构通过 IContextAware.SetContext 注入上下文，未绑定时访问会明确失败。
///     该基类不读取全局状态，也不负责跨线程同步；调用方应遵循组件自身的线程模型。
///     需要同步绑定与读取时，可使用 ContextAwareGenerator 生成的实例级锁实现。
/// </remarks>
public abstract class ContextAwareBase : IContextAware
{
    /// <summary>
    ///     获取或设置当前实例绑定的上下文；尚未绑定时为 null。
    /// </summary>
    /// <remarks>该属性不执行同步，派生类应通过自身线程模型约束访问。</remarks>
    protected IArchitectureContext? Context
    {
        get; set;
    }

    /// <summary>
    ///     为当前实例绑定上下文，然后执行上下文就绪回调。
    /// </summary>
    /// <param name="context">当前实例所属的架构上下文。</param>
    /// <exception cref="ArgumentNullException">context 为 null。</exception>
    void IContextAware.SetContext(IArchitectureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        OnContextReady();
    }

    /// <summary>
    ///     返回当前实例已经绑定的上下文，不回退到全局上下文。
    /// </summary>
    /// <returns>当前实例绑定的上下文。</returns>
    /// <exception cref="InvalidOperationException">当前实例尚未绑定上下文。</exception>
    IArchitectureContext IContextAware.GetContext()
    {
        return Context ?? throw new InvalidOperationException(
            "Architecture context has not been initialized. Bind this instance through IContextAware.SetContext before accessing it.");
    }

    /// <summary>
    ///     在当前实例完成显式上下文绑定之后执行派生类的初始化逻辑。
    /// </summary>
    protected virtual void OnContextReady()
    {
    }
}

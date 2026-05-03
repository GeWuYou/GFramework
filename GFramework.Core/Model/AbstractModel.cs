// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Rule;

namespace GFramework.Core.Model;

/// <summary>
///     抽象模型基类，实现IModel接口，提供模型的基本架构支持
/// </summary>
public abstract class AbstractModel : ContextAwareBase, IModel
{
    /// <summary>
    ///     初始化模型，调用抽象方法OnInit执行具体初始化逻辑
    /// </summary>
    void IInitializable.Initialize()
    {
        OnInit();
    }

    /// <summary>
    ///     处理架构阶段事件的虚拟方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }


    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}
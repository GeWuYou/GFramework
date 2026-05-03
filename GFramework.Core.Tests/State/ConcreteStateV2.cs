// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     具体状态实现类V2版本，用于测试状态的基本功能。
/// </summary>
public sealed class ConcreteStateV2 : IState
{
    /// <summary>
    ///     获取或设置是否允许转换。
    /// </summary>
    public bool AllowTransitions { get; set; } = true;

    /// <summary>
    ///     获取进入状态是否被调用的标志。
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    ///     获取退出状态是否被调用的标志。
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    ///     获取进入此状态的来源状态。
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    ///     获取从此状态退出的目标状态。
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    ///     获取或设置转换到目标状态时执行的动作。
    /// </summary>
    public Action<IState>? CanTransitionToAction { get; set; }

    /// <summary>
    ///     进入当前状态时调用的方法。
    /// </summary>
    /// <param name="from">从哪个状态进入。</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    /// <summary>
    ///     退出当前状态时调用的方法。
    /// </summary>
    /// <param name="to">退出到哪个状态。</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    /// <summary>
    ///     判断是否可以转换到目标状态。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>如果可以转换则返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public bool CanTransitionTo(IState target)
    {
        CanTransitionToAction?.Invoke(target);
        return AllowTransitions;
    }
}

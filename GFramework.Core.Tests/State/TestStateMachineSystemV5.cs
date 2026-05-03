// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GFramework.Core.Abstractions.State;
using GFramework.Core.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为 <see cref="StateMachineSystemTests" /> 提供可观察内部状态注册表的测试状态机实现。
/// </summary>
public class TestStateMachineSystemV5 : StateMachineSystem
{
    /// <summary>
    ///     获取状态机当前维护的状态实例映射，供测试断言注册结果使用。
    /// </summary>
    /// <returns>状态类型到状态实例的只读视图。</returns>
    public IReadOnlyDictionary<Type, IState> GetStates()
    {
        return States as IReadOnlyDictionary<Type, IState> ?? new ReadOnlyDictionary<Type, IState>(States);
    }
}

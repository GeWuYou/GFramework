// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;

namespace GFramework.Core.State;

/// <summary>
///     表示状态变更事件的数据类
/// </summary>
public sealed class StateChangedEvent
{
    /// <summary>
    ///     获取变更前的旧状态
    /// </summary>
    public IState? OldState { get; init; }

    /// <summary>
    ///     获取变更后的新状态
    /// </summary>
    public IState? NewState { get; init; }
}
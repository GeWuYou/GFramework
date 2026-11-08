// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述一组动作绑定的可持久化快照。
/// </summary>
/// <remarks>
///     初始化一个输入绑定快照。
/// </remarks>
/// <param name="actions">动作绑定集合。</param>
public sealed class InputBindingSnapshot(IReadOnlyList<InputActionBinding> actions)
{

    /// <summary>
    ///     获取动作绑定集合。
    /// </summary>
    public IReadOnlyList<InputActionBinding> Actions { get; } = actions ?? [];
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     描述一个逻辑动作当前持有的绑定集合。
/// </summary>
public sealed class InputActionBinding
{
    /// <summary>
    ///     初始化一个动作绑定快照。
    /// </summary>
    /// <param name="actionName">动作名称。</param>
    /// <param name="bindings">当前绑定列表。</param>
    /// <exception cref="ArgumentException">当 <paramref name="actionName" /> 为空时抛出。</exception>
    public InputActionBinding(string actionName, IReadOnlyList<InputBindingDescriptor> bindings)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException("Action name cannot be null or whitespace.", nameof(actionName));
        }

        ActionName = actionName;
        Bindings = bindings ?? [];
    }

    /// <summary>
    ///     获取动作名称。
    /// </summary>
    public string ActionName
    {
        get;
    }

    /// <summary>
    ///     获取当前绑定列表。
    /// </summary>
    public IReadOnlyList<InputBindingDescriptor> Bindings
    {
        get;
    }
}

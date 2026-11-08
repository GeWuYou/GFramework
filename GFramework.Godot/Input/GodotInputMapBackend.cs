// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;

namespace GFramework.Godot.Input;

/// <summary>
///     基于 Godot `InputMap` 的默认后端实现。
/// </summary>
internal sealed class GodotInputMapBackend : IGodotInputMapBackend
{
    private readonly Dictionary<string, List<InputBindingDescriptor>> _defaults;

    /// <summary>
    ///     初始化后端，并捕获当前 `InputMap` 作为默认快照。
    /// </summary>
    public GodotInputMapBackend()
    {
        _defaults = GetActionNames().ToDictionary(
            static actionName => actionName,
            actionName => GetBindings(actionName).ToList(),
            StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetActionNames()
    {
        return [.. InputMap.GetActions().Select(static action => action.ToString())];
    }

    /// <inheritdoc />
    public IReadOnlyList<InputBindingDescriptor> GetBindings(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        if (!InputMap.HasAction(actionName))
        {
            return [];
        }

        var bindings = new List<InputBindingDescriptor>();
        foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
        {
            if (GodotInputBindingCodec.TryCreateBinding(inputEvent, out var binding))
            {
                bindings.Add(binding);
            }
        }

        return bindings;
    }

    /// <inheritdoc />
    public void SetBindings(string actionName, IReadOnlyList<InputBindingDescriptor> bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(bindings);

        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        InputMap.ActionEraseEvents(actionName);
        foreach (var binding in bindings)
        {
            InputMap.ActionAddEvent(actionName, GodotInputBindingCodec.CreateInputEvent(binding));
        }
    }

    /// <inheritdoc />
    public void ResetAction(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        if (_defaults.TryGetValue(actionName, out var bindings))
        {
            SetBindings(actionName, bindings);
            return;
        }

        if (InputMap.HasAction(actionName))
        {
            // Actions absent from the captured default snapshot should disappear after reset
            // so the live InputMap matches the original project defaults exactly.
            InputMap.EraseAction(actionName);
        }
    }

    /// <inheritdoc />
    public void ResetAll()
    {
        foreach (var actionName in GetActionNames())
        {
            ResetAction(actionName);
        }
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;

namespace GFramework.Game.Input;

/// <summary>
///     提供基于内存快照的默认输入绑定存储实现。
/// </summary>
/// <remarks>
///     该实现聚焦于框架级动作绑定管理语义：默认值恢复、主绑定替换、冲突交换与快照导入导出。
///     它不依赖具体宿主输入事件，适合作为 `Game` 层默认运行时与单元测试基线。
///     该类型内部使用普通 `Dictionary` / `List` 保存可变状态，不提供额外同步原语。
///     宿主应在同一输入线程或受控的串行配置阶段访问它；如果存在跨线程读写需求，应由外层协调同步。
/// </remarks>
public sealed class InputBindingStore : IInputBindingStore
{
    private readonly Dictionary<string, List<InputBindingDescriptor>> _defaultBindings;
    private readonly Dictionary<string, List<InputBindingDescriptor>> _currentBindings;

    /// <summary>
    ///     初始化输入绑定存储。
    /// </summary>
    /// <param name="defaultSnapshot">默认绑定快照。</param>
    public InputBindingStore(InputBindingSnapshot defaultSnapshot)
    {
        _defaultBindings = ToDictionary(defaultSnapshot);
        _currentBindings = CloneDictionary(_defaultBindings);
    }

    /// <inheritdoc />
    public InputActionBinding GetBindings(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        return _currentBindings.TryGetValue(actionName, out var bindings)
            ? new InputActionBinding(actionName, [.. bindings])
            : new InputActionBinding(actionName, []);
    }

    /// <inheritdoc />
    public InputBindingSnapshot ExportSnapshot()
    {
        var actions = _currentBindings
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(static pair => new InputActionBinding(pair.Key, [.. pair.Value]))
            .ToArray();

        return new InputBindingSnapshot(actions);
    }

    /// <inheritdoc />
    public void ImportSnapshot(InputBindingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _currentBindings.Clear();
        foreach (var action in snapshot.Actions)
        {
            _currentBindings[action.ActionName] = [.. action.Bindings];
        }
    }

    /// <inheritdoc />
    public void SetPrimaryBinding(string actionName, InputBindingDescriptor binding, bool swapIfTaken = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(binding);

        var targetBindings = GetOrCreateBindings(actionName);
        var existingOwner = FindOwner(actionName, binding);

        if (existingOwner is not null)
        {
            if (!swapIfTaken)
            {
                return;
            }

            var previousPrimary = targetBindings.Count > 0 ? targetBindings[0] : null;
            var ownerBindings = GetOrCreateBindings(existingOwner);
            ReplaceBinding(ownerBindings, binding, previousPrimary);
        }

        RemoveBinding(targetBindings, binding);
        targetBindings.Insert(0, binding);
    }

    /// <inheritdoc />
    public void ResetAction(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        if (_defaultBindings.TryGetValue(actionName, out var bindings))
        {
            _currentBindings[actionName] = [.. bindings];
            return;
        }

        _currentBindings.Remove(actionName);
    }

    /// <inheritdoc />
    public void ResetAll()
    {
        _currentBindings.Clear();
        foreach (var pair in _defaultBindings)
        {
            _currentBindings[pair.Key] = [.. pair.Value];
        }
    }

    private static Dictionary<string, List<InputBindingDescriptor>> ToDictionary(InputBindingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Actions.ToDictionary(
            static action => action.ActionName,
            static action => action.Bindings.ToList(),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, List<InputBindingDescriptor>> CloneDictionary(
        IReadOnlyDictionary<string, List<InputBindingDescriptor>> source)
    {
        return source.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToList(),
            StringComparer.Ordinal);
    }

    private static void RemoveBinding(List<InputBindingDescriptor> bindings, InputBindingDescriptor binding)
    {
        bindings.RemoveAll(existing => AreEquivalent(existing, binding));
    }

    private static void ReplaceBinding(
        List<InputBindingDescriptor> bindings,
        InputBindingDescriptor bindingToReplace,
        InputBindingDescriptor? replacement)
    {
        var index = bindings.FindIndex(existing => AreEquivalent(existing, bindingToReplace));
        if (index < 0)
        {
            return;
        }

        bindings.RemoveAt(index);
        if (replacement is not null)
        {
            bindings.Insert(index, replacement);
        }
    }

    private static bool AreEquivalent(InputBindingDescriptor left, InputBindingDescriptor right)
    {
        return left.DeviceKind == right.DeviceKind
               && left.BindingKind == right.BindingKind
               && string.Equals(left.Code, right.Code, StringComparison.Ordinal)
               && Nullable.Equals(left.AxisDirection, right.AxisDirection);
    }

    private List<InputBindingDescriptor> GetOrCreateBindings(string actionName)
    {
        if (!_currentBindings.TryGetValue(actionName, out var bindings))
        {
            bindings = [];
            _currentBindings[actionName] = bindings;
        }

        return bindings;
    }

    private string? FindOwner(string excludedActionName, InputBindingDescriptor binding)
    {
        foreach (var pair in _currentBindings)
        {
            if (string.Equals(pair.Key, excludedActionName, StringComparison.Ordinal))
            {
                continue;
            }

            if (pair.Value.Exists(existing => AreEquivalent(existing, binding)))
            {
                return pair.Key;
            }
        }

        return null;
    }
}

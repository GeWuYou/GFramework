// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;
using GFramework.Godot.Input;

namespace GFramework.Godot.Tests.Input;

/// <summary>
///     验证 Godot 输入绑定存储在纯托管后端上的动作快照、导入与冲突交换语义。
/// </summary>
[TestFixture]
public sealed class GodotInputBindingStoreTests
{
    /// <summary>
    ///     验证导出快照会反映后端提供的框架绑定描述。
    /// </summary>
    [Test]
    public void ExportSnapshot_Should_ReturnBackendBindings()
    {
        var backend = new FakeInputMapBackend(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:13",
                            "Enter"),
                    ]),
            ]));

        var store = new GodotInputBindingStore(backend);
        var snapshot = store.ExportSnapshot();
        var acceptBindings = snapshot.Actions.Single(
            action => string.Equals(action.ActionName, "ui_accept", StringComparison.Ordinal));

        Assert.That(acceptBindings.Bindings[0].Code, Is.EqualTo("key:13"));
    }

    /// <summary>
    ///     验证导入快照后会把新绑定回写到后端，并能重新导出。
    /// </summary>
    [Test]
    public void ImportSnapshot_Should_UpdateBackendBindings()
    {
        var backend = new FakeInputMapBackend(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:13",
                            "Enter"),
                    ]),
            ]));

        var store = new GodotInputBindingStore(backend);
        store.ImportSnapshot(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:32",
                            "Space"),
                    ]),
            ]));

        var snapshot = store.ExportSnapshot();
        var acceptBindings = snapshot.Actions.Single(
            action => string.Equals(action.ActionName, "ui_accept", StringComparison.Ordinal));

        Assert.That(acceptBindings.Bindings[0].Code, Is.EqualTo("key:32"));
    }

    /// <summary>
    ///     验证导入快照时，会清空快照中未出现动作的后端绑定。
    /// </summary>
    [Test]
    public void ImportSnapshot_WhenActionMissingFromSnapshot_Should_ClearBackendBindings()
    {
        var backend = new FakeInputMapBackend(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:13",
                            "Enter"),
                    ]),
                new InputActionBinding(
                    "ui_cancel",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:27",
                            "Escape"),
                    ]),
            ]));

        var store = new GodotInputBindingStore(backend);
        store.ImportSnapshot(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:32",
                            "Space"),
                    ]),
            ]));

        var snapshot = store.ExportSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(
                snapshot.Actions.Single(action => string.Equals(action.ActionName, "ui_accept", StringComparison.Ordinal)).Bindings[0].Code,
                Is.EqualTo("key:32"));
            Assert.That(
                snapshot.Actions.Single(action => string.Equals(action.ActionName, "ui_cancel", StringComparison.Ordinal)).Bindings,
                Is.Empty);
        });
    }

    /// <summary>
    ///     验证从纯托管绑定设置主绑定时，会保留 `Game` 层冲突交换语义。
    /// </summary>
    [Test]
    public void SetPrimaryBinding_WhenBindingTaken_SwapsBackendBindings()
    {
        var backend = new FakeInputMapBackend(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "move_left",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:65",
                            "A"),
                    ]),
                new InputActionBinding(
                    "move_right",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:68",
                            "D"),
                    ]),
            ]));

        var store = new GodotInputBindingStore(backend);
        store.SetPrimaryBinding(
            "move_left",
            new InputBindingDescriptor(
                InputDeviceKind.KeyboardMouse,
                InputBindingKind.Key,
                "key:68",
                "D"));

        var snapshot = store.ExportSnapshot();
        var moveLeft = snapshot.Actions.Single(
            action => string.Equals(action.ActionName, "move_left", StringComparison.Ordinal));
        var moveRight = snapshot.Actions.Single(
            action => string.Equals(action.ActionName, "move_right", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(moveLeft.Bindings[0].Code, Is.EqualTo("key:68"));
            Assert.That(moveRight.Bindings[0].Code, Is.EqualTo("key:65"));
        });
    }

    /// <summary>
    ///     验证重置全部绑定时，会移除运行时新增且默认快照中不存在的动作。
    /// </summary>
    [Test]
    public void ResetAll_WhenRuntimeActionIsNotInDefaults_Should_RemoveAction()
    {
        var backend = new FakeInputMapBackend(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:13",
                            "Enter"),
                    ]),
            ]));

        var store = new GodotInputBindingStore(backend);
        store.ImportSnapshot(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "ui_accept",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:13",
                            "Enter"),
                    ]),
                new InputActionBinding(
                    "debug_toggle",
                    [
                        new InputBindingDescriptor(
                            InputDeviceKind.KeyboardMouse,
                            InputBindingKind.Key,
                            "key:192",
                            "QuoteLeft"),
                    ]),
            ]));

        store.ResetAll();

        var snapshot = store.ExportSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(
                snapshot.Actions.Any(action => string.Equals(action.ActionName, "ui_accept", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                snapshot.Actions.Any(action => string.Equals(action.ActionName, "debug_toggle", StringComparison.Ordinal)),
                Is.False);
        });
    }

    /// <summary>
    ///     测试用的纯托管 InputMap 后端。
    /// </summary>
    /// <remarks>
    ///     初始化测试后端。
    /// </remarks>
    /// <param name="snapshot">初始快照。</param>
    private sealed class FakeInputMapBackend(InputBindingSnapshot snapshot) : IGodotInputMapBackend
    {
        private readonly Dictionary<string, List<InputBindingDescriptor>> _defaults = snapshot.Actions.ToDictionary(
                static action => action.ActionName,
                static action => action.Bindings.ToList(),
                StringComparer.Ordinal);
        private readonly Dictionary<string, List<InputBindingDescriptor>> _current = snapshot.Actions.ToDictionary(
                static action => action.ActionName,
                static action => action.Bindings.ToList(),
                StringComparer.Ordinal);

        /// <inheritdoc />
        public IReadOnlyList<string> GetActionNames()
        {
            return [.. _current.Keys.Order(StringComparer.Ordinal)];
        }

        /// <inheritdoc />
        public IReadOnlyList<InputBindingDescriptor> GetBindings(string actionName)
        {
            return _current.TryGetValue(actionName, out var bindings) ? [.. bindings] : Array.Empty<InputBindingDescriptor>();
        }

        /// <inheritdoc />
        public void SetBindings(string actionName, IReadOnlyList<InputBindingDescriptor> bindings)
        {
            _current[actionName] = [.. bindings];
        }

        /// <inheritdoc />
        public void ResetAction(string actionName)
        {
            if (_defaults.TryGetValue(actionName, out var bindings))
            {
                _current[actionName] = [.. bindings];
                return;
            }

            _current.Remove(actionName);
        }

        /// <inheritdoc />
        public void ResetAll()
        {
            _current.Clear();
            foreach (var pair in _defaults)
            {
                _current[pair.Key] = [.. pair.Value];
            }
        }
    }
}

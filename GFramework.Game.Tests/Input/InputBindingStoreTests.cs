// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;
using GFramework.Game.Input;

namespace GFramework.Game.Tests.Input;

/// <summary>
///     验证默认输入绑定存储的重绑定、冲突交换与默认恢复行为。
/// </summary>
[TestFixture]
public sealed class InputBindingStoreTests
{
    /// <summary>
    ///     验证主绑定冲突时，会把原绑定交换回被占用动作。
    /// </summary>
    [Test]
    public void SetPrimaryBinding_WhenBindingOwnedByAnotherAction_SwapsBindings()
    {
        var store = CreateStore();
        var replacement = new InputBindingDescriptor(
            InputDeviceKind.KeyboardMouse,
            InputBindingKind.Key,
            "key:68",
            "D");

        store.SetPrimaryBinding("move_left", replacement);

        var moveLeft = store.GetBindings("move_left");
        var moveRight = store.GetBindings("move_right");

        Assert.Multiple(() =>
        {
            Assert.That(moveLeft.Bindings[0].Code, Is.EqualTo("key:68"));
            Assert.That(moveRight.Bindings[0].Code, Is.EqualTo("key:65"));
        });
    }

    /// <summary>
    ///     验证重置全部绑定时，会回退到初始化默认快照。
    /// </summary>
    [Test]
    public void ResetAll_Should_Restore_DefaultSnapshot()
    {
        var store = CreateStore();
        store.SetPrimaryBinding(
            "move_left",
            new InputBindingDescriptor(InputDeviceKind.KeyboardMouse, InputBindingKind.Key, "key:81", "Q"));

        store.ResetAll();
        var snapshot = store.ExportSnapshot();

        Assert.That(
            snapshot.Actions.Single(action => string.Equals(action.ActionName, "move_left", StringComparison.Ordinal)).Bindings[0].Code,
            Is.EqualTo("key:65"));
    }

    /// <summary>
    ///     验证查询不存在的动作时，不会把空条目写回当前快照。
    /// </summary>
    [Test]
    public void GetBindings_WhenActionMissing_Should_NotMutateSnapshot()
    {
        var store = CreateStore();

        var missingBindings = store.GetBindings("jump");
        var snapshot = store.ExportSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(missingBindings.ActionName, Is.EqualTo("jump"));
            Assert.That(missingBindings.Bindings, Is.Empty);
            Assert.That(snapshot.Actions.Any(action => string.Equals(action.ActionName, "jump", StringComparison.Ordinal)), Is.False);
        });
    }

    private static InputBindingStore CreateStore()
    {
        return new InputBindingStore(
            new InputBindingSnapshot(
            [
                new InputActionBinding(
                    "move_left",
                    [
                        new InputBindingDescriptor(InputDeviceKind.KeyboardMouse, InputBindingKind.Key, "key:65", "A"),
                    ]),
                new InputActionBinding(
                    "move_right",
                    [
                        new InputBindingDescriptor(InputDeviceKind.KeyboardMouse, InputBindingKind.Key, "key:68", "D"),
                    ]),
            ]));
    }
}

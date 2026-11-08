// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Game.Abstractions.Input;

namespace GFramework.Godot.Input;

/// <summary>
///     负责在 Godot 原生输入事件与框架绑定描述之间做双向转换。
/// </summary>
internal static class GodotInputBindingCodec
{
    /// <summary>
    ///     尝试把原生输入事件转换成框架绑定描述。
    /// </summary>
    /// <param name="inputEvent">原生输入事件。</param>
    /// <param name="binding">转换后的绑定描述。</param>
    /// <returns>如果转换成功则返回 <see langword="true" />。</returns>
    public static bool TryCreateBinding(InputEvent inputEvent, out InputBindingDescriptor binding)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);

        switch (inputEvent)
        {
            case InputEventKey keyEvent:
                var keyCode = GetKeyCode(keyEvent);
                binding = new InputBindingDescriptor(
                    InputDeviceKind.KeyboardMouse,
                    InputBindingKind.Key,
string.Create(CultureInfo.InvariantCulture, $"key:{(int)keyCode}"),
                    keyCode.ToString());
                return true;
            case InputEventMouseButton mouseButtonEvent:
                binding = new InputBindingDescriptor(
                    InputDeviceKind.KeyboardMouse,
                    InputBindingKind.MouseButton,
string.Create(CultureInfo.InvariantCulture, $"mouse:{(int)mouseButtonEvent.ButtonIndex}"),
                    mouseButtonEvent.ButtonIndex.ToString());
                return true;
            case InputEventJoypadButton joypadButtonEvent:
                binding = new InputBindingDescriptor(
                    InputDeviceKind.Gamepad,
                    InputBindingKind.GamepadButton,
string.Create(CultureInfo.InvariantCulture, $"joy-button:{(int)joypadButtonEvent.ButtonIndex}"),
                    joypadButtonEvent.ButtonIndex.ToString());
                return true;
            case InputEventJoypadMotion joypadMotionEvent:
                var direction = joypadMotionEvent.AxisValue >= 0f ? 1f : -1f;
                binding = new InputBindingDescriptor(
                    InputDeviceKind.Gamepad,
                    InputBindingKind.GamepadAxis,
string.Create(CultureInfo.InvariantCulture, $"joy-axis:{(int)joypadMotionEvent.Axis}:{direction}"),
                    GetAxisDisplayName(joypadMotionEvent.Axis, direction),
                    direction);
                return true;
            default:
                binding = null!;
                return false;
        }
    }

    /// <summary>
    ///     把框架绑定描述还原为 Godot 输入事件。
    /// </summary>
    /// <param name="binding">绑定描述。</param>
    /// <returns>可写回 `InputMap` 的输入事件。</returns>
    /// <exception cref="ArgumentException">当绑定描述无法转换时抛出。</exception>
    public static InputEvent CreateInputEvent(InputBindingDescriptor binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return binding.BindingKind switch
        {
            InputBindingKind.Key => CreateKeyEvent(binding),
            InputBindingKind.MouseButton => CreateMouseButtonEvent(binding),
            InputBindingKind.GamepadButton => CreateGamepadButtonEvent(binding),
            InputBindingKind.GamepadAxis => CreateGamepadAxisEvent(binding),
            _ => throw new ArgumentException($"Unsupported binding kind '{binding.BindingKind}'.", nameof(binding)),
        };
    }

    /// <summary>
    ///     从原生输入事件推断当前设备上下文。
    /// </summary>
    /// <param name="inputEvent">原生输入事件。</param>
    /// <returns>推断出的设备上下文。</returns>
    public static InputDeviceContext GetDeviceContext(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);

        return inputEvent switch
        {
            InputEventKey => new InputDeviceContext(InputDeviceKind.KeyboardMouse),
            InputEventMouse => new InputDeviceContext(InputDeviceKind.KeyboardMouse),
            InputEventJoypadButton joypadButtonEvent => CreateGamepadContext(joypadButtonEvent.Device),
            InputEventJoypadMotion joypadMotionEvent => CreateGamepadContext(joypadMotionEvent.Device),
            InputEventScreenTouch => new InputDeviceContext(InputDeviceKind.Touch),
            _ => new InputDeviceContext(InputDeviceKind.Unknown),
        };
    }

    private static InputDeviceContext CreateGamepadContext(int deviceIndex)
    {
        return new InputDeviceContext(
            InputDeviceKind.Gamepad,
            deviceIndex,
            "gamepad");
    }

    private static InputEventKey CreateKeyEvent(InputBindingDescriptor binding)
    {
        var code = ParseSingleSegment(binding.Code, "key");
        return new InputEventKey
        {
            Keycode = (Key)code,
            PhysicalKeycode = (Key)code,
        };
    }

    private static InputEventMouseButton CreateMouseButtonEvent(InputBindingDescriptor binding)
    {
        var buttonIndex = ParseSingleSegment(binding.Code, "mouse");
        return new InputEventMouseButton
        {
            ButtonIndex = (MouseButton)buttonIndex,
        };
    }

    private static InputEventJoypadButton CreateGamepadButtonEvent(InputBindingDescriptor binding)
    {
        var buttonIndex = ParseSingleSegment(binding.Code, "joy-button");
        return new InputEventJoypadButton
        {
            ButtonIndex = (JoyButton)buttonIndex,
        };
    }

    private static InputEventJoypadMotion CreateGamepadAxisEvent(InputBindingDescriptor binding)
    {
        var parts = binding.Code.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !string.Equals(parts[0], "joy-axis", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Binding code '{binding.Code}' is not a valid joy-axis code.", nameof(binding));
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var axis))
        {
            throw new ArgumentException($"Binding code '{binding.Code}' does not contain a valid axis index.", nameof(binding));
        }

        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var direction))
        {
            throw new ArgumentException($"Binding code '{binding.Code}' does not contain a valid axis direction.", nameof(binding));
        }

        return new InputEventJoypadMotion
        {
            Axis = (JoyAxis)axis,
            AxisValue = direction,
        };
    }

    private static int ParseSingleSegment(string code, string prefix)
    {
        var parts = code.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Binding code '{code}' is not a valid {prefix} code.", nameof(code));
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"Binding code '{code}' does not contain a valid numeric value.", nameof(code));
        }

        return value;
    }

    private static Key GetKeyCode(InputEventKey keyEvent)
    {
        return keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
    }

    private static string GetAxisDisplayName(JoyAxis axis, float direction)
    {
        return direction >= 0f
            ? $"{axis} Positive" : $"{axis} Negative";
    }
}

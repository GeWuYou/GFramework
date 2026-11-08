// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;

namespace GFramework.Game.Input;

/// <summary>
///     提供可由宿主侧更新的默认输入设备跟踪器。
/// </summary>
public sealed class InputDeviceTracker : IInputDeviceTracker
{
    /// <summary>
    ///     初始化输入设备跟踪器。
    /// </summary>
    public InputDeviceTracker()
    {
        CurrentDevice = new InputDeviceContext(InputDeviceKind.Unknown);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     该属性不提供额外同步原语。
    ///     宿主应在同一输入线程内调用 <see cref="Update" /> 并读取当前值，例如 Godot 的主线程或输入事件线程。
    /// </remarks>
    public InputDeviceContext CurrentDevice
    {
        get; private set;
    }

    /// <summary>
    ///     使用新的宿主设备上下文覆盖当前状态。
    /// </summary>
    /// <param name="context">新的设备上下文。</param>
    /// <remarks>
    ///     该方法设计给宿主输入线程串行调用。
    ///     如果宿主需要跨线程读取设备上下文，应在外层提供自己的同步策略，而不是依赖此类型完成可见性保证。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="context" /> 为 <see langword="null" /> 时抛出。</exception>
    public void Update(InputDeviceContext context)
    {
        CurrentDevice = context ?? throw new ArgumentNullException(nameof(context));
    }
}

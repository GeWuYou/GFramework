// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     只读状态容器接口，用于暴露应用状态快照和订阅能力。
///     该抽象适用于 Controller、Query、ViewModel 等只需要观察状态的调用方，
///     使其无需依赖写入能力即可响应复杂状态树的变化。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public interface IReadonlyStore<out TState>
{
    /// <summary>
    ///     获取当前状态快照。
    ///     Store 负责保证返回值与最近一次成功分发后的状态一致。
    /// </summary>
    TState State { get; }

    /// <summary>
    ///     订阅状态变化通知。
    ///     仅当 Store 判断状态发生有效变化时，才会调用该监听器。
    /// </summary>
    /// <param name="listener">状态变化时的监听器，参数为新的状态快照。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    IUnRegister Subscribe(Action<TState> listener);

    /// <summary>
    ///     订阅状态变化通知，并立即以当前状态调用一次监听器。
    ///     该方法适合在 UI 初始化或 ViewModel 首次绑定时建立同步视图。
    /// </summary>
    /// <param name="listener">状态变化时的监听器，参数为新的状态快照。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    IUnRegister SubscribeWithInitValue(Action<TState> listener);

    /// <summary>
    ///     取消订阅指定的状态监听器。
    /// </summary>
    /// <param name="listener">需要移除的监听器。</param>
    void UnSubscribe(Action<TState> listener);
}
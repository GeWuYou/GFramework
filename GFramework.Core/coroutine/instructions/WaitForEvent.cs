// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     WaitForEvent 类用于等待特定事件的发生，并提供事件数据和完成状态。
///     实现了 IYieldInstruction 和 IDestroyable 接口，支持协程等待和资源释放。
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public sealed class WaitForEvent<TEvent> : IYieldInstruction, IDisposable
{
    private bool _disposed;
    private volatile bool _done;
    private IUnRegister? _unRegister;

    /// <summary>
    ///     初始化 WaitForEvent 实例，注册事件监听器以等待指定事件。
    /// </summary>
    /// <param name="eventBus">事件总线实例，用于注册和监听事件。</param>
    /// <exception cref="ArgumentNullException">当 eventBus 为 null 时抛出异常。</exception>
    public WaitForEvent(IEventBus eventBus)
    {
        var eventBus1 = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // 注册事件监听器，当事件触发时调用 OnEventTriggered 方法
        _unRegister = eventBus1.Register<TEvent>(OnEventTriggered);
    }

    /// <summary>
    ///     获取接收到的事件数据。仅在事件触发后可用。
    /// </summary>
    public TEvent? EventData { get; private set; }

    /// <summary>
    ///     释放 WaitForEvent 实例占用的资源，包括注销事件监听器。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // 注销事件注册并清理资源，防止内存泄漏
        _unRegister?.UnRegister();
        _unRegister = null;
        _disposed = true;
    }

    /// <summary>
    ///     获取等待是否已完成。当事件触发后，此属性将返回 true。
    /// </summary>
    public bool IsDone => _done;

    /// <summary>
    ///     更新方法，用于处理时间更新逻辑。通常由协程系统调用。
    /// </summary>
    /// <param name="deltaTime">时间增量（秒），表示自上次更新以来经过的时间。</param>
    public void Update(double deltaTime)
    {
        // 如果事件已完成且事件监听器仍存在，则注销监听器以释放资源
        if (!_done || _unRegister == null) return;
        _unRegister.UnRegister();
        _unRegister = null;
    }

    /// <summary>
    ///     事件触发时的回调处理方法。设置事件数据并标记等待完成。
    /// </summary>
    /// <param name="eventData">触发事件时传递的数据。</param>
    private void OnEventTriggered(TEvent eventData)
    {
        EventData = eventData;
        _done = true;
    }
}
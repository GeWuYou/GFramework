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

using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
/// 事件监听器作用域，用于监听特定类型的事件并在触发时提供事件数据
/// </summary>
/// <typeparam name="TEvent">要监听的事件类型</typeparam>
public sealed class EventListenerScope<TEvent> : IDisposable
{
    private readonly IUnRegister? _unRegister;
    private TEvent? _eventData;
    private volatile bool _triggered;

    /// <summary>
    /// 初始化事件监听器作用域的新实例
    /// </summary>
    /// <param name="eventBus">事件总线实例，用于注册事件监听器</param>
    public EventListenerScope(IEventBus eventBus)
    {
        _unRegister = eventBus.Register<TEvent>(OnEventTriggered);
    }

    /// <summary>
    /// 获取接收到的事件数据
    /// </summary>
    public TEvent? EventData => _eventData;

    /// <summary>
    /// 获取事件是否已被触发
    /// </summary>
    public bool IsTriggered => _triggered;

    /// <summary>
    /// 释放资源，取消事件监听器的注册
    /// </summary>
    public void Dispose()
    {
        _unRegister?.UnRegister();
    }

    /// <summary>
    /// 事件触发时的回调方法，用于存储事件数据并标记已触发状态
    /// </summary>
    /// <param name="eventData">接收到的事件数据</param>
    private void OnEventTriggered(TEvent eventData)
    {
        _eventData = eventData;
        _triggered = true;
    }
}
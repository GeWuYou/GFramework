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

public sealed class WaitForEvent<TEvent> : IYieldInstruction, IDisposable
{
    private bool _disposed;
    private volatile bool _done;
    private IUnRegister? _unRegister;

    /// <summary>
    ///     初始化等待事件的指令
    /// </summary>
    /// <param name="eventBus">事件总线实例</param>
    public WaitForEvent(IEventBus eventBus)
    {
        var eventBus1 = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // 注册事件监听器
        _unRegister = eventBus1.Register<TEvent>(OnEventTriggered);
    }

    /// <summary>
    ///     获取接收到的事件数据
    /// </summary>
    public TEvent? EventData { get; private set; }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // 注销事件注册并清理资源
        _unRegister?.UnRegister();
        _unRegister = null;
        _disposed = true;
    }

    /// <summary>
    ///     获取等待是否已完成
    /// </summary>
    public bool IsDone => _done;

    /// <summary>
    ///     更新方法，用于处理时间更新逻辑
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 事件的完成由事件回调设置
        // 如果已完成，确保注销事件监听器
        if (!_done || _unRegister == null) return;
        _unRegister.UnRegister();
        _unRegister = null;
    }

    /// <summary>
    ///     事件触发时的回调处理
    /// </summary>
    /// <param name="eventData">事件数据</param>
    private void OnEventTriggered(TEvent eventData)
    {
        EventData = eventData;
        _done = true;
    }
}
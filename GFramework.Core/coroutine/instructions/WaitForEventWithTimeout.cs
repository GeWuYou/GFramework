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

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     带超时的事件等待指令
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <param name="waitForEvent">要包装的事件等待指令</param>
/// <param name="timeout">超时时间（秒）</param>
public sealed class WaitForEventWithTimeout<TEvent>(WaitForEvent<TEvent> waitForEvent, float timeout)
    : IYieldInstruction
{
    private readonly WaitForEvent<TEvent> _waitForEvent =
        waitForEvent ?? throw new ArgumentNullException(nameof(waitForEvent));

    private float _elapsedTime;

    /// <summary>
    ///     获取是否已超时
    /// </summary>
    public bool IsTimeout => _elapsedTime >= timeout;

    /// <summary>
    ///     获取接收到的事件数据
    /// </summary>
    public TEvent? EventData => _waitForEvent.EventData;

    /// <summary>
    ///     获取指令是否已完成（事件已触发或超时）
    /// </summary>
    public bool IsDone => _waitForEvent.IsDone || IsTimeout;

    /// <summary>
    ///     更新指令状态
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 只有在事件未完成时才累加经过的时间
        if (!_waitForEvent.IsDone) _elapsedTime += (float)deltaTime;

        _waitForEvent.Update(deltaTime);
    }
}
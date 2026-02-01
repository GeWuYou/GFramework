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

using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine.instructions;

namespace GFramework.Core.coroutine.extensions;

/// <summary>
///     命令协程扩展方法类
///     提供将命令的异步执行包装为协程的功能
/// </summary>
public static class CommandCoroutineExtensions
{
    /// <summary>
    ///     将 Command 的异步执行包装为协程，并处理异常
    /// </summary>
    /// <typeparam name="TCommand">命令类型，必须实现 IAsyncCommand 接口</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="command">要执行的命令实例</param>
    /// <param name="onError">错误回调处理</param>
    /// <returns>返回协程指令枚举器</returns>
    public static IEnumerator<IYieldInstruction> SendCommandCoroutineWithErrorHandler<TCommand>(
        this IContextAware contextAware,
        TCommand command,
        Action<Exception>? onError = null)
        where TCommand : class, IAsyncCommand
    {
        var task = contextAware.GetContext().SendCommandAsync(command);

        yield return task.AsCoroutineInstruction();

        if (task.IsFaulted) onError?.Invoke(task.Exception!);
    }

    /// <summary>
    ///     发送 Command 并等待指定 Event
    /// </summary>
    /// <typeparam name="TCommand">命令类型，必须实现 IAsyncCommand 接口</typeparam>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="command">要执行的命令实例</param>
    /// <param name="onEvent">事件触发时的回调处理</param>
    /// <param name="timeout">等待超时时间（秒），0表示无限等待</param>
    /// <returns>返回协程指令枚举器</returns>
    public static IEnumerator<IYieldInstruction> SendCommandAndWaitEventCoroutine<TCommand, TEvent>(
        this IContextAware contextAware,
        TCommand command,
        Action<TEvent>? onEvent = null,
        float timeout = 0f)
        where TCommand : IAsyncCommand
        where TEvent : class
    {
        var context = contextAware.GetContext();
        var eventBus = context.GetService<IEventBus>()!;

        WaitForEvent<TEvent>? wait = null;

        try
        {
            // 先注册事件监听器
            wait = new WaitForEvent<TEvent>(eventBus);

            // 发送异步命令并等待完成
            var task = context.SendCommandAsync(command);
            yield return task.AsCoroutineInstruction();

            // 如果有超时设置，使用超时等待
            if (timeout > 0f)
            {
                var timeoutWait = new WaitForEventWithTimeout<TEvent>(wait, timeout);
                yield return timeoutWait;

                // 检查是否超时
                if (timeoutWait.IsTimeout)
                    // 超时处理
                    throw new TimeoutException($"wait for the event ${typeof(TEvent).Name} timeout.");
            }
            else
            {
                // 等待事件触发（无超时）
                yield return wait;
            }

            // 调用事件回调
            if (onEvent != null && wait.EventData != null) onEvent.Invoke(wait.EventData);
        }
        finally
        {
            // 确保清理事件监听器
            wait?.Dispose();
        }
    }
}
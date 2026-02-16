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
using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine.instructions;
using Mediator;

namespace GFramework.Core.coroutine.extensions;

/// <summary>
/// 提供Mediator模式与协程集成的扩展方法。
/// 包含发送命令和等待事件的协程实现。
/// </summary>
public static class MediatorCoroutineExtensions
{
    /// <summary>
    /// 以协程方式发送命令并处理可能的异常。
    /// </summary>
    /// <typeparam name="TCommand">命令的类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于获取服务</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="onError">发生异常时的回调处理函数</param>
    /// <returns>协程枚举器，用于协程执行</returns>
    public static IEnumerator<IYieldInstruction> SendCommandCoroutine<TCommand>(
        this IContextAware contextAware,
        TCommand command,
        Action<Exception>? onError = null)
        where TCommand : notnull
    {
        var mediator = contextAware
            .GetContext()
            .GetService<IMediator>()!;

        var task = mediator.Send(command).AsTask();

        yield return task.AsCoroutineInstruction();

        if (task.IsFaulted)
            onError?.Invoke(task.Exception!);
    }

    /// <summary>
    /// 发送命令并等待特定事件的协程实现，支持超时控制。
    /// </summary>
    /// <typeparam name="TCommand">命令的类型</typeparam>
    /// <typeparam name="TEvent">要等待的事件类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于获取服务</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="onEvent">接收到事件时的回调处理函数</param>
    /// <param name="timeout">超时时间（秒），0表示无超时</param>
    /// <returns>协程枚举器，用于协程执行</returns>
    public static IEnumerator<IYieldInstruction> SendCommandAndWaitEventCoroutine<TCommand, TEvent>(
        this IContextAware contextAware,
        TCommand command,
        Action<TEvent>? onEvent = null,
        float timeout = 0f)
        where TCommand : notnull
        where TEvent : class
    {
        var context = contextAware.GetContext();
        var mediator = context.GetService<IMediator>()!;
        var eventBus = context.GetService<IEventBus>()!;

        WaitForEvent<TEvent>? wait = null;

        try
        {
            wait = new WaitForEvent<TEvent>(eventBus);

            var task = mediator.Send(command).AsTask();

            yield return task.AsCoroutineInstruction();

            if (timeout > 0f)
            {
                var timeoutWait = new WaitForEventWithTimeout<TEvent>(wait, timeout);
                yield return timeoutWait;

                if (timeoutWait.IsTimeout)
                    throw new TimeoutException(
                        $"wait for event {typeof(TEvent).Name} timeout.");
            }
            else
            {
                yield return wait;
            }

            if (wait.EventData != null)
                onEvent?.Invoke(wait.EventData);
        }
        finally
        {
            wait?.Dispose();
        }
    }
}
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

using GFramework.Core.rule;
using Mediator;

namespace GFramework.Core.cqrs.notification;

/// <summary>
/// 抽象通知处理器基类
/// 继承自ContextAwareBase并实现INotificationHandler接口，为具体的通知处理器提供基础功能
/// 用于处理CQRS模式中的通知消息，支持异步处理
/// </summary>
/// <typeparam name="TNotification">通知类型，必须实现INotification接口</typeparam>
public abstract class AbstractNotificationHandler<TNotification> : ContextAwareBase, INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// 处理指定的通知消息
    /// 由具体的通知处理器子类实现通知处理逻辑
    /// </summary>
    /// <param name="notification">要处理的通知对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask</returns>
    public abstract ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
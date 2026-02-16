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

using GFramework.Core.Abstractions.cqrs.notification;
using Mediator;

namespace GFramework.Core.cqrs.notification;

/// <summary>
/// 表示一个基础通知类，用于处理带有输入的通知模式实现。
/// 该类实现了 INotification 接口，提供了通用的通知结构。
/// </summary>
/// <typeparam name="TInput">通知输入数据的类型，必须实现 INotificationInput 接口</typeparam>
/// <param name="input">通知执行所需的输入数据</param>
public class NotificationBase<TInput>(TInput input) : INotification where TInput : INotificationInput
{
    /// <summary>
    /// 获取通知的输入数据。
    /// </summary>
    public TInput Input => input;
}
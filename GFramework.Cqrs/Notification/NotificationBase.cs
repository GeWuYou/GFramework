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

using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs.Notification;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     为携带输入模型的 CQRS 通知提供统一基类。
/// </summary>
/// <typeparam name="TInput">通知输入类型，必须实现 <see cref="INotificationInput" />。</typeparam>
/// <param name="input">通知广播时携带的输入对象。</param>
/// <remarks>
///     该类型继续保留在历史公开命名空间中，以避免调用方因 runtime 程序集拆分而批量修改继承层次。
///     具体实现现由 <c>GFramework.Cqrs</c> 程序集承载，并通过 type forward 维持旧程序集兼容性。
/// </remarks>
public abstract class NotificationBase<TInput>(TInput input) : INotification
    where TInput : INotificationInput
{
    /// <summary>
    ///     获取通知广播时携带的输入对象。
    /// </summary>
    public TInput Input => input;
}

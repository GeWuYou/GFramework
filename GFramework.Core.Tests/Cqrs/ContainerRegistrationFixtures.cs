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

namespace GFramework.Core.Tests.Cqrs;

/// <summary>
///     为容器层测试提供可扫描的最小通知夹具。
/// </summary>
internal sealed record DeterministicOrderNotification : INotification;

/// <summary>
///     供容器注册测试验证程序集扫描结果的通知处理器。
/// </summary>
internal sealed class DeterministicOrderNotificationHandler : INotificationHandler<DeterministicOrderNotification>
{
    /// <summary>
    ///     无副作用地消费通知。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(DeterministicOrderNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

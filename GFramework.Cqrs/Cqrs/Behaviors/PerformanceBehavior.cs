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

using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Cqrs.Behaviors;

/// <summary>
///     在 CQRS 请求管道中监控处理耗时，并对长耗时请求发出告警。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
///     该行为保留现有公开命名空间以维持消费端兼容性，但实现已迁入 <c>GFramework.Cqrs</c> 程序集。
/// </remarks>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const double SlowRequestThresholdMilliseconds = 500;

    private readonly ILogger _logger =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(PerformanceBehavior<TRequest, TResponse>));

    /// <summary>
    ///     统计当前请求处理耗时，并在超阈值时记录警告日志。
    /// </summary>
    /// <param name="message">当前请求消息。</param>
    /// <param name="next">后续处理委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求处理结果。</returns>
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            return await next(message, cancellationToken);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            if (elapsed.TotalMilliseconds > SlowRequestThresholdMilliseconds)
            {
                var requestName = typeof(TRequest).Name;
                _logger.Warn($"Long Running Request: {requestName} ({elapsed.TotalMilliseconds:F2} ms)");
            }
        }
    }
}

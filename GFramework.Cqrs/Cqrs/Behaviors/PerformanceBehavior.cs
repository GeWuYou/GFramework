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

using System.Globalization;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Cqrs.Cqrs.Behaviors;

/// <summary>
///     在 CQRS 请求管道中监控处理耗时，并对长耗时请求发出告警。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
///     该行为保留现有公开命名空间以维持消费端兼容性，但实现已迁入 <c>GFramework.Cqrs</c> 程序集。
///     日志基于标准 <see cref="Microsoft.Extensions.Logging.ILogger" />：DI 激活时通过构造函数注入
///     <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}" />；未注册日志基础设施时回退到空实现。
/// </remarks>
/// <remarks>
///     初始化耗时监控行为。
/// </remarks>
/// <param name="logger">
///     当前行为使用的日志器；为 <see langword="null" /> 时回退到
///     <see cref="NullLogger{TCategoryName}.Instance" />，避免要求宿主必须注册日志基础设施。
/// </param>
public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const double SlowRequestThresholdMilliseconds = 500;

    private readonly ILogger _logger = logger ?? NullLogger<PerformanceBehavior<TRequest, TResponse>>.Instance;

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
            return await next(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            if (elapsed.TotalMilliseconds > SlowRequestThresholdMilliseconds)
            {
                var requestName = typeof(TRequest).Name;
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                {
                    _logger.LogWarning("Long Running Request: {RequestName} ({ElapsedMilliseconds:F2} ms)", requestName, elapsed.TotalMilliseconds);
                }
            }
        }
    }
}

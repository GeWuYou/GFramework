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

using System.Diagnostics;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using Mediator;

namespace GFramework.Core.cqrs.behaviors;

/// <summary>
/// 性能监控行为类，用于监控CQRS请求的执行时间
/// 实现IPipelineBehavior接口，检测并记录执行时间过长的请求
/// </summary>
/// <typeparam name="TRequest">请求类型，必须实现IRequest接口</typeparam>
/// <typeparam name="TResponse">响应类型</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(PerformanceBehavior<TRequest, TResponse>));

    /// <summary>
    /// 处理请求并监控执行时间
    /// 使用Stopwatch测量请求处理耗时，超过500ms时记录警告日志
    /// </summary>
    /// <param name="message">要处理的请求消息</param>
    /// <param name="next">下一个处理委托，用于继续管道执行</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>处理结果的ValueTask</returns>
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

            if (elapsed.TotalMilliseconds > 500)
            {
                var requestName = typeof(TRequest).Name;
                _logger.Warn(
                    $"Long Running Request: {requestName} ({elapsed.TotalMilliseconds:F2} ms)");
            }
        }
    }
}
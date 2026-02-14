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
/// 日志记录行为类，用于在CQRS管道中记录请求处理的日志信息
/// 实现IPipelineBehavior接口，为请求处理提供日志记录功能
/// </summary>
/// <typeparam name="TRequest">请求类型，必须实现IRequest接口</typeparam>
/// <typeparam name="TResponse">响应类型</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(LoggingBehavior<TRequest, TResponse>));

    /// <summary>
    /// 处理请求并记录日志
    /// 在请求处理前后记录调试信息，处理异常时记录错误日志
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
        var requestName = typeof(TRequest).Name;
        var start = Stopwatch.GetTimestamp();

        _logger.Debug($"Handling {requestName}");

        try
        {
            var response = await next(message, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(start);
            _logger.Debug($"Handled {requestName} successfully in {elapsed.TotalMilliseconds} ms");

            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Handling {requestName} was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            _logger.Error($"Error handling {requestName} after {elapsed.TotalMilliseconds} ms", ex);
            throw;
        }
    }
}
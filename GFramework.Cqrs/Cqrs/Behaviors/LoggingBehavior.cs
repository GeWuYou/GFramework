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
///     在 CQRS 请求管道中记录请求开始、完成、取消与失败日志。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
///     该行为已迁移到 <c>GFramework.Cqrs.Cqrs.Behaviors</c> 命名空间，
///     实现位于 <c>GFramework.Cqrs</c> 程序集，用于承载 CQRS runtime 细节并与旧层解耦。
///     日志基于标准 <see cref="Microsoft.Extensions.Logging.ILogger" />：DI 激活时通过构造函数注入
///     <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}" />；未注册日志基础设施时回退到空实现。
/// </remarks>
/// <remarks>
///     初始化请求日志行为。
/// </remarks>
/// <param name="logger">
///     当前行为使用的日志器；为 <see langword="null" /> 时回退到
///     <see cref="NullLogger{TCategoryName}.Instance" />，避免要求宿主必须注册日志基础设施。
/// </param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger = logger ?? NullLogger<LoggingBehavior<TRequest, TResponse>>.Instance;

    /// <summary>
    ///     执行日志包装后的下一段请求处理逻辑。
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
        var requestName = typeof(TRequest).Name;
        var start = Stopwatch.GetTimestamp();

        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            _logger.LogDebug("Handling {RequestName}", requestName);
        }

        try
        {
            var response = await next(message, cancellationToken).ConfigureAwait(false);

            var elapsed = Stopwatch.GetElapsedTime(start);
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                _logger.LogDebug("Handled {RequestName} successfully in {ElapsedMilliseconds} ms", requestName, elapsed.TotalMilliseconds);
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                _logger.LogWarning("Handling {RequestName} was cancelled", requestName);
            }
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
            {
                _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMilliseconds} ms", requestName, elapsed.TotalMilliseconds);
            }
            throw;
        }
    }
}

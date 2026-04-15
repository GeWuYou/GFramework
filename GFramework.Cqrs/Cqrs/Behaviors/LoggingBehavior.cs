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
///     在 CQRS 请求管道中记录请求开始、完成、取消与失败日志。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <remarks>
///     该行为已迁移到 <c>GFramework.Cqrs.Cqrs.Behaviors</c> 命名空间，
///     实现位于 <c>GFramework.Cqrs</c> 程序集，用于承载 CQRS runtime 细节并与旧层解耦。
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(LoggingBehavior<TRequest, TResponse>));

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

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     为 legacy Core CQRS bridge 提供共享的上下文解析与同步兼容辅助逻辑。
/// </summary>
/// <remarks>
///     旧的同步 Command/Query 入口仍需要阻塞等待统一 <see cref="ICqrsRuntime" /> 返回结果。
///     这里统一通过 <see cref="Task.Run(System.Func{System.Threading.Tasks.Task})" /> 把等待动作切换到线程池，
///     避免直接占用调用方的 <see cref="SynchronizationContext" /> 导致 legacy 同步入口与异步 pipeline 互相卡死。
/// </remarks>
internal static class LegacyCqrsDispatchHelper
{
    /// <summary>
    ///     解析当前 legacy 目标对象是否能够绑定到统一 CQRS runtime 的架构上下文。
    /// </summary>
    /// <param name="runtime">当前执行器可用的统一 CQRS runtime。</param>
    /// <param name="target">即将执行的 legacy 目标对象。</param>
    /// <param name="context">命中时返回可用于 CQRS runtime 的架构上下文。</param>
    /// <returns>
    ///     当 <paramref name="runtime" /> 可用且 <paramref name="target" /> 能稳定提供
    ///     <see cref="IArchitectureContext" /> 时返回 <see langword="true" />；否则返回 <see langword="false" />。
    /// </returns>
    internal static bool TryResolveDispatchContext(
        [NotNullWhen(true)] ICqrsRuntime? runtime,
        object target,
        out IArchitectureContext context)
    {
        ArgumentNullException.ThrowIfNull(target);

        context = null!;

        if (runtime is null || target is not IContextAware contextAware)
        {
            return false;
        }

        try
        {
            context = contextAware.GetContext();
            return true;
        }
        catch (InvalidOperationException exception) when (IsMissingContextException(exception))
        {
            return false;
        }
    }

    /// <summary>
    ///     判断当前 <see cref="InvalidOperationException" /> 是否表示 legacy 目标尚未具备可桥接的架构上下文。
    /// </summary>
    /// <param name="exception">由 <see cref="IContextAware.GetContext" /> 抛出的异常。</param>
    /// <returns>
    ///     仅当异常明确表示“上下文尚未设置”或“当前没有活动上下文”时返回 <see langword="true" />；
    ///     其他运行时错误必须继续向上传播，避免把真实故障误判为可安全回退。
    /// </returns>
    private static bool IsMissingContextException(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Equals(
                   exception.Message,
                   "Architecture context has not been set. Call SetContext before accessing the context.",
                   StringComparison.Ordinal)
               || string.Equals(
                   exception.Message,
                   "No active architecture context is currently bound.",
                   StringComparison.Ordinal);
    }

    /// <summary>
    ///     同步等待统一 CQRS runtime 完成无返回值请求。
    /// </summary>
    /// <param name="runtime">负责分发当前请求的统一 CQRS runtime。</param>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="request">要同步等待的请求。</param>
    internal static void SendSynchronously(
        ICqrsRuntime runtime,
        IArchitectureContext context,
        IRequest<Unit> request)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Task.Run(() => runtime.SendAsync(context, request).AsTask()).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     同步等待统一 CQRS runtime 完成带返回值请求，并返回实际响应。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    /// <param name="runtime">负责分发当前请求的统一 CQRS runtime。</param>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="request">要同步等待的请求。</param>
    /// <returns>统一 CQRS runtime 返回的响应结果。</returns>
    internal static TResponse SendSynchronously<TResponse>(
        ICqrsRuntime runtime,
        IArchitectureContext context,
        IRequest<TResponse> request)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(() => runtime.SendAsync(context, request).AsTask()).GetAwaiter().GetResult();
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs;

/// <summary>
///     描述单个 stream request/response 类型对与其 generated invoker 元数据之间的映射条目。
/// </summary>
public sealed record CqrsStreamInvokerDescriptorEntry
{
    /// <summary>
    ///     初始化 stream invoker 描述符映射条目。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">对应的 generated stream invoker 描述符。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="requestType" />、<paramref name="responseType" /> 或 <paramref name="descriptor" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public CqrsStreamInvokerDescriptorEntry(
        Type requestType,
        Type responseType,
        CqrsStreamInvokerDescriptor descriptor)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    /// <summary>
    ///     获取流式请求运行时类型。
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    ///     获取流式响应元素类型。
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    ///     获取对应的 generated stream invoker 描述符。
    /// </summary>
    public CqrsStreamInvokerDescriptor Descriptor { get; }
}

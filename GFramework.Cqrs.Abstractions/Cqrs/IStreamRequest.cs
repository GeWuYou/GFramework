// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     表示一个流式 CQRS 请求。
///     请求处理器可以逐步产生响应序列，而不是一次性返回完整结果。
/// </summary>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
public interface IStreamRequest<out TResponse>;

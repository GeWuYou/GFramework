// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证 generated request invoker provider 接线的测试请求。
/// </summary>
/// <param name="Value">用于验证 generated invoker 结果拼接的请求负载。</param>
internal sealed record GeneratedRequestInvokerRequest(string Value) : IRequest<string>;

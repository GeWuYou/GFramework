// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证 generated stream invoker provider 接线的测试流式请求。
/// </summary>
/// <param name="Start">用于构造 generated stream 输出的起始值。</param>
internal sealed record GeneratedStreamInvokerRequest(int Start) : IStreamRequest<int>;

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 pipeline executor 上下文刷新回归提供带分发标识的最小请求。
/// </summary>
/// <param name="DispatchId">当前分发的稳定标识，便于断言 handler 与 behavior 看到的是同一次请求。</param>
internal sealed record DispatcherPipelineContextRefreshRequest(string DispatchId) : IRequest<int>;

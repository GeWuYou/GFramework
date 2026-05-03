// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证 pipeline invoker 缓存的测试请求。
/// </summary>
internal sealed record DispatcherPipelineCacheRequest : IRequest<int>;

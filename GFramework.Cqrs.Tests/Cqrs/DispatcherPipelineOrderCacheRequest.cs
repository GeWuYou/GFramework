// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为双行为 pipeline 顺序回归提供最小请求。
/// </summary>
internal sealed record DispatcherPipelineOrderCacheRequest : IRequest<int>;

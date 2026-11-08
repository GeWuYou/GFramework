// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证架构公开 stream pipeline 行为注册入口的最小流式请求。
/// </summary>
public sealed class ModuleStreamBehaviorRequest : IStreamRequest<int>;

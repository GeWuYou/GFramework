// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     优先级服务接口
/// </summary>
public interface IPrioritizedService : IPrioritized, IMixedService
{
}

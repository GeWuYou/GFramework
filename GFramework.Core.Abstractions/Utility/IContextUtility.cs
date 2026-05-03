// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Abstractions.Utility;

/// <summary>
///     上下文工具接口，继承自IUtility和IContextAware接口
///     提供具有上下文感知能力的工具功能
/// </summary>
public interface IContextUtility : IUtility, IContextAware, ILifecycle;
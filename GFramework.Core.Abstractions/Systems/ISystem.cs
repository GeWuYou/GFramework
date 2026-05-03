// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Abstractions.Systems;

/// <summary>
///     系统接口，定义了系统的基本行为和功能
///     该接口继承了多个框架相关的接口，提供了系统初始化和销毁能力
/// </summary>
public interface ISystem : IContextAware, IArchitecturePhaseListener, ILifecycle;
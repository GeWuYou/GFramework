// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Abstractions.Model;

/// <summary>
///     模型接口，定义了模型的基本行为和功能
/// </summary>
public interface IModel : IContextAware, IArchitecturePhaseListener, IInitializable;
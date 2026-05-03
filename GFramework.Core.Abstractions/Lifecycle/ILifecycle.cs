// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     完整生命周期接口，组合了初始化和销毁能力
/// </summary>
public interface ILifecycle : IInitializable, IDestroyable;
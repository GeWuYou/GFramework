// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     定义异步生命周期接口，组合了异步初始化和异步销毁
/// </summary>
public interface IAsyncLifecycle : IAsyncInitializable, IAsyncDestroyable
{
}
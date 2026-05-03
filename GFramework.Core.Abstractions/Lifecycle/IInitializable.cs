// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     可初始化接口，为需要初始化的组件提供标准初始化能力
/// </summary>
public interface IInitializable
{
    /// <summary>
    ///     初始化组件
    /// </summary>
    void Initialize();
}
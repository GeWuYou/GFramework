// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     可销毁接口，为需要资源清理的组件提供标准销毁能力
/// </summary>
public interface IDestroyable
{
    /// <summary>
    ///     销毁组件并释放资源
    /// </summary>
    void Destroy();
}
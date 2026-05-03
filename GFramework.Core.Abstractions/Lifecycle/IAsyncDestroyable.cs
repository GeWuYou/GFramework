// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     定义异步销毁接口，用于需要异步清理资源的组件
/// </summary>
public interface IAsyncDestroyable
{
    /// <summary>
    ///     异步销毁方法，在组件关闭时调用
    /// </summary>
    /// <returns>表示异步销毁操作的任务</returns>
    ValueTask DestroyAsync();
}
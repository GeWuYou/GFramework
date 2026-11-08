// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;

namespace GFramework.Core.Abstractions.Lifecycle;

/// <summary>
///     定义异步初始化接口，用于需要异步初始化的组件或服务
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    ///     异步初始化方法，用于执行组件或服务的异步初始化逻辑
    /// </summary>
    /// <param name="cancellationToken">取消当前初始化；实现应等待其已开始的工作安全结束。</param>
    /// <returns>表示异步初始化操作的Task</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

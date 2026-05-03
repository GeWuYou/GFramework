// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Core.Abstractions.Rule;

/// <summary>
///     上下文感知接口，用于为实现该接口的类提供设置和获取架构上下文的能力
/// </summary>
public interface IContextAware
{
    /// <summary>
    ///     设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文对象，用于提供架构级别的服务和功能访问</param>
    void SetContext(IArchitectureContext context);

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>当前的架构上下文对象</returns>
    IArchitectureContext GetContext();
}
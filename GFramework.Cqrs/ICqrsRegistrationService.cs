// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace GFramework.Cqrs;

/// <summary>
///     协调 CQRS 处理器程序集的接入流程。
/// </summary>
/// <remarks>
///     该服务封装“程序集去重 + 生成注册器优先 + 反射回退”的默认接入语义，
///     让 <c>GFramework.Core</c> 的容器层只保留公开入口，而不再直接维护 CQRS handler 注册细节。
/// </remarks>
public interface ICqrsRegistrationService
{
    /// <summary>
    ///     注册一个或多个程序集中的 CQRS 处理器。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    void RegisterHandlers(IEnumerable<Assembly> assemblies);
}

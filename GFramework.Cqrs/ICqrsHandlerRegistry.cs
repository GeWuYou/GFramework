// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Cqrs;

/// <summary>
///     定义由源码生成器产出的 CQRS 处理器注册器契约。
/// </summary>
/// <remarks>
///     运行时会优先调用实现该接口的程序集级注册器，以避免在冷启动阶段对整个程序集执行反射扫描。
///     当目标程序集没有生成注册器，或生成注册器因兼容性原因不可用时，运行时仍会回退到反射扫描路径。
/// </remarks>
public interface ICqrsHandlerRegistry
{
    /// <summary>
    ///     将当前程序集中的 CQRS 处理器映射注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断信息的日志器。</param>
    void Register(IServiceCollection services, ILogger logger);
}

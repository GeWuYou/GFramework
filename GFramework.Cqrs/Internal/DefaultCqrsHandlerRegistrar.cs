// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     默认的 CQRS 处理器注册器实现。
///     该适配器把标准 <see cref="IServiceCollection" /> 上的 handler 接入入口转发到现有注册流水线，
///     使组合根只依赖 <see cref="ICqrsHandlerRegistrar" /> 抽象。
/// </summary>
/// <param name="services">承载 handler 注册结果的标准服务集合。</param>
/// <param name="logger">
///     注册阶段诊断日志器；为 <see langword="null" /> 时回退到空实现，注册期诊断默认静默。
/// </param>
internal sealed class DefaultCqrsHandlerRegistrar(IServiceCollection services, ILogger? logger = null)
    : ICqrsHandlerRegistrar
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly ILogger _logger = logger ?? NullLogger<DefaultCqrsHandlerRegistrar>.Instance;

    /// <summary>
    ///     按当前 runtime 约定扫描并注册处理器程序集。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    public void RegisterHandlers(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        CqrsHandlerRegistrar.RegisterHandlers(_services, assemblies, _logger);
    }
}

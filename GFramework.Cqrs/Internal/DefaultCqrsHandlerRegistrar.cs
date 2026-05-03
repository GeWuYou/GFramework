// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     默认的 CQRS 处理器注册器实现。
///     该适配器把容器公开的 handler 接入入口转发到现有的注册流水线，
///     使容器主路径只依赖 <see cref="ICqrsHandlerRegistrar" /> 抽象。
/// </summary>
internal sealed class DefaultCqrsHandlerRegistrar(IIocContainer container, ILogger logger) : ICqrsHandlerRegistrar
{
    private readonly IIocContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     按当前 runtime 约定扫描并注册处理器程序集。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    public void RegisterHandlers(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        CqrsHandlerRegistrar.RegisterHandlers(_container, assemblies, _logger);
    }
}

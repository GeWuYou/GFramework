// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     默认的 CQRS 程序集注册协调器。
/// </summary>
/// <remarks>
///     该实现把“按稳定程序集键去重”和“委托给 handler registrar 执行实际映射注册”收敛到 CQRS runtime 内部，
///     避免外层容器继续了解 handler 注册流水线的内部结构。
///     <para>
///         该类型不是线程安全的。调用方应在外部同步边界内访问 <see cref="RegisterHandlers" />，
///         例如由容器写锁串行化程序集注册流程。
///     </para>
/// </remarks>
internal sealed class DefaultCqrsRegistrationService(ICqrsHandlerRegistrar registrar, ILogger logger)
    : ICqrsRegistrationService
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly HashSet<string> _registeredAssemblyKeys = new(StringComparer.Ordinal);
    private readonly ICqrsHandlerRegistrar _registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));

    /// <summary>
    ///     注册指定程序集中的 CQRS handlers。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    public void RegisterHandlers(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var processedAssemblyKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assembly in assemblies
                     .Where(static assembly => assembly is not null)
                     .OrderBy(GetAssemblyRegistrationKey, StringComparer.Ordinal))
        {
            var assemblyKey = GetAssemblyRegistrationKey(assembly);
            if (!processedAssemblyKeys.Add(assemblyKey))
                continue;

            if (_registeredAssemblyKeys.Contains(assemblyKey))
            {
                _logger.Debug(
                    $"Skipping CQRS handler registration for assembly {assemblyKey} because it was already registered.");
                continue;
            }

            _registrar.RegisterHandlers([assembly]);
            _registeredAssemblyKeys.Add(assemblyKey);
        }
    }

    /// <summary>
    ///     生成稳定程序集键，避免相同程序集被不同 <see cref="Assembly" /> 实例重复接入。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>稳定的程序集标识。</returns>
    private static string GetAssemblyRegistrationKey(Assembly assembly)
    {
        return assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
    }
}

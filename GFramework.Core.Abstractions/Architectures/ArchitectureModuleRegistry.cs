// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     架构模块注册表 - 用于外部模块的自动注册
/// </summary>
public static class ArchitectureModuleRegistry
{
    private static readonly ConcurrentDictionary<string, Func<IServiceModule>> Factories = new(StringComparer.Ordinal);

    /// <summary>
    ///     注册模块工厂（幂等操作，相同模块名只会注册一次）
    /// </summary>
    /// <param name="factory">模块工厂函数</param>
    public static void Register(Func<IServiceModule> factory)
    {
        // 创建临时实例以获取模块名（用于幂等性检查）
        var tempModule = factory();
        var moduleName = tempModule.ModuleName;

        // 幂等注册：相同模块名只注册一次
        Factories.TryAdd(moduleName, factory);
    }

    /// <summary>
    ///     创建所有已注册的模块实例
    /// </summary>
    /// <returns>模块实例集合</returns>
    public static IEnumerable<IServiceModule> CreateModules()
    {
        return Factories.Values.Select(f => f());
    }

    /// <summary>
    ///     清空注册表（主要用于测试）
    /// </summary>
    public static void Clear()
    {
        Factories.Clear();
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构模块管理器
///     负责管理架构模块的安装
/// </summary>
internal sealed class ArchitectureModules(
    IArchitecture architecture,
    ILogger logger)
{
    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        var name = module.GetType().Name;
        logger.Debug($"Installing module: {name}");
        module.Install(architecture);
        logger.Info($"Module installed: {name}");
        return module;
    }
}

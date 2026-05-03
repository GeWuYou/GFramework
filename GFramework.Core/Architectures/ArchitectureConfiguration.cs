// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     默认架构配置类，实现IArchitectureConfiguration接口
///     提供日志工厂、日志级别和架构选项的默认配置
/// </summary>
public sealed class ArchitectureConfiguration : IArchitectureConfiguration
{
    /// <summary>
    ///     获取或设置日志选项
    ///     默认配置为Info级别日志，使用控制台日志工厂提供程序
    /// </summary>
    public LoggerProperties LoggerProperties { get; set; } = new()
    {
        LoggerFactoryProvider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Info
        }
    };

    /// <summary>
    ///     获取或设置架构选项
    ///     默认创建新的ArchitectureOptions实例
    /// </summary>
    public ArchitectureProperties ArchitectureProperties { get; set; } = new()
    {
        AllowLateRegistration = false,
        StrictPhaseValidation = true
    };
}
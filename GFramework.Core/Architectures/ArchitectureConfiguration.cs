// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Properties;

namespace GFramework.Core.Architectures;

/// <summary>
///     默认架构配置类，实现IArchitectureConfiguration接口
///     提供日志工厂、日志级别和架构选项的默认配置
/// </summary>
public sealed class ArchitectureConfiguration : IArchitectureConfiguration
{
    /// <summary>
    ///     获取或设置日志选项。
    ///     默认不绑定工厂：架构在构造时创建独立的默认控制台工厂并负责释放。
    /// </summary>
    public LoggerProperties LoggerProperties { get; set; } = new();

    /// <summary>
    ///     获取或设置架构选项
    ///     默认创建新的ArchitectureOptions实例
    /// </summary>
    public ArchitectureProperties ArchitectureProperties
    {
        get; set;
    } = new()
    {
        StrictPhaseValidation = true,
    };
}

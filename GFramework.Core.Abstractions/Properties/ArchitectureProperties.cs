// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Properties;

/// <summary>
///     架构选项配置类，用于定义架构行为的相关配置选项。
///     通过该类可以控制架构的初始化行为和运行时特性。
/// </summary>
public sealed class ArchitectureProperties
{
    /// <summary>
    ///     严格阶段验证开关，当设置为 true 时启用严格的阶段验证机制。
    ///     默认值为 false，表示不启用严格验证。
    /// </summary>
    public bool StrictPhaseValidation
    {
        get; set;
    }
}

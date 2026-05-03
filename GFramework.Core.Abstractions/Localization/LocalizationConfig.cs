// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化配置
/// </summary>
public class LocalizationConfig
{
    /// <summary>
    /// 默认语言代码
    /// </summary>
    public string DefaultLanguage { get; set; } = "eng";

    /// <summary>
    /// 回退语言代码（当目标语言缺少键时使用）
    /// </summary>
    public string FallbackLanguage { get; set; } = "eng";

    /// <summary>
    /// 本地化文件路径（Godot 资源路径）
    /// </summary>
    public string LocalizationPath { get; set; } = "res://localization";

    /// <summary>
    /// 用户覆盖文件路径（用于热更新和自定义翻译）
    /// </summary>
    public string OverridePath { get; set; } = "user://localization_override";

    /// <summary>
    /// 是否启用热重载（监视覆盖文件变化）
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// 是否在加载时验证本地化文件
    /// </summary>
    public bool ValidateOnLoad { get; set; } = true;
}
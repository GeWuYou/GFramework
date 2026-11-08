// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化键未找到异常
/// </summary>
/// <remarks>
/// 初始化键未找到异常
/// </remarks>
/// <param name="tableName">表名</param>
/// <param name="key">键名</param>
public class LocalizationKeyNotFoundException(string tableName, string key) : LocalizationException($"Localization key '{key}' not found in table '{tableName}'")
{

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; } = tableName;

    /// <summary>
    /// 键名
    /// </summary>
    public string Key { get; } = key;
}

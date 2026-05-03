// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化键未找到异常
/// </summary>
public class LocalizationKeyNotFoundException : LocalizationException
{
    /// <summary>
    /// 初始化键未找到异常
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="key">键名</param>
    public LocalizationKeyNotFoundException(string tableName, string key)
        : base($"Localization key '{key}' not found in table '{tableName}'")
    {
        TableName = tableName;
        Key = key;
    }

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// 键名
    /// </summary>
    public string Key { get; }
}
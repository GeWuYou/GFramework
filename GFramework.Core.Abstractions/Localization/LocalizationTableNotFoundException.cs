// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化表未找到异常
/// </summary>
public class LocalizationTableNotFoundException : LocalizationException
{
    /// <summary>
    /// 初始化表未找到异常
    /// </summary>
    /// <param name="tableName">表名</param>
    public LocalizationTableNotFoundException(string tableName)
        : base($"Localization table '{tableName}' not found")
    {
        TableName = tableName;
    }

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; }
}
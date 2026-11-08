// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化表未找到异常
/// </summary>
/// <remarks>
/// 初始化表未找到异常
/// </remarks>
/// <param name="tableName">表名</param>
public class LocalizationTableNotFoundException(string tableName) : LocalizationException($"Localization table '{tableName}' not found")
{

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; } = tableName;
}

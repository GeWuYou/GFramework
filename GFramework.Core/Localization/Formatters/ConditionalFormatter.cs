// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Localization;

namespace GFramework.Core.Localization.Formatters;

/// <summary>
/// 条件格式化器
/// 格式: {condition:if:trueText|falseText}
/// 示例: {upgraded:if:Upgraded|Normal}
/// </summary>
public class ConditionalFormatter : ILocalizationFormatter
{
    /// <inheritdoc/>
    public string Name => "if";

    /// <inheritdoc/>
    public bool TryFormat(string format, object value, IFormatProvider? provider, out string result)
    {
        result = string.Empty;

        try
        {
            var parts = format.Split('|');

            if (parts.Length != 2)
            {
                return false;
            }

            var condition = value is bool b ? b : Convert.ToBoolean(value);
            result = condition ? parts[0] : parts[1];
            return true;
        }
        catch
        {
            return false;
        }
    }
}
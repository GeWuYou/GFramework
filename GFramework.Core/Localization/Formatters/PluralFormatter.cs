// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Localization;

namespace GFramework.Core.Localization.Formatters;

/// <summary>
/// 复数格式化器
/// 格式: {count:plural:singular|plural}
/// 示例: {count:plural:item|items}
/// </summary>
public class PluralFormatter : ILocalizationFormatter
{
    /// <inheritdoc/>
    public string Name => "plural";

    /// <inheritdoc/>
    public bool TryFormat(string format, object value, IFormatProvider? provider, out string result)
    {
        result = string.Empty;

        if (value is not IConvertible convertible)
        {
            return false;
        }

        try
        {
            var number = convertible.ToDecimal(provider);
            var parts = format.Split('|');

            if (parts.Length != 2)
            {
                return false;
            }

            result = Math.Abs(number) == 1 ? parts[0] : parts[1];
            return true;
        }
        catch
        {
            return false;
        }
    }
}
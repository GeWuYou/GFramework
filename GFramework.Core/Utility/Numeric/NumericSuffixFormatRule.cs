using System.Globalization;
using System.Numerics;
using GFramework.Core.Abstractions.Utility.Numeric;

namespace GFramework.Core.Utility.Numeric;

/// <summary>
/// 基于后缀阈值表的数值缩写规则。
/// </summary>
public sealed class NumericSuffixFormatRule : INumericFormatRule
{
    private readonly NumericSuffixThreshold[] _thresholds;

    /// <summary>
    /// 初始化后缀缩写规则。
    /// </summary>
    /// <param name="name">规则名称。</param>
    /// <param name="thresholds">阈值表。</param>
    public NumericSuffixFormatRule(string name, IEnumerable<NumericSuffixThreshold> thresholds)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(thresholds);

        Name = name;
        _thresholds = thresholds.OrderBy(entry => entry.Divisor).ToArray();

        if (_thresholds.Length == 0)
        {
            throw new ArgumentException("至少需要一个缩写阈值。", nameof(thresholds));
        }

        ValidateThresholds(_thresholds);
    }

    /// <summary>
    /// 默认国际缩写规则。
    /// </summary>
    public static NumericSuffixFormatRule InternationalCompact { get; } = new(
        "compact",
        [
            new NumericSuffixThreshold(1_000m, "K"),
            new NumericSuffixThreshold(1_000_000m, "M"),
            new NumericSuffixThreshold(1_000_000_000m, "B"),
            new NumericSuffixThreshold(1_000_000_000_000m, "T")
        ]);

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public bool TryFormat<T>(T value, NumericFormatOptions options, out string result)
    {
        ArgumentNullException.ThrowIfNull(options);
        NumericDisplayFormatter.NormalizeOptions(options);

        if (TryFormatSpecialFloatingPoint(value, options.FormatProvider, out result))
        {
            return true;
        }

        object? boxedValue = value;
        if (boxedValue is null)
        {
            result = string.Empty;
            return false;
        }

        return boxedValue switch
        {
            byte byteValue => TryFormatDecimal(byteValue, options, out result),
            sbyte sbyteValue => TryFormatDecimal(sbyteValue, options, out result),
            short shortValue => TryFormatDecimal(shortValue, options, out result),
            ushort ushortValue => TryFormatDecimal(ushortValue, options, out result),
            int intValue => TryFormatDecimal(intValue, options, out result),
            uint uintValue => TryFormatDecimal(uintValue, options, out result),
            long longValue => TryFormatDecimal(longValue, options, out result),
            ulong ulongValue => TryFormatDecimal(ulongValue, options, out result),
            nint nativeIntValue => TryFormatDecimal(nativeIntValue, options, out result),
            nuint nativeUIntValue => TryFormatDecimal(nativeUIntValue, options, out result),
            decimal decimalValue => TryFormatDecimal(decimalValue, options, out result),
            float floatValue => TryFormatDouble(floatValue, options, out result),
            double doubleValue => TryFormatDouble(doubleValue, options, out result),
            BigInteger bigIntegerValue => TryFormatBigInteger(bigIntegerValue, options, out result),
            _ => TryFormatConvertible(boxedValue, options, out result)
        };
    }

    private static void ValidateThresholds(IReadOnlyList<NumericSuffixThreshold> thresholds)
    {
        decimal? previousDivisor = null;

        foreach (var threshold in thresholds)
        {
            if (threshold.Divisor <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholds), "阈值除数必须大于 0。");
            }

            if (string.IsNullOrWhiteSpace(threshold.Suffix))
            {
                throw new ArgumentException("阈值后缀不能为空。", nameof(thresholds));
            }

            if (previousDivisor.HasValue && threshold.Divisor <= previousDivisor.Value)
            {
                throw new ArgumentException("阈值除数必须严格递增。", nameof(thresholds));
            }

            previousDivisor = threshold.Divisor;
        }
    }

    private static bool TryFormatSpecialFloatingPoint<T>(
        T value,
        IFormatProvider? provider,
        out string result)
    {
        object? boxedValue = value;
        if (boxedValue is null)
        {
            result = string.Empty;
            return false;
        }

        switch (boxedValue)
        {
            case float floatValue when float.IsNaN(floatValue) || float.IsInfinity(floatValue):
                result = floatValue.ToString(null, provider);
                return true;
            case double doubleValue when double.IsNaN(doubleValue) || double.IsInfinity(doubleValue):
                result = doubleValue.ToString(null, provider);
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }

    private bool TryFormatConvertible(object value, NumericFormatOptions options, out string result)
    {
        if (value is not IConvertible convertible)
        {
            result = string.Empty;
            return false;
        }

        try
        {
            var decimalValue = convertible.ToDecimal(options.FormatProvider ?? CultureInfo.InvariantCulture);
            return TryFormatDecimal(decimalValue, options, out result);
        }
        catch
        {
            result = string.Empty;
            return false;
        }
    }

    private bool TryFormatBigInteger(BigInteger value, NumericFormatOptions options, out string result)
    {
        try
        {
            return TryFormatDecimal((decimal)value, options, out result);
        }
        catch (OverflowException)
        {
            try
            {
                return TryFormatDouble((double)value, options, out result);
            }
            catch (OverflowException)
            {
                result = value.ToString(options.FormatProvider ?? CultureInfo.CurrentCulture);
                return true;
            }
        }
    }

    private bool TryFormatDecimal(decimal value, NumericFormatOptions options, out string result)
    {
        var absoluteValue = Math.Abs(value);

        if (absoluteValue < options.CompactThreshold)
        {
            result = FormatPlainDecimal(value, options);
            return true;
        }

        var suffixIndex = FindThresholdIndex(absoluteValue);
        if (suffixIndex < 0)
        {
            result = FormatPlainDecimal(value, options);
            return true;
        }

        var scaledValue = RoundScaledDecimal(absoluteValue, suffixIndex, options, out suffixIndex);
        result = ComposeResult(value < 0m, FormatDecimalCore(scaledValue, options, false), suffixIndex);
        return true;
    }

    private bool TryFormatDouble(double value, NumericFormatOptions options, out string result)
    {
        var absoluteValue = Math.Abs(value);

        if (absoluteValue < (double)options.CompactThreshold)
        {
            result = FormatPlainDouble(value, options);
            return true;
        }

        var suffixIndex = FindThresholdIndex(absoluteValue);
        if (suffixIndex < 0)
        {
            result = FormatPlainDouble(value, options);
            return true;
        }

        var scaledValue = RoundScaledDouble(absoluteValue, suffixIndex, options, out suffixIndex);
        result = ComposeResult(value < 0d, FormatDoubleCore(scaledValue, options, false), suffixIndex);
        return true;
    }

    private string ComposeResult(bool negative, string numericPart, int suffixIndex)
    {
        return $"{(negative ? "-" : string.Empty)}{numericPart}{_thresholds[suffixIndex].Suffix}";
    }

    private int FindThresholdIndex(decimal absoluteValue)
    {
        for (var i = _thresholds.Length - 1; i >= 0; i--)
        {
            if (absoluteValue >= _thresholds[i].Divisor)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindThresholdIndex(double absoluteValue)
    {
        for (var i = _thresholds.Length - 1; i >= 0; i--)
        {
            if (absoluteValue >= (double)_thresholds[i].Divisor)
            {
                return i;
            }
        }

        return -1;
    }

    private decimal RoundScaledDecimal(decimal absoluteValue, int suffixIndex, NumericFormatOptions options,
        out int resolvedIndex)
    {
        resolvedIndex = suffixIndex;
        var roundedValue = RoundDecimal(absoluteValue / _thresholds[resolvedIndex].Divisor, options);

        while (resolvedIndex < _thresholds.Length - 1)
        {
            var promoteThreshold = _thresholds[resolvedIndex + 1].Divisor / _thresholds[resolvedIndex].Divisor;
            if (roundedValue < promoteThreshold)
            {
                break;
            }

            resolvedIndex++;
            roundedValue = RoundDecimal(absoluteValue / _thresholds[resolvedIndex].Divisor, options);
        }

        return roundedValue;
    }

    private double RoundScaledDouble(double absoluteValue, int suffixIndex, NumericFormatOptions options,
        out int resolvedIndex)
    {
        resolvedIndex = suffixIndex;
        var roundedValue = RoundDouble(absoluteValue / (double)_thresholds[resolvedIndex].Divisor, options);

        while (resolvedIndex < _thresholds.Length - 1)
        {
            var promoteThreshold =
                (double)(_thresholds[resolvedIndex + 1].Divisor / _thresholds[resolvedIndex].Divisor);
            if (roundedValue < promoteThreshold)
            {
                break;
            }

            resolvedIndex++;
            roundedValue = RoundDouble(absoluteValue / (double)_thresholds[resolvedIndex].Divisor, options);
        }

        return roundedValue;
    }

    private static decimal RoundDecimal(decimal value, NumericFormatOptions options)
    {
        return Math.Round(value, options.MaxDecimalPlaces, options.MidpointRounding);
    }

    private static double RoundDouble(double value, NumericFormatOptions options)
    {
        return Math.Round(value, options.MaxDecimalPlaces, options.MidpointRounding);
    }

    private static string FormatPlainDecimal(decimal value, NumericFormatOptions options)
    {
        return FormatDecimalCore(RoundDecimal(value, options), options, options.UseGroupingBelowThreshold);
    }

    private static string FormatPlainDouble(double value, NumericFormatOptions options)
    {
        return FormatDoubleCore(RoundDouble(value, options), options, options.UseGroupingBelowThreshold);
    }

    private static string FormatDecimalCore(decimal value, NumericFormatOptions options, bool useGrouping)
    {
        return value.ToString(BuildFormatString(options, useGrouping), options.FormatProvider);
    }

    private static string FormatDoubleCore(double value, NumericFormatOptions options, bool useGrouping)
    {
        return value.ToString(BuildFormatString(options, useGrouping), options.FormatProvider);
    }

    private static string BuildFormatString(NumericFormatOptions options, bool useGrouping)
    {
        var integerPart = useGrouping ? "#,0" : "0";

        if (options.MaxDecimalPlaces == 0)
        {
            return integerPart;
        }

        if (!options.TrimTrailingZeros)
        {
            var fixedDigits = Math.Max(options.MaxDecimalPlaces, options.MinDecimalPlaces);
            return $"{integerPart}.{new string('0', fixedDigits)}";
        }

        var requiredDigits = new string('0', options.MinDecimalPlaces);
        var optionalDigits = new string('#', options.MaxDecimalPlaces - options.MinDecimalPlaces);
        return $"{integerPart}.{requiredDigits}{optionalDigits}";
    }
}
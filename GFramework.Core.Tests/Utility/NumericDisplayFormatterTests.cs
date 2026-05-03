// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Core.Abstractions.Utility.Numeric;
using GFramework.Core.Extensions;
using GFramework.Core.Utility.Numeric;

namespace GFramework.Core.Tests.Utility;

[TestFixture]
public class NumericDisplayFormatterTests
{
    [Test]
    public void FormatCompact_ShouldReturnPlainText_WhenValueIsBelowThreshold()
    {
        var result = NumericDisplay.FormatCompact(950);

        Assert.That(result, Is.EqualTo("950"));
    }

    [Test]
    public void FormatCompact_ShouldFormatInt_AsCompactText()
    {
        var result = NumericDisplay.FormatCompact(1_200);

        Assert.That(result, Is.EqualTo("1.2K"));
    }

    [Test]
    public void FormatCompact_ShouldFormatLong_AsCompactText()
    {
        var result = NumericDisplay.FormatCompact(1_000_000L);

        Assert.That(result, Is.EqualTo("1M"));
    }

    [Test]
    public void FormatCompact_ShouldFormatDecimal_AsCompactText()
    {
        var result = NumericDisplay.FormatCompact(1_234.56m);

        Assert.That(result, Is.EqualTo("1.2K"));
    }

    [Test]
    public void FormatCompact_ShouldFormatNegativeValues()
    {
        var result = NumericDisplay.FormatCompact(-1_250);

        Assert.That(result, Is.EqualTo("-1.3K"));
    }

    [Test]
    public void FormatCompact_ShouldPromoteRoundedBoundary_ToNextSuffix()
    {
        var result = NumericDisplay.FormatCompact(999_950);

        Assert.That(result, Is.EqualTo("1M"));
    }

    [Test]
    public void Format_ShouldRespectFormatProvider()
    {
        var result = NumericDisplay.Format(1_234.5m, new NumericFormatOptions
        {
            CompactThreshold = 10_000m,
            FormatProvider = CultureInfo.GetCultureInfo("de-DE")
        });

        Assert.That(result, Is.EqualTo("1234,5"));
    }

    [Test]
    public void Format_ShouldUseGroupingBelowThreshold_WhenEnabled()
    {
        var result = NumericDisplay.Format(12_345, new NumericFormatOptions
        {
            CompactThreshold = 1_000_000m,
            UseGroupingBelowThreshold = true,
            FormatProvider = CultureInfo.InvariantCulture
        });

        Assert.That(result, Is.EqualTo("12,345"));
    }

    [Test]
    public void Format_ShouldSupportCustomSuffixRule()
    {
        var rule = new NumericSuffixFormatRule("custom",
        [
            new NumericSuffixThreshold(10m, "X"),
            new NumericSuffixThreshold(100m, "Y")
        ]);

        var result = NumericDisplay.Format(123, new NumericFormatOptions
        {
            Rule = rule,
            CompactThreshold = 10m,
            FormatProvider = CultureInfo.InvariantCulture
        });

        Assert.That(result, Is.EqualTo("1.2Y"));
    }

    [Test]
    public void Format_ShouldHandlePositiveInfinity()
    {
        var result = NumericDisplay.Format(double.PositiveInfinity, new NumericFormatOptions
        {
            FormatProvider = CultureInfo.InvariantCulture
        });

        Assert.That(result, Is.EqualTo("Infinity"));
    }

    [Test]
    public void Format_ObjectOverload_ShouldDispatchToNumericFormatter()
    {
        var result = NumericDisplay.Format((object)1_234m);

        Assert.That(result, Is.EqualTo("1.2K"));
    }

    [Test]
    public void ToCompactString_ShouldUseNumericExtension()
    {
        var result = 15_320.ToCompactString();

        Assert.That(result, Is.EqualTo("15.3K"));
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Filters;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 CompositeFilter 的功能和行为
/// </summary>
[TestFixture]
public class CompositeFilterTests
{
    [Test]
    public void Constructor_WithNullFilters_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CompositeFilter(null!));
    }

    [Test]
    public void Constructor_WithEmptyFilters_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CompositeFilter(Array.Empty<ILogFilter>()));
    }

    [Test]
    public void ShouldLog_WithAllFiltersReturningTrue_ShouldReturnTrue()
    {
        var filter1 = new LogLevelFilter(LogLevel.Info);
        var filter2 = new NamespaceFilter("GFramework");
        var compositeFilter = new CompositeFilter(filter1, filter2);

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "GFramework.Core", "Test", null, null);

        Assert.That(compositeFilter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithOneFilterReturningFalse_ShouldReturnFalse()
    {
        var filter1 = new LogLevelFilter(LogLevel.Warning); // 要求 Warning 以上
        var filter2 = new NamespaceFilter("GFramework");
        var compositeFilter = new CompositeFilter(filter1, filter2);

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "GFramework.Core", "Test", null, null);

        Assert.That(compositeFilter.ShouldLog(entry), Is.False);
    }

    [Test]
    public void ShouldLog_WithAllFiltersReturningFalse_ShouldReturnFalse()
    {
        var filter1 = new LogLevelFilter(LogLevel.Warning);
        var filter2 = new NamespaceFilter("MyApp");
        var compositeFilter = new CompositeFilter(filter1, filter2);

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "GFramework.Core", "Test", null, null);

        Assert.That(compositeFilter.ShouldLog(entry), Is.False);
    }

    [Test]
    public void ShouldLog_WithMultipleFilters_ShouldApplyAndLogic()
    {
        var levelFilter = new LogLevelFilter(LogLevel.Info);
        var namespaceFilter = new NamespaceFilter("GFramework");
        var compositeFilter = new CompositeFilter(levelFilter, namespaceFilter);

        // 满足所有条件
        var entry1 = new LogEntry(DateTime.UtcNow, LogLevel.Info, "GFramework.Core", "Test", null, null);
        Assert.That(compositeFilter.ShouldLog(entry1), Is.True);

        // 级别不满足
        var entry2 = new LogEntry(DateTime.UtcNow, LogLevel.Debug, "GFramework.Core", "Test", null, null);
        Assert.That(compositeFilter.ShouldLog(entry2), Is.False);

        // 命名空间不满足
        var entry3 = new LogEntry(DateTime.UtcNow, LogLevel.Info, "OtherNamespace", "Test", null, null);
        Assert.That(compositeFilter.ShouldLog(entry3), Is.False);

        // 都不满足
        var entry4 = new LogEntry(DateTime.UtcNow, LogLevel.Debug, "OtherNamespace", "Test", null, null);
        Assert.That(compositeFilter.ShouldLog(entry4), Is.False);
    }

    [Test]
    public void ShouldLog_WithNestedCompositeFilters_ShouldWork()
    {
        var filter1 = new LogLevelFilter(LogLevel.Info);
        var filter2 = new NamespaceFilter("GFramework");
        var innerComposite = new CompositeFilter(filter1, filter2);

        var filter3 = new LogLevelFilter(LogLevel.Warning);
        var outerComposite = new CompositeFilter(innerComposite, filter3);

        // 需要同时满足：Info 以上 AND GFramework 命名空间 AND Warning 以上
        var entry1 = new LogEntry(DateTime.UtcNow, LogLevel.Warning, "GFramework.Core", "Test", null, null);
        Assert.That(outerComposite.ShouldLog(entry1), Is.True);

        var entry2 = new LogEntry(DateTime.UtcNow, LogLevel.Info, "GFramework.Core", "Test", null, null);
        Assert.That(outerComposite.ShouldLog(entry2), Is.False); // 不满足 Warning
    }
}
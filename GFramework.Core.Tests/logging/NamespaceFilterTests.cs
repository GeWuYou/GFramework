using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.filters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 NamespaceFilter 的功能和行为
/// </summary>
[TestFixture]
public class NamespaceFilterTests
{
    [Test]
    public void Constructor_WithNullNamespaces_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NamespaceFilter(null!));
    }

    [Test]
    public void Constructor_WithEmptyNamespaces_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new NamespaceFilter(Array.Empty<string>()));
    }

    [Test]
    public void ShouldLog_WithMatchingNamespace_ShouldReturnTrue()
    {
        var filter = new NamespaceFilter("GFramework.Core", "MyApp");
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "GFramework.Core.Logging", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithNonMatchingNamespace_ShouldReturnFalse()
    {
        var filter = new NamespaceFilter("GFramework.Core", "MyApp");
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "OtherNamespace", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.False);
    }

    [Test]
    public void ShouldLog_WithExactMatch_ShouldReturnTrue()
    {
        var filter = new NamespaceFilter("GFramework.Core");
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "GFramework.Core", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithPrefixMatch_ShouldReturnTrue()
    {
        var filter = new NamespaceFilter("GFramework");
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "GFramework.Core.Logging", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_IsCaseInsensitive()
    {
        var filter = new NamespaceFilter("gframework.core");
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "GFramework.Core.Logging", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithMultipleNamespaces_ShouldMatchAny()
    {
        var filter = new NamespaceFilter("GFramework.Core", "MyApp.Services", "ThirdParty");

        var entry1 = new LogEntry(DateTime.Now, LogLevel.Info, "GFramework.Core.Logging", "Test", null, null);
        var entry2 = new LogEntry(DateTime.Now, LogLevel.Info, "MyApp.Services.UserService", "Test", null, null);
        var entry3 = new LogEntry(DateTime.Now, LogLevel.Info, "ThirdParty.Library", "Test", null, null);
        var entry4 = new LogEntry(DateTime.Now, LogLevel.Info, "OtherNamespace", "Test", null, null);

        Assert.That(filter.ShouldLog(entry1), Is.True);
        Assert.That(filter.ShouldLog(entry2), Is.True);
        Assert.That(filter.ShouldLog(entry3), Is.True);
        Assert.That(filter.ShouldLog(entry4), Is.False);
    }
}
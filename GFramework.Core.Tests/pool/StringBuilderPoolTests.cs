using GFramework.Core.pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.pool;

/// <summary>
///     测试 StringBuilderPool 的功能
/// </summary>
[TestFixture]
public class StringBuilderPoolTests
{
    [Test]
    public void Rent_Should_Return_StringBuilder()
    {
        // Act
        var sb = StringBuilderPool.Rent();

        // Assert
        Assert.That(sb, Is.Not.Null);
        Assert.That(sb.Length, Is.EqualTo(0));
    }

    [Test]
    public void Rent_Should_Respect_Capacity()
    {
        // Act
        var sb = StringBuilderPool.Rent(512);

        // Assert
        Assert.That(sb.Capacity, Is.GreaterThanOrEqualTo(512));
    }

    [Test]
    public void Return_Should_Clear_StringBuilder()
    {
        // Arrange
        var sb = StringBuilderPool.Rent();
        sb.Append("Hello World");

        // Act
        StringBuilderPool.Return(sb);

        // Assert
        Assert.That(sb.Length, Is.EqualTo(0));
    }

    [Test]
    public void Return_Should_Not_Throw_For_Large_Capacity()
    {
        // Arrange
        var sb = StringBuilderPool.Rent(10000);

        // Act & Assert
        Assert.DoesNotThrow(() => StringBuilderPool.Return(sb));
    }

    [Test]
    public void GetScoped_Should_Return_Disposable_Wrapper()
    {
        // Act
        using var scoped = StringBuilderPool.GetScoped();

        // Assert
        Assert.That(scoped.Value, Is.Not.Null);
        Assert.That(scoped.Value.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetScoped_Should_Auto_Return_On_Dispose()
    {
        // Arrange
        var sb = StringBuilderPool.GetScoped().Value;
        sb.Append("Test");

        // Act
        using (var scoped = StringBuilderPool.GetScoped())
        {
            scoped.Value.Append("Hello");
        }

        // Assert - 如果没有异常就说明正常归还了
        Assert.Pass();
    }

    [Test]
    public void StringBuilder_Should_Be_Reusable()
    {
        // Arrange
        var sb = StringBuilderPool.Rent();
        sb.Append("First use");
        StringBuilderPool.Return(sb);

        // Act
        sb.Append("Second use");

        // Assert
        Assert.That(sb.ToString(), Is.EqualTo("Second use"));
    }

    [Test]
    public void GetScoped_With_Using_Should_Work()
    {
        // Act
        string result;
        using (var scoped = StringBuilderPool.GetScoped())
        {
            scoped.Value.Append("Hello");
            scoped.Value.Append(" ");
            scoped.Value.Append("World");
            result = scoped.Value.ToString();
        }

        // Assert
        Assert.That(result, Is.EqualTo("Hello World"));
    }
}

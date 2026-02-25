using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 CollectionExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class CollectionExtensionsTests
{
    [Test]
    public void ForEach_Should_Execute_Action_For_Each_Element()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var sum = 0;

        // Act
        numbers.ForEach(n => sum += n);

        // Assert
        Assert.That(sum, Is.EqualTo(15));
    }

    [Test]
    public void ForEach_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<int>? numbers = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => numbers!.ForEach(n => { }));
    }

    [Test]
    public void ForEach_Should_Throw_ArgumentNullException_When_Action_Is_Null()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => numbers.ForEach(null!));
    }

    [Test]
    public void IsNullOrEmpty_Should_Return_True_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<int>? numbers = null;

        // Act
        var result = numbers.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_Should_Return_True_When_Source_Is_Empty()
    {
        // Arrange
        var numbers = Array.Empty<int>();

        // Act
        var result = numbers.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_Should_Return_False_When_Source_Has_Elements()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act
        var result = numbers.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void WhereNotNull_Should_Filter_Out_Null_Elements()
    {
        // Arrange
        var items = new string?[] { "a", null, "b", null, "c" };

        // Act
        var result = items.WhereNotNull().ToArray();

        // Assert
        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void WhereNotNull_Should_Return_Empty_Collection_When_All_Elements_Are_Null()
    {
        // Arrange
        var items = new string?[] { null, null, null };

        // Act
        var result = items.WhereNotNull().ToArray();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhereNotNull_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<string?>? items = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => items!.WhereNotNull().ToArray());
    }

    [Test]
    public void ToDictionarySafe_Should_Create_Dictionary()
    {
        // Arrange
        var items = new[] { ("a", 1), ("b", 2), ("c", 3) };

        // Act
        var result = items.ToDictionarySafe(x => x.Item1, x => x.Item2);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result["a"], Is.EqualTo(1));
        Assert.That(result["b"], Is.EqualTo(2));
        Assert.That(result["c"], Is.EqualTo(3));
    }

    [Test]
    public void ToDictionarySafe_Should_Overwrite_Duplicate_Keys()
    {
        // Arrange
        var items = new[] { ("a", 1), ("b", 2), ("a", 3) };

        // Act
        var result = items.ToDictionarySafe(x => x.Item1, x => x.Item2);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result["a"], Is.EqualTo(3)); // 最后一个值
        Assert.That(result["b"], Is.EqualTo(2));
    }

    [Test]
    public void ToDictionarySafe_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<(string, int)>? items = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            items!.ToDictionarySafe(x => x.Item1, x => x.Item2));
    }

    [Test]
    public void ToDictionarySafe_Should_Throw_ArgumentNullException_When_KeySelector_Is_Null()
    {
        // Arrange
        var items = new[] { ("a", 1), ("b", 2) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            items.ToDictionarySafe<(string, int), string, int>(null!, x => x.Item2));
    }

    [Test]
    public void ToDictionarySafe_Should_Throw_ArgumentNullException_When_ValueSelector_Is_Null()
    {
        // Arrange
        var items = new[] { ("a", 1), ("b", 2) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            items.ToDictionarySafe<(string, int), string, int>(x => x.Item1, null!));
    }
}

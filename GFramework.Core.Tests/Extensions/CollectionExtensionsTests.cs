using GFramework.Core.Extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 CollectionExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class CollectionExtensionsTests
{
    /// <summary>
    ///     测试ForEach方法对每个元素执行指定操作
    /// </summary>
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

    /// <summary>
    ///     测试ForEach方法在源为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ForEach_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<int>? numbers = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => numbers!.ForEach(n => { }));
    }

    /// <summary>
    ///     测试ForEach方法在操作为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ForEach_Should_Throw_ArgumentNullException_When_Action_Is_Null()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => numbers.ForEach(null!));
    }

    /// <summary>
    ///     测试IsNullOrEmpty方法在源为null时返回true
    /// </summary>
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

    /// <summary>
    ///     测试IsNullOrEmpty方法在源为空时返回true
    /// </summary>
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

    /// <summary>
    ///     测试IsNullOrEmpty方法在源有元素时返回false
    /// </summary>
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

    /// <summary>
    ///     测试WhereNotNull方法过滤掉null元素
    /// </summary>
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

    /// <summary>
    ///     测试WhereNotNull方法在所有元素都为null时返回空集合
    /// </summary>
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

    /// <summary>
    ///     测试WhereNotNull方法在源为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void WhereNotNull_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<string?>? items = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => items!.WhereNotNull().ToArray());
    }

    /// <summary>
    ///     测试ToDictionarySafe方法创建字典
    /// </summary>
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

    /// <summary>
    ///     测试ToDictionarySafe保持具体Dictionary返回类型，避免公开API继续收窄。
    /// </summary>
    [Test]
    public void ToDictionarySafe_Should_Preserve_Concrete_Return_Type()
    {
        var method = typeof(GFramework.Core.Extensions.CollectionExtensions)
            .GetMethods()
            .Single(static method => string.Equals(method.Name, nameof(GFramework.Core.Extensions.CollectionExtensions.ToDictionarySafe), StringComparison.Ordinal));
        var methodGenericArguments = method.GetGenericArguments();
        var returnTypeGenericArguments = method.ReturnType.GetGenericArguments();

        Assert.Multiple(() =>
        {
            Assert.That(method.IsGenericMethodDefinition, Is.True);
            Assert.That(method.ReturnType.IsGenericType, Is.True);
            Assert.That(method.ReturnType.GetGenericTypeDefinition(), Is.EqualTo(typeof(Dictionary<,>)));
            Assert.That(methodGenericArguments.Select(static argument => argument.Name), Is.EqualTo(new[] { "T", "TKey", "TValue" }));
            Assert.That(returnTypeGenericArguments, Has.Length.EqualTo(2));
            Assert.That(returnTypeGenericArguments[0], Is.SameAs(methodGenericArguments[1]));
            Assert.That(returnTypeGenericArguments[1], Is.SameAs(methodGenericArguments[2]));
        });
    }

    /// <summary>
    ///     测试ToDictionarySafe方法在存在重复键时覆盖前面的值
    /// </summary>
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

    /// <summary>
    ///     测试ToDictionarySafe方法在源为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ToDictionarySafe_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        // Arrange
        IEnumerable<(string, int)>? items = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            items!.ToDictionarySafe(x => x.Item1, x => x.Item2));
    }

    /// <summary>
    ///     测试ToDictionarySafe方法在键选择器为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ToDictionarySafe_Should_Throw_ArgumentNullException_When_KeySelector_Is_Null()
    {
        // Arrange
        var items = new[] { ("a", 1), ("b", 2) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            items.ToDictionarySafe<(string, int), string, int>(null!, x => x.Item2));
    }

    /// <summary>
    ///     测试ToDictionarySafe方法在值选择器为null时抛出ArgumentNullException
    /// </summary>
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

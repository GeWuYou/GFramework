// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Functional.Functions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional.Functions;

/// <summary>
///     FunctionExtensions扩展方法测试类，用于验证高级函数式编程扩展方法的正确性
///     包括柯里化、偏函数应用、重复执行、安全执行和缓存等功能的测试
/// </summary>
[TestFixture]
public class FunctionExtensionsTests
{
    /// <summary>
    ///     测试Partial方法 - 验证部分应用函数功能
    /// </summary>
    [Test]
    public void Partial_Should_Fix_First_Argument_Of_Binary_Function()
    {
        // Arrange
        Func<int, int, int> multiply = (x, y) => x * y;

        // Act
        var doubleFunction = multiply.Partial(2);
        var result = doubleFunction(5);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    /// <summary>
    ///     测试Repeat方法 - 验证重复执行函数功能
    /// </summary>
    [Test]
    public void Repeat_Should_Execute_Function_N_Times()
    {
        // Arrange
        var initialValue = 2;

        // Act
        var result = initialValue.Repeat(3, x => x * 2);

        // Assert
        // 2 -> 4 -> 8 -> 16 (3次重复)
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void Compose_Should_Apply_Functions_In_Reverse_Order()
    {
        // Arrange
        int AddOne(int x) => x + 1;
        Func<int, int> multiplyTwo = x => x * 2;

        // Act
        var composed = multiplyTwo.Compose((Func<int, int>)AddOne); // (x + 1) * 2

        // Assert
        Assert.That(composed(5), Is.EqualTo(12)); // (5 + 1) * 2 = 12
    }

    [Test]
    public void Compose_WithNullOuterFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, int> addOne = x => x + 1;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((Func<int, int>)null!).Compose(addOne));
    }

    [Test]
    public void Compose_WithNullInnerFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, int> multiplyTwo = x => x * 2;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            multiplyTwo.Compose<int, int, int>(null!));
    }

    [Test]
    public void AndThen_Should_Apply_Functions_In_Order()
    {
        // Arrange
        Func<int, int> addOne = x => x + 1;
        int MultiplyTwo(int x) => x * 2;

        // Act
        var chained = addOne.AndThen((Func<int, int>)MultiplyTwo); // (x + 1) * 2

        // Assert
        Assert.That(chained(5), Is.EqualTo(12)); // (5 + 1) * 2 = 12
    }

    [Test]
    public void AndThen_WithNullFirstFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, int> multiplyTwo = x => x * 2;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((Func<int, int>)null!).AndThen(multiplyTwo));
    }

    [Test]
    public void AndThen_WithNullSecondFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, int> addOne = x => x + 1;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            addOne.AndThen<int, int, int>(null!));
    }

    [Test]
    public void Curry_TwoParameters_Should_Return_Nested_Functions()
    {
        // Arrange
        Func<int, int, int> add = (x, y) => x + y;

        // Act
        var curriedAdd = add.Curry();
        var add5 = curriedAdd(5);
        var result = add5(3);

        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Curry_ThreeParameters_Should_Return_Nested_Functions()
    {
        // Arrange
        Func<int, int, int, int> add3 = (x, y, z) => x + y + z;

        // Act
        var curriedAdd = add3.Curry();
        var result = curriedAdd(1)(2)(3);

        // Assert
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Curry_WithNullFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, int, int> func = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => func.Curry());
    }

    [Test]
    public void Uncurry_Should_Restore_Multi_Parameter_Function()
    {
        // Arrange
        Func<int, Func<int, int>> curriedAdd = x => y => x + y;

        // Act
        var add = curriedAdd.Uncurry();
        var result = add(5, 3);

        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Uncurry_WithNullFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int, Func<int, int>> func = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => func.Uncurry());
    }

    [Test]
    public void Curry_Then_Uncurry_Should_Be_Identity()
    {
        // Arrange
        Func<int, int, int> original = (x, y) => x * y;

        // Act
        var restored = original.Curry().Uncurry();
        var result = restored(6, 7);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Defer_Should_Not_Execute_Immediately()
    {
        // Arrange
        var executed = false;
        Func<int> func = () =>
        {
            executed = true;
            return 42;
        };

        // Act
        var lazy = func.Defer();

        // Assert
        Assert.That(executed, Is.False);
        Assert.That(lazy.Value, Is.EqualTo(42));
        Assert.That(executed, Is.True);
    }

    [Test]
    public void Defer_WithNullFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int> func = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => func.Defer());
    }

    [Test]
    public void Once_Should_Execute_Function_Only_Once()
    {
        // Arrange
        var counter = 0;
        Func<int> func = () => ++counter;

        // Act
        var once = func.Once();
        var result1 = once();
        var result2 = once();
        var result3 = once();

        // Assert
        Assert.That(result1, Is.EqualTo(1));
        Assert.That(result2, Is.EqualTo(1));
        Assert.That(result3, Is.EqualTo(1));
        Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public void Once_WithNullFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Func<int> func = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => func.Once());
    }

    [Test]
    public void Once_Should_Be_Thread_Safe()
    {
        // Arrange
        var counter = 0;
        Func<int> func = () => Interlocked.Increment(ref counter);

        var once = func.Once();

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => once()))
            .ToArray();

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert
        Assert.That(counter, Is.EqualTo(1));
        Assert.That(tasks.Select(t => t.Result).Distinct().Count(), Is.EqualTo(1));
    }
}
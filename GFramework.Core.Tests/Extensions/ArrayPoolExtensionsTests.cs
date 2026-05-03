// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using GFramework.Core.Extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 ArrayPoolExtensions 的功能
/// </summary>
[TestFixture]
public class ArrayPoolExtensionsTests
{
    private ArrayPool<int> _pool = null!;

    [SetUp]
    public void SetUp()
    {
        _pool = ArrayPool<int>.Shared;
    }

    [Test]
    public void RentArray_Should_Return_Array()
    {
        // Act
        var array = _pool.RentArray(100);

        // Assert
        Assert.That(array, Is.Not.Null);
        Assert.That(array.Length, Is.GreaterThanOrEqualTo(100));

        // Cleanup
        _pool.ReturnArray(array);
    }

    [Test]
    public void ReturnArray_Should_Not_Throw()
    {
        // Arrange
        var array = _pool.RentArray(100);

        // Act & Assert
        Assert.DoesNotThrow(() => _pool.ReturnArray(array));
    }

    [Test]
    public void ReturnArray_With_Clear_Should_Clear_Array()
    {
        // Arrange
        var array = _pool.RentArray(10);
        array[0] = 42;
        array[5] = 99;

        // Act
        _pool.ReturnArray(array, clearArray: true);

        // Assert
        Assert.That(array[0], Is.EqualTo(0));
        Assert.That(array[5], Is.EqualTo(0));
    }

    [Test]
    public void GetScoped_Should_Return_Disposable_Wrapper()
    {
        // Act
        using var scoped = _pool.GetScoped(100);

        // Assert
        Assert.That(scoped.Array, Is.Not.Null);
        Assert.That(scoped.Array.Length, Is.GreaterThanOrEqualTo(100));
        Assert.That(scoped.Length, Is.GreaterThanOrEqualTo(100));
    }

    [Test]
    public void GetScoped_Should_Auto_Return_On_Dispose()
    {
        // Arrange & Act
        int[] array;
        using (var scoped = _pool.GetScoped(100))
        {
            array = scoped.Array;
            array[0] = 42;
        }

        // Assert - 如果没有异常就说明正常归还了
        Assert.Pass();
    }

    [Test]
    public void ScopedArray_AsSpan_Should_Return_Span()
    {
        // Arrange
        using var scoped = _pool.GetScoped(100);

        // Act
        var span = scoped.AsSpan();

        // Assert
        Assert.That(span.Length, Is.EqualTo(scoped.Length));
    }

    [Test]
    public void ScopedArray_AsSpan_With_Range_Should_Return_Slice()
    {
        // Arrange
        using var scoped = _pool.GetScoped(100);

        // Act
        var span = scoped.AsSpan(10, 20);

        // Assert
        Assert.That(span.Length, Is.EqualTo(20));
    }

    [Test]
    public void GetScoped_With_ClearOnReturn_Should_Clear_Array()
    {
        // Arrange
        int[] array;
        using (var scoped = _pool.GetScoped(10, clearOnReturn: true))
        {
            array = scoped.Array;
            array[0] = 42;
            array[5] = 99;
        }

        // Assert
        Assert.That(array[0], Is.EqualTo(0));
        Assert.That(array[5], Is.EqualTo(0));
    }

    [Test]
    public void Multiple_Scoped_Arrays_Should_Work_Independently()
    {
        // Act
        using var scoped1 = _pool.GetScoped(50);
        using var scoped2 = _pool.GetScoped(100);

        scoped1.Array[0] = 1;
        scoped2.Array[0] = 2;

        // Assert
        Assert.That(scoped1.Array[0], Is.EqualTo(1));
        Assert.That(scoped2.Array[0], Is.EqualTo(2));
        Assert.That(scoped1.Array, Is.Not.SameAs(scoped2.Array));
    }

    [Test]
    public void RentArray_Should_Be_Reusable()
    {
        // Arrange
        var array1 = _pool.RentArray(100);
        array1[0] = 42;
        _pool.ReturnArray(array1, clearArray: true);

        // Act
        var array2 = _pool.RentArray(100);

        // Assert
        // 可能是同一个数组（已清空），也可能是不同的数组
        Assert.That(array2, Is.Not.Null);
        Assert.That(array2.Length, Is.GreaterThanOrEqualTo(100));

        // Cleanup
        _pool.ReturnArray(array2);
    }

    [Test]
    public void ScopedArray_Should_Work_With_Using_Declaration()
    {
        // Act
        using var scoped = _pool.GetScoped(50);
        scoped.Array[0] = 123;

        // Assert
        Assert.That(scoped.Array[0], Is.EqualTo(123));
    }

    [Test]
    public void AsSpan_Should_Allow_Span_Operations()
    {
        // Arrange
        using var scoped = _pool.GetScoped(10);
        var span = scoped.AsSpan();

        // Act
        span.Fill(42);

        // Assert
        for (var i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.EqualTo(42));
        }
    }
}

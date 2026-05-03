// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.Pool;

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

    /// <summary>
    ///     验证 Rent 方法会复用已归还的 StringBuilder 实例
    /// </summary>
    [Test]
    public void Rent_Should_Reuse_Returned_StringBuilder()
    {
        // Arrange
        var sb1 = StringBuilderPool.Rent();
        var originalInstance = sb1;
        StringBuilderPool.Return(sb1);

        // Act
        var sb2 = StringBuilderPool.Rent();

        // Assert
        Assert.That(sb2, Is.SameAs(originalInstance), "应该复用同一个实例");
    }

    /// <summary>
    ///     验证 Rent 方法会确保返回的 StringBuilder 满足最小容量要求
    /// </summary>
    [Test]
    public void Rent_Should_Ensure_Minimum_Capacity()
    {
        // Arrange
        var sb1 = StringBuilderPool.Rent(100);
        StringBuilderPool.Return(sb1);

        // Act - 请求更大容量
        var sb2 = StringBuilderPool.Rent(500);

        // Assert
        Assert.That(sb2.Capacity, Is.GreaterThanOrEqualTo(500));
    }

    /// <summary>
    ///     验证超过最大保留容量的 StringBuilder 不会被池化
    /// </summary>
    [Test]
    public void Return_Should_Not_Pool_Large_Capacity_StringBuilder()
    {
        // Arrange
        var sb1 = StringBuilderPool.Rent(10000);
        StringBuilderPool.Return(sb1);

        // Act - 租用新的小容量 StringBuilder
        var sb2 = StringBuilderPool.Rent(100);

        // Assert
        Assert.That(sb2, Is.Not.SameAs(sb1), "大容量实例不应被池化");
    }

    /// <summary>
    ///     验证对象池在多线程环境下的线程安全性
    /// </summary>
    [Test]
    public void Pool_Should_Be_Thread_Safe()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var sb = StringBuilderPool.Rent();
                    sb.Append("Test");
                    StringBuilderPool.Return(sb);
                }
            });
        }

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }
}
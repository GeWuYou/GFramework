// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;

namespace GFramework.Core.Tests.Pool;

/// <summary>
///     对象池功能测试类，用于验证对象池的基本操作和行为
/// </summary>
[TestFixture]
public class ObjectPoolTests
{
    /// <summary>
    ///     测试初始化方法，在每个测试方法执行前设置测试环境
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _pool = new TestObjectPool();
    }

    /// <summary>
    ///     测试用的对象池实例
    /// </summary>
    private TestObjectPool _pool = null!;

    /// <summary>
    ///     验证当对象池为空时，Acquire方法应该创建新对象
    /// </summary>
    [Test]
    public void Acquire_Should_Create_New_Object_When_Pool_Empty()
    {
        var obj = _pool.Acquire("test");

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.PoolKey, Is.EqualTo("test"));
        Assert.That(obj.OnAcquireCalled, Is.True);
    }

    /// <summary>
    ///     验证Acquire方法应该返回池中的可用对象
    /// </summary>
    [Test]
    public void Acquire_Should_Return_Pooled_Object()
    {
        var obj1 = _pool.Acquire("test");
        obj1.TestValue = 100;

        _pool.Release("test", obj1);

        var obj2 = _pool.Acquire("test");

        Assert.That(obj2, Is.SameAs(obj1));
        Assert.That(obj2.TestValue, Is.EqualTo(100));
        Assert.That(obj2.OnAcquireCalled, Is.True);
    }

    /// <summary>
    ///     验证Release方法应该调用对象的OnRelease回调
    /// </summary>
    [Test]
    public void Release_Should_Call_OnRelease()
    {
        var obj = _pool.Acquire("test");

        _pool.Release("test", obj);

        Assert.That(obj.OnReleaseCalled, Is.True);
    }

    /// <summary>
    ///     验证Clear方法应该销毁所有对象
    /// </summary>
    [Test]
    public void Clear_Should_Destroy_All_Objects()
    {
        var obj1 = _pool.Acquire("key1");
        var obj2 = _pool.Acquire("key2");

        _pool.Release("key1", obj1);
        _pool.Release("key2", obj2);

        _pool.Clear();

        Assert.That(obj1.OnPoolDestroyCalled, Is.True);
        Assert.That(obj2.OnPoolDestroyCalled, Is.True);
    }

    /// <summary>
    ///     验证多个池键应该相互独立
    /// </summary>
    [Test]
    public void Multiple_Pools_Should_Be_Independent()
    {
        var obj1 = _pool.Acquire("key1");
        var obj2 = _pool.Acquire("key2");

        _pool.Release("key1", obj1);

        var obj3 = _pool.Acquire("key1");
        var obj4 = _pool.Acquire("key2");

        Assert.That(obj3, Is.SameAs(obj1));
        Assert.That(obj4, Is.Not.SameAs(obj2));
    }

    /// <summary>
    ///     验证OnAcquire回调应该在新对象和池中对象上都被调用
    /// </summary>
    [Test]
    public void OnAcquire_Should_Be_Called_On_New_And_Pooled_Objects()
    {
        var obj1 = _pool.Acquire("test");
        Assert.That(obj1.OnAcquireCalled, Is.True);

        _pool.Release("test", obj1);
        obj1.OnAcquireCalled = false;

        var obj2 = _pool.Acquire("test");
        Assert.That(obj2.OnAcquireCalled, Is.True);
    }

    /// <summary>
    ///     验证GetPoolSize应该返回正确的池大小
    /// </summary>
    [Test]
    public void GetPoolSize_Should_Return_Correct_Size()
    {
        // Arrange
        var obj1 = _pool.Acquire("test");
        var obj2 = _pool.Acquire("test");

        // Act & Assert
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(0));

        _pool.Release("test", obj1);
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(1));

        _pool.Release("test", obj2);
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(2));
    }

    /// <summary>
    ///     验证GetActiveCount应该返回正确的活跃对象数量
    /// </summary>
    [Test]
    public void GetActiveCount_Should_Return_Correct_Count()
    {
        // Arrange & Act & Assert
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(0));

        var obj1 = _pool.Acquire("test");
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(1));

        var obj2 = _pool.Acquire("test");
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(2));

        _pool.Release("test", obj1);
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(1));

        _pool.Release("test", obj2);
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(0));
    }

    /// <summary>
    ///     验证SetMaxCapacity应该限制池的最大容量
    /// </summary>
    [Test]
    public void SetMaxCapacity_Should_Limit_Pool_Size()
    {
        // Arrange
        _pool.SetMaxCapacity("test", 2);
        var obj1 = _pool.Acquire("test");
        var obj2 = _pool.Acquire("test");
        var obj3 = _pool.Acquire("test");

        // Act
        _pool.Release("test", obj1);
        _pool.Release("test", obj2);
        _pool.Release("test", obj3);

        // Assert
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(2));
        Assert.That(obj3.OnPoolDestroyCalled, Is.True);
    }

    /// <summary>
    ///     验证Prewarm应该预创建指定数量的对象
    /// </summary>
    [Test]
    public void Prewarm_Should_Create_Objects_In_Advance()
    {
        // Act
        _pool.Prewarm("test", 5);

        // Assert
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(5));

        var obj = _pool.Acquire("test");
        Assert.That(_pool.GetPoolSize("test"), Is.EqualTo(4));
        Assert.That(obj.OnReleaseCalled, Is.True); // 预热时调用了OnRelease
    }

    /// <summary>
    ///     验证GetStatistics应该返回正确的统计信息
    /// </summary>
    [Test]
    public void GetStatistics_Should_Return_Correct_Statistics()
    {
        // Arrange
        _pool.SetMaxCapacity("test", 2);
        _pool.Prewarm("test", 2);

        var obj1 = _pool.Acquire("test");
        var obj2 = _pool.Acquire("test");
        var obj3 = _pool.Acquire("test"); // 这个会创建新对象

        _pool.Release("test", obj1);
        _pool.Release("test", obj2);
        _pool.Release("test", obj3); // 这个会被销毁

        // Act
        var stats = _pool.GetStatistics("test");

        // Assert
        Assert.That(stats.AvailableCount, Is.EqualTo(2));
        Assert.That(stats.ActiveCount, Is.EqualTo(0));
        Assert.That(stats.MaxCapacity, Is.EqualTo(2));
        Assert.That(stats.TotalCreated, Is.EqualTo(3)); // 2个预热 + 1个新创建
        Assert.That(stats.TotalAcquired, Is.EqualTo(3));
        Assert.That(stats.TotalReleased, Is.EqualTo(3));
        Assert.That(stats.TotalDestroyed, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证不存在的池应该返回空统计信息
    /// </summary>
    [Test]
    public void GetStatistics_Should_Return_Empty_For_Nonexistent_Pool()
    {
        // Act
        var stats = _pool.GetStatistics("nonexistent");

        // Assert
        Assert.That(stats.AvailableCount, Is.EqualTo(0));
        Assert.That(stats.ActiveCount, Is.EqualTo(0));
        Assert.That(stats.MaxCapacity, Is.EqualTo(0));
        Assert.That(stats.TotalCreated, Is.EqualTo(0));
        Assert.That(stats.TotalAcquired, Is.EqualTo(0));
        Assert.That(stats.TotalReleased, Is.EqualTo(0));
        Assert.That(stats.TotalDestroyed, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证双重释放不会导致 ActiveCount 变为负数
    /// </summary>
    [Test]
    public void Release_Should_Not_Make_ActiveCount_Negative_On_Double_Release()
    {
        // Arrange
        var obj = _pool.Acquire("test");
        _pool.Release("test", obj);

        // Act - 双重释放
        _pool.Release("test", obj);

        // Assert
        Assert.That(_pool.GetActiveCount("test"), Is.EqualTo(0));
    }

    /// <summary>
    ///     验证使用错误的 key 释放不会影响原 key 的 ActiveCount
    /// </summary>
    [Test]
    public void Release_With_Wrong_Key_Should_Not_Affect_Original_Key_ActiveCount()
    {
        // Arrange
        var obj = _pool.Acquire("key1");

        // Act - 使用错误的 key 释放
        _pool.Release("key2", obj);

        // Assert
        Assert.That(_pool.GetActiveCount("key1"), Is.EqualTo(1));
        Assert.That(_pool.GetActiveCount("key2"), Is.EqualTo(0));
    }
}

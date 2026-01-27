using GFramework.Core.Abstractions.pool;
using GFramework.Core.pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.pool;

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
}

/// <summary>
///     测试用对象池实现类，继承自AbstractObjectPoolSystem
/// </summary>
public class TestObjectPool : AbstractObjectPoolSystem<string, TestPoolableObject>
{
    /// <summary>
    ///     创建新的池化对象
    /// </summary>
    /// <param name="key">用于标识对象的键</param>
    /// <returns>新创建的TestPoolableObject实例</returns>
    protected override TestPoolableObject Create(string key)
    {
        return new TestPoolableObject { PoolKey = key };
    }

    /// <summary>
    ///     初始化方法，用于对象池初始化时的操作
    /// </summary>
    protected override void OnInit()
    {
    }
}

/// <summary>
///     测试用池化对象类，实现了IPoolableObject接口
/// </summary>
public class TestPoolableObject : IPoolableObject
{
    /// <summary>
    ///     获取或设置对象的池键
    /// </summary>
    public string PoolKey { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置测试用的整数值
    /// </summary>
    public int TestValue { get; set; }

    /// <summary>
    ///     获取或设置OnAcquire方法是否被调用的标志
    /// </summary>
    public bool OnAcquireCalled { get; set; }

    /// <summary>
    ///     获取或设置OnRelease方法是否被调用的标志
    /// </summary>
    public bool OnReleaseCalled { get; set; }

    /// <summary>
    ///     获取或设置OnPoolDestroy方法是否被调用的标志
    /// </summary>
    public bool OnPoolDestroyCalled { get; set; }

    /// <summary>
    ///     对象被获取时的回调方法
    /// </summary>
    public void OnAcquire()
    {
        OnAcquireCalled = true;
    }

    /// <summary>
    ///     对象被释放时的回调方法
    /// </summary>
    public void OnRelease()
    {
        OnReleaseCalled = true;
    }

    /// <summary>
    ///     对象被销毁时的回调方法
    /// </summary>
    public void OnPoolDestroy()
    {
        OnPoolDestroyCalled = true;
    }
}
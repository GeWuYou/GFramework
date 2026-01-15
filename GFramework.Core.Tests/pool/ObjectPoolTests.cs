using GFramework.Core.Abstractions.pool;
using GFramework.Core.pool;
using NUnit.Framework;

namespace GFramework.Core.Tests.pool;

[TestFixture]
public class ObjectPoolTests
{
    [SetUp]
    public void SetUp()
    {
        _pool = new TestObjectPool();
    }

    private TestObjectPool _pool = null!;

    [Test]
    public void Acquire_Should_Create_New_Object_When_Pool_Empty()
    {
        var obj = _pool.Acquire("test");

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.PoolKey, Is.EqualTo("test"));
        Assert.That(obj.OnAcquireCalled, Is.True);
    }

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

    [Test]
    public void Release_Should_Call_OnRelease()
    {
        var obj = _pool.Acquire("test");

        _pool.Release("test", obj);

        Assert.That(obj.OnReleaseCalled, Is.True);
    }

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

public class TestObjectPool : AbstractObjectPoolSystem<string, TestPoolableObject>
{
    protected override TestPoolableObject Create(string key)
    {
        return new TestPoolableObject { PoolKey = key };
    }

    protected override void OnInit()
    {
    }
}

public class TestPoolableObject : IPoolableObject
{
    public string PoolKey { get; set; } = string.Empty;
    public int TestValue { get; set; }
    public bool OnAcquireCalled { get; set; }
    public bool OnReleaseCalled { get; set; }
    public bool OnPoolDestroyCalled { get; set; }

    public void OnAcquire()
    {
        OnAcquireCalled = true;
    }

    public void OnRelease()
    {
        OnReleaseCalled = true;
    }

    public void OnPoolDestroy()
    {
        OnPoolDestroyCalled = true;
    }
}
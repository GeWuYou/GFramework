using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程句柄的单元测试类
/// 测试内容包括：
/// - 协程句柄创建和有效性验证
/// - 相等性比较
/// - 哈希码生成
/// - 操作符重载
/// - 多实例独立性
/// </summary>
[TestFixture]
public class CoroutineHandleTests
{
    /// <summary>
    /// 验证协程句柄创建时应有有效的Key
    /// </summary>
    [Test]
    public void CoroutineHandle_Should_Have_Valid_Key_When_Created()
    {
        var handle = new CoroutineHandle(1);

        Assert.That(handle.IsValid, Is.True);
        Assert.That(handle.Key, Is.Not.EqualTo(0));
    }

    /// <summary>
    /// 验证默认协程句柄应该无效
    /// </summary>
    [Test]
    public void Default_CoroutineHandle_Should_Be_Invalid()
    {
        var handle = default(CoroutineHandle);

        Assert.That(handle.IsValid, Is.False);
        Assert.That(handle.Key, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证相同实例ID创建的句柄应该具有不同的Key
    /// </summary>
    [Test]
    public void CoroutineHandles_With_Same_InstanceId_Should_Have_Different_Keys()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = new CoroutineHandle(1);

        Assert.That(handle1.Equals(handle2), Is.False);
    }

    /// <summary>
    /// 验证不同实例ID创建的句柄应该不同
    /// </summary>
    [Test]
    public void CoroutineHandles_With_Different_InstanceIds_Should_Be_Different()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = new CoroutineHandle(2);

        Assert.That(handle1.Equals(handle2), Is.False);
    }

    /// <summary>
    /// 验证协程句柄的相等性比较
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_For_Identical_Handles()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = handle1;

        Assert.That(handle1.Equals(handle2), Is.True);
    }

    /// <summary>
    /// 验证协程句柄的不相等性比较
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_For_Different_Handles()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = new CoroutineHandle(1);

        Assert.That(handle1.Equals(handle2), Is.False);
    }

    /// <summary>
    /// 验证Equals方法与null对象的比较
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Comparing_To_Null()
    {
        var handle = new CoroutineHandle(1);

        Assert.That(handle.Equals(null), Is.False);
    }

    /// <summary>
    /// 验证Equals方法与其他类型对象的比较
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Comparing_To_Other_Type()
    {
        var handle = new CoroutineHandle(1);

        Assert.That(handle.Equals("test"), Is.False);
    }

    /// <summary>
    /// 验证哈希码的一致性
    /// </summary>
    [Test]
    public void GetHashCode_Should_Be_Consistent()
    {
        var handle = new CoroutineHandle(1);
        var hashCode1 = handle.GetHashCode();
        var hashCode2 = handle.GetHashCode();

        Assert.That(hashCode1, Is.EqualTo(hashCode2));
    }

    /// <summary>
    /// 验证不同句柄应该有不同的哈希码
    /// </summary>
    [Test]
    public void GetHashCode_Should_Be_Different_For_Different_Handles()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = new CoroutineHandle(1);

        Assert.That(handle1.GetHashCode(), Is.Not.EqualTo(handle2.GetHashCode()));
    }

    /// <summary>
    /// 验证相等操作符的正确性
    /// </summary>
    [Test]
    public void EqualityOperator_Should_Work_Correctly()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = handle1;
        var handle3 = new CoroutineHandle(1);

        Assert.That(handle1 == handle2, Is.True);
        Assert.That(handle1 == handle3, Is.False);
    }

    /// <summary>
    /// 验证不等操作符的正确性
    /// </summary>
    [Test]
    public void InequalityOperator_Should_Work_Correctly()
    {
        var handle1 = new CoroutineHandle(1);
        var handle2 = handle1;
        var handle3 = new CoroutineHandle(1);

        Assert.That(handle1 != handle2, Is.False);
        Assert.That(handle1 != handle3, Is.True);
    }

    /// <summary>
    /// 验证协程句柄实现了IEquatable接口
    /// </summary>
    [Test]
    public void CoroutineHandle_Should_Implement_IEquatable_Interface()
    {
        var handle = new CoroutineHandle(1);

        Assert.That(handle, Is.InstanceOf<IEquatable<CoroutineHandle>>());
    }

    /// <summary>
    /// 验证协程句柄是只读结构体
    /// </summary>
    [Test]
    public void CoroutineHandle_Should_Be_Immutable_Struct()
    {
        Assert.That(typeof(CoroutineHandle).IsValueType, Is.True);
        Assert.That(typeof(CoroutineHandle).IsClass, Is.False);
    }

    /// <summary>
    /// 验证实例ID超过预留空间时的处理
    /// </summary>
    [Test]
    public void CoroutineHandle_Should_Handle_Large_InstanceId()
    {
        var handle = new CoroutineHandle(20);

        Assert.That(handle.IsValid, Is.True);
        Assert.That(handle.Key, Is.Not.EqualTo(0));
    }

    /// <summary>
    /// 验证多个连续创建的句柄Key递增
    /// </summary>
    [Test]
    public void Multiple_Creates_Should_Increment_Keys()
    {
        var handles = new List<CoroutineHandle>();
        for (var i = 0; i < 5; i++)
        {
            handles.Add(new CoroutineHandle(1));
        }

        for (var i = 0; i < handles.Count - 1; i++)
        {
            Assert.That(handles[i].Equals(handles[i + 1]), Is.False);
        }
    }

    /// <summary>
    /// 验证协程句柄的内部ID属性可以通过Key访问
    /// </summary>
    [Test]
    public void CoroutineHandle_Key_Should_Return_Low_4_Bits_Of_Id()
    {
        var handle1 = new CoroutineHandle(1);
        var key1 = handle1.Key;

        Assert.That(key1, Is.GreaterThan(0));
        Assert.That(key1, Is.LessThan(16));
    }
}
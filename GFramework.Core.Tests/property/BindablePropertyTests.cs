using GFramework.Core.property;
using NUnit.Framework;

namespace GFramework.Core.Tests.property;

/// <summary>
/// BindableProperty类的单元测试
/// </summary>
[TestFixture]
public class BindablePropertyTests
{
    /// <summary>
    /// 测试清理方法，在每个测试方法执行后重置比较器
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        BindableProperty<string>.Comparer = (a, b) => a?.Equals(b) ?? b == null;
    }

    /// <summary>
    /// 测试获取值时应返回默认值
    /// </summary>
    [Test]
    public void Value_Get_Should_Return_Default_Value()
    {
        var property = new BindableProperty<int>(5);

        Assert.That(property.Value, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试设置值时应触发事件
    /// </summary>
    [Test]
    public void Value_Set_Should_Trigger_Event()
    {
        var property = new BindableProperty<int>();
        var receivedValue = 0;

        property.Register(value => { receivedValue = value; });

        property.Value = 42;

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    /// <summary>
    /// 测试设置相同值时不触发事件
    /// </summary>
    [Test]
    public void Value_Set_To_Same_Value_Should_Not_Trigger_Event()
    {
        var property = new BindableProperty<int>(5);
        var count = 0;

        property.Register(_ => { count++; });

        property.Value = 5;

        Assert.That(count, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试取消注册应移除处理器
    /// </summary>
    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        var property = new BindableProperty<int>();
        var count = 0;

        Action<int> handler = _ => { count++; };
        property.Register(handler);

        property.Value = 1;
        Assert.That(count, Is.EqualTo(1));

        property.UnRegister(handler);
        property.Value = 2;
        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试带初始值注册应立即调用处理器
    /// </summary>
    [Test]
    public void RegisterWithInitValue_Should_Call_Handler_Immediately()
    {
        var property = new BindableProperty<int>(5);
        var receivedValue = 0;

        property.RegisterWithInitValue(value => { receivedValue = value; });

        Assert.That(receivedValue, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试无事件设置值不应触发事件
    /// </summary>
    [Test]
    public void SetValueWithoutEvent_Should_Not_Trigger_Event()
    {
        var property = new BindableProperty<int>();
        var count = 0;

        property.Register(_ => { count++; });

        property.SetValueWithoutEvent(42);

        Assert.That(count, Is.EqualTo(0));
        Assert.That(property.Value, Is.EqualTo(42));
    }

    /// <summary>
    /// 测试使用自定义比较器
    /// </summary>
    [Test]
    public void WithComparer_Should_Use_Custom_Comparer()
    {
        var comparerWasCalled = false;
        var comparisonResult = false;

        // 设置自定义比较器
        BindableProperty<string>.Comparer = (a, b) =>
        {
            comparerWasCalled = true;
            comparisonResult = a.Length == b.Length;
            return comparisonResult;
        };

        var property = new BindableProperty<string>("test");
        var count = 0;

        property.Register(_ => { count++; });
        property.Value = "test";

        Assert.That(comparerWasCalled, Is.True, "自定义比较器应该被调用");
        Assert.That(comparisonResult, Is.True, "比较结果应该是true（相同长度）");
        Assert.That(count, Is.EqualTo(0), "不应该触发事件");
    }

    /// <summary>
    /// 测试多个处理器都应被调用
    /// </summary>
    [Test]
    public void Multiple_Handlers_Should_All_Be_Called()
    {
        var property = new BindableProperty<int>();
        var count1 = 0;
        var count2 = 0;

        property.Register(_ => { count1++; });
        property.Register(_ => { count2++; });

        property.Value = 42;

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试注册应返回IUnRegister接口
    /// </summary>
    [Test]
    public void Register_Should_Return_IUnRegister()
    {
        var property = new BindableProperty<int>();
        var unRegister = property.Register(_ => { });

        Assert.That(unRegister, Is.Not.Null);
    }

    /// <summary>
    /// 测试ToString应返回值的字符串表示
    /// </summary>
    [Test]
    public void ToString_Should_Return_Value_As_String()
    {
        var property = new BindableProperty<int>(42);

        var result = property.ToString();

        Assert.That(result, Is.EqualTo("42"));
    }
}
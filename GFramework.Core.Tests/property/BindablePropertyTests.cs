using GFramework.Core.property;
using NUnit.Framework;

namespace GFramework.Core.Tests.property;

[TestFixture]
public class BindablePropertyTests
{
    [TearDown]
    public void TearDown()
    {
        BindableProperty<string>.Comparer = (a, b) => a?.Equals(b) ?? b == null;
    }

    [Test]
    public void Value_Get_Should_Return_Default_Value()
    {
        var property = new BindableProperty<int>(5);

        Assert.That(property.Value, Is.EqualTo(5));
    }

    [Test]
    public void Value_Set_Should_Trigger_Event()
    {
        var property = new BindableProperty<int>();
        var receivedValue = 0;

        property.Register(value => { receivedValue = value; });

        property.Value = 42;

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    [Test]
    public void Value_Set_To_Same_Value_Should_Not_Trigger_Event()
    {
        var property = new BindableProperty<int>(5);
        var count = 0;

        property.Register(_ => { count++; });

        property.Value = 5;

        Assert.That(count, Is.EqualTo(0));
    }

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

    [Test]
    public void RegisterWithInitValue_Should_Call_Handler_Immediately()
    {
        var property = new BindableProperty<int>(5);
        var receivedValue = 0;

        property.RegisterWithInitValue(value => { receivedValue = value; });

        Assert.That(receivedValue, Is.EqualTo(5));
    }

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

    [Test]
    public void WithComparer_Should_Use_Custom_Comparer()
    {
        var comparerWasCalled = false;
        var comparisonResult = false;

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

    [Test]
    public void Register_Should_Return_IUnRegister()
    {
        var property = new BindableProperty<int>();
        var unRegister = property.Register(_ => { });

        Assert.That(unRegister, Is.Not.Null);
    }

    [Test]
    public void ToString_Should_Return_Value_As_String()
    {
        var property = new BindableProperty<int>(42);

        var result = property.ToString();

        Assert.That(result, Is.EqualTo("42"));
    }
}
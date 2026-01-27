using GFramework.Core.events;
using GFramework.Core.property;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     注销功能测试类，用于测试不同类型的注销行为
/// </summary>
[TestFixture]
public class UnRegisterTests
{
    /// <summary>
    ///     测试DefaultUnRegister在调用注销时是否正确触发回调函数
    /// </summary>
    [Test]
    public void DefaultUnRegister_Should_InvokeCallback_When_UnRegisterCalled()
    {
        var invoked = false;
        var unRegister = new DefaultUnRegister(() => { invoked = true; });

        unRegister.UnRegister();

        Assert.That(invoked, Is.True);
    }

    /// <summary>
    ///     测试DefaultUnRegister在注销后是否清除回调函数，防止重复执行
    /// </summary>
    [Test]
    public void DefaultUnRegister_Should_ClearCallback_After_UnRegister()
    {
        var callCount = 0;
        var unRegister = new DefaultUnRegister(() => { callCount++; });

        unRegister.UnRegister();
        unRegister.UnRegister();

        Assert.That(callCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试DefaultUnRegister在传入空回调函数时不会抛出异常
    /// </summary>
    [Test]
    public void DefaultUnRegister_WithNullCallback_Should_NotThrow()
    {
        var unRegister = new DefaultUnRegister(null!);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister是否能正确从属性中注销事件处理器
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_Should_UnRegister_From_Property()
    {
        var property = new BindableProperty<int>();
        var callCount = 0;

        Action<int> handler = _ => { callCount++; };
        property.Register(handler);

        var unRegister = new BindablePropertyUnRegister<int>(property, handler);
        unRegister.UnRegister();

        property.Value = 42;

        Assert.That(callCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister在注销后是否清除内部引用
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_Should_Clear_References()
    {
        var property = new BindableProperty<int>();

        Action<int> handler = _ => { };
        var unRegister = new BindablePropertyUnRegister<int>(property, handler);

        unRegister.UnRegister();

        // 验证注销后引用被清除
        Assert.That(unRegister.BindableProperty, Is.Null);
        Assert.That(unRegister.OnValueChanged, Is.Null);
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister在传入空属性时不会抛出异常
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_WithNull_Property_Should_NotThrow()
    {
        Action<int> handler = _ => { };
        var unRegister = new BindablePropertyUnRegister<int>(null!, handler);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister在传入空处理器时不会抛出异常
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_WithNull_Handler_Should_NotThrow()
    {
        var property = new BindableProperty<int>();
        var unRegister = new BindablePropertyUnRegister<int>(property, null!);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }
}
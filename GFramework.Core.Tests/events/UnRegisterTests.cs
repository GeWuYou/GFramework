using GFramework.Core.events;
using GFramework.Core.property;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

[TestFixture]
public class UnRegisterTests
{
    [Test]
    public void DefaultUnRegister_Should_InvokeCallback_When_UnRegisterCalled()
    {
        var invoked = false;
        var unRegister = new DefaultUnRegister(() => { invoked = true; });

        unRegister.UnRegister();

        Assert.That(invoked, Is.True);
    }

    [Test]
    public void DefaultUnRegister_Should_ClearCallback_After_UnRegister()
    {
        var callCount = 0;
        var unRegister = new DefaultUnRegister(() => { callCount++; });

        unRegister.UnRegister();
        unRegister.UnRegister();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void DefaultUnRegister_WithNullCallback_Should_NotThrow()
    {
        var unRegister = new DefaultUnRegister(null!);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }

    [Test]
    public void BindablePropertyUnRegister_Should_UnRegister_From_Property()
    {
        var property = new BindableProperty<int>(0);
        var callCount = 0;

        Action<int> handler = _ => { callCount++; };
        property.Register(handler);

        var unRegister = new BindablePropertyUnRegister<int>(property, handler);
        unRegister.UnRegister();

        property.Value = 42;

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void BindablePropertyUnRegister_Should_Clear_References()
    {
        var property = new BindableProperty<int>(0);

        Action<int> handler = _ => { };
        var unRegister = new BindablePropertyUnRegister<int>(property, handler);

        unRegister.UnRegister();

        Assert.That(unRegister.BindableProperty, Is.Null);
        Assert.That(unRegister.OnValueChanged, Is.Null);
    }

    [Test]
    public void BindablePropertyUnRegister_WithNull_Property_Should_NotThrow()
    {
        Action<int> handler = _ => { };
        var unRegister = new BindablePropertyUnRegister<int>(null!, handler);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }

    [Test]
    public void BindablePropertyUnRegister_WithNull_Handler_Should_NotThrow()
    {
        var property = new BindableProperty<int>(0);
        var unRegister = new BindablePropertyUnRegister<int>(property, null!);

        Assert.DoesNotThrow(() => unRegister.UnRegister());
    }
}
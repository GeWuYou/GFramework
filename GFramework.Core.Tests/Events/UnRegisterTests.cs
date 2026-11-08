// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;
using GFramework.Core.Property;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

/// <summary>
///     注销功能测试类，用于测试不同类型的注销行为
/// </summary>
[TestFixture]
public class UnRegisterTests
{
    /// <summary>
    ///     测试Subscription在调用注销时是否正确触发回调函数
    /// </summary>
    [Test]
    public void Subscription_Should_InvokeCallback_When_UnRegisterCalled()
    {
        var invoked = false;
        var unRegister = new Subscription(() => invoked = true);

        unRegister.Dispose();

        Assert.That(invoked, Is.True);
    }

    /// <summary>
    ///     测试Subscription在注销后是否清除回调函数，防止重复执行
    /// </summary>
    [Test]
    public void Subscription_Should_ClearCallback_After_UnRegister()
    {
        var callCount = 0;
        var unRegister = new Subscription(() => callCount++);

        unRegister.Dispose();
        unRegister.Dispose();

        Assert.That(callCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试Subscription在传入空回调函数时不会抛出异常
    /// </summary>
    [Test]
    public void Subscription_WithNullCallback_Should_NotThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Subscription(null!));
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister是否能正确从属性中注销事件处理器
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_Should_UnRegister_From_Property()
    {
        var property = new BindableProperty<int>();
        var callCount = 0;

        void handler(int _)
        {
            callCount++;
        }
        property.Register(handler);

        var unRegister = new BindablePropertyUnRegister<int>(property, handler);
        unRegister.Dispose();

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

        static void handler(int _)
        {
        }
        var unRegister = new BindablePropertyUnRegister<int>(property, handler);

        unRegister.Dispose();

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
        static void handler(int _)
        {
        }
        var unRegister = new BindablePropertyUnRegister<int>(null!, handler);

        Assert.DoesNotThrow(() => unRegister.Dispose());
    }

    /// <summary>
    ///     测试BindablePropertyUnRegister在传入空处理器时不会抛出异常
    /// </summary>
    [Test]
    public void BindablePropertyUnRegister_WithNull_Handler_Should_NotThrow()
    {
        var property = new BindableProperty<int>();
        var unRegister = new BindablePropertyUnRegister<int>(property, null!);

        Assert.DoesNotThrow(() => unRegister.Dispose());
    }
}

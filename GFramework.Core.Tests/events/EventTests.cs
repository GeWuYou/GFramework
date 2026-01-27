using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     测试事件系统功能的测试类
/// </summary>
[TestFixture]
public class EventTests
{
    /// <summary>
    ///     在每个测试方法执行前进行初始化设置
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _easyEvent = new EasyEvent();
        _eventInt = new Event<int>();
        _eventIntString = new Event<int, string>();
    }

    private EasyEvent _easyEvent = null!;
    private Event<int> _eventInt = null!;
    private Event<int, string> _eventIntString = null!;

    /// <summary>
    ///     测试EasyEvent注册功能是否正确添加处理器
    /// </summary>
    [Test]
    public void EasyEvent_Register_Should_Add_Handler()
    {
        var called = false;
        _easyEvent.Register(() => { called = true; });

        _easyEvent.Trigger();

        Assert.That(called, Is.True);
    }

    /// <summary>
    ///     测试EasyEvent取消注册功能是否正确移除处理器
    /// </summary>
    [Test]
    public void EasyEvent_UnRegister_Should_Remove_Handler()
    {
        var count = 0;
        var handler = () => { count++; };

        _easyEvent.Register(handler);
        _easyEvent.Trigger();
        Assert.That(count, Is.EqualTo(1));

        _easyEvent.UnRegister(handler);
        _easyEvent.Trigger();
        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试EasyEvent多个处理器是否都能被调用
    /// </summary>
    [Test]
    public void EasyEvent_Multiple_Handlers_Should_All_Be_Called()
    {
        var count1 = 0;
        var count2 = 0;

        _easyEvent.Register(() => { count1++; });
        _easyEvent.Register(() => { count2++; });

        _easyEvent.Trigger();

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试带泛型参数的事件注册功能是否正确添加处理器
    /// </summary>
    [Test]
    public void EventT_Register_Should_Add_Handler()
    {
        var receivedValue = 0;
        _eventInt.Register(value => { receivedValue = value; });

        _eventInt.Trigger(42);

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试带泛型参数的事件取消注册功能是否正确移除处理器
    /// </summary>
    [Test]
    public void EventT_UnRegister_Should_Remove_Handler()
    {
        var count = 0;
        Action<int> handler = value => { count++; };

        _eventInt.Register(handler);
        _eventInt.Trigger(1);
        Assert.That(count, Is.EqualTo(1));

        _eventInt.UnRegister(handler);
        _eventInt.Trigger(2);
        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试带泛型参数的事件多个处理器是否都能被调用
    /// </summary>
    [Test]
    public void EventT_Multiple_Handlers_Should_All_Be_Called()
    {
        var values = new List<int>();

        _eventInt.Register(value => { values.Add(value); });
        _eventInt.Register(value => { values.Add(value * 2); });

        _eventInt.Trigger(5);

        Assert.That(values.Count, Is.EqualTo(2));
        Assert.That(values, Does.Contain(5));
        Assert.That(values, Does.Contain(10));
    }

    /// <summary>
    ///     测试带两个泛型参数的事件注册功能是否正确添加处理器
    /// </summary>
    [Test]
    public void EventTTK_Register_Should_Add_Handler()
    {
        var receivedInt = 0;
        var receivedString = string.Empty;
        _eventIntString.Register((i, s) =>
        {
            receivedInt = i;
            receivedString = s;
        });

        _eventIntString.Trigger(42, "test");

        Assert.That(receivedInt, Is.EqualTo(42));
        Assert.That(receivedString, Is.EqualTo("test"));
    }

    /// <summary>
    ///     测试带两个泛型参数的事件取消注册功能是否正确移除处理器
    /// </summary>
    [Test]
    public void EventTTK_UnRegister_Should_Remove_Handler()
    {
        var count = 0;
        Action<int, string> handler = (i, s) => { count++; };

        _eventIntString.Register(handler);
        _eventIntString.Trigger(1, "a");
        Assert.That(count, Is.EqualTo(1));

        _eventIntString.UnRegister(handler);
        _eventIntString.Trigger(2, "b");
        Assert.That(count, Is.EqualTo(1));
    }
}
using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     EasyEvents功能测试类，用于验证事件系统的注册、触发和参数传递功能
/// </summary>
[TestFixture]
public class EasyEventsTests
{
    /// <summary>
    ///     测试用例初始化方法，在每个测试方法执行前设置EasyEvents实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _easyEvents = new EasyEvents();
    }

    private EasyEvents _easyEvents = null!;

    /// <summary>
    ///     测试单参数事件的功能，验证事件能够正确接收并传递int类型参数
    /// </summary>
    [Test]
    public void Get_EventT_Should_Trigger_With_Parameter()
    {
        var receivedValue = 0;
        var @event = EasyEvents.GetOrAdd<Event<int>>();

        @event.Register(value => { receivedValue = value; });

        // 触发事件并传递参数42
        @event.Trigger(42);

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试双参数事件的功能，验证事件能够正确接收并传递int和string类型的参数
    /// </summary>
    [Test]
    public void Get_EventTTK_Should_Trigger_With_Two_Parameters()
    {
        var receivedInt = 0;
        var receivedString = string.Empty;
        var @event = EasyEvents.GetOrAdd<Event<int, string>>();

        @event.Register((i, s) =>
        {
            receivedInt = i;
            receivedString = s;
        });

        // 触发事件并传递两个参数：整数100和字符串"hello"
        @event.Trigger(100, "hello");

        Assert.That(receivedInt, Is.EqualTo(100));
        Assert.That(receivedString, Is.EqualTo("hello"));
    }
}
using GFramework.Core.Events;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

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
    ///     测试单参数事件的功能,验证事件能够正确接收并传递int类型参数
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
    ///     测试双参数事件的功能,验证事件能够正确接收并传递int和string类型的参数
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

    /// <summary>
    ///     测试并发场景下GetOrAdd的线程安全性
    /// </summary>
    [Test]
    public void GetOrAdd_Should_Be_Thread_Safe()
    {
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        var @event = _easyEvents.GetOrAddEvent<Event<int>>();
                        Assert.That(@event, Is.Not.Null);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty, $"并发测试中发生异常: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    /// <summary>
    ///     测试 AddEvent 对重复事件类型保持兼容的参数异常类型。
    /// </summary>
    [Test]
    public void AddEvent_Should_Throw_When_Already_Registered()
    {
        _easyEvents.AddEvent<Event<int>>();

        Assert.Throws<ArgumentException>(() => _easyEvents.AddEvent<Event<int>>());
    }

    /// <summary>
    ///     测试并发场景下多个不同事件类型的注册
    /// </summary>
    [Test]
    public void Concurrent_Registration_Of_Different_Event_Types_Should_Work()
    {
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new List<Exception>();

        // 每个线程注册不同类型的事件
        for (var i = 0; i < threadCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    switch (index)
                    {
                        case 0:
                            _easyEvents.GetOrAddEvent<Event<int>>();
                            break;
                        case 1:
                            _easyEvents.GetOrAddEvent<Event<string>>();
                            break;
                        case 2:
                            _easyEvents.GetOrAddEvent<Event<bool>>();
                            break;
                        case 3:
                            _easyEvents.GetOrAddEvent<Event<int, string>>();
                            break;
                        case 4:
                            _easyEvents.GetOrAddEvent<Event<double>>();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.That(exceptions, Is.Empty);

        // 验证所有事件都已注册
        Assert.That(_easyEvents.GetEvent<Event<int>>(), Is.Not.Null);
        Assert.That(_easyEvents.GetEvent<Event<string>>(), Is.Not.Null);
        Assert.That(_easyEvents.GetEvent<Event<bool>>(), Is.Not.Null);
        Assert.That(_easyEvents.GetEvent<Event<int, string>>(), Is.Not.Null);
        Assert.That(_easyEvents.GetEvent<Event<double>>(), Is.Not.Null);
    }
}

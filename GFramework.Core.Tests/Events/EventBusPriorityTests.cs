// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Events;
using NUnit.Framework;

namespace GFramework.Core.Tests.Events;

/// <summary>
///     测试事件系统优先级和传播控制功能
/// </summary>
[TestFixture]
public class EventBusPriorityTests
{
    private sealed class TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    [Test]
    public void Register_With_Priority_Should_Execute_In_Priority_Order()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(1), priority: 1);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(3), priority: 3);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(2), priority: 2);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionOrder, Is.EqualTo([3, 2, 1]));
    }

    [Test]
    public void Register_Without_Priority_Should_Use_Default_Priority()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<string>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("default"), priority: 0);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("high"), priority: 10);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionOrder[0], Is.EqualTo("high"));
        Assert.That(executionOrder[1], Is.EqualTo("default"));
    }

    [Test]
    public void Send_With_Propagation_All_Should_Execute_All_Handlers()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionCount = 0;

        eventBus.Subscribe<TestEvent>(_ => executionCount++, priority: 1);
        eventBus.Subscribe<TestEvent>(_ => executionCount++, priority: 2);
        eventBus.Subscribe<TestEvent>(_ => executionCount++, priority: 3);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionCount, Is.EqualTo(3));
    }

    [Test]
    public void Send_With_Propagation_Highest_Should_Execute_Only_Highest_Priority()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(1), priority: 1);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(3), priority: 3);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(2), priority: 2);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.Highest);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(1));
        Assert.That(executionOrder[0], Is.EqualTo(3));
    }

    [Test]
    public void Send_With_Propagation_Highest_Should_Execute_All_With_Same_Highest_Priority()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<string>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("high1"), priority: 10);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("high2"), priority: 10);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("low"), priority: 1);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.Highest);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(2));
        Assert.That(executionOrder, Does.Contain("high1"));
        Assert.That(executionOrder, Does.Contain("high2"));
        Assert.That(executionOrder, Does.Not.Contain("low"));
    }

    [Test]
    public void Negative_Priority_Should_Work_Correctly()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(-1), priority: -1);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(0), priority: 0);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add(1), priority: 1);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionOrder, Is.EqualTo([1, 0, -1]));
    }

    [Test]
    public void Multiple_Events_Should_Maintain_Independent_Priorities()
    {
        // Arrange
        var eventBus = new EventBus();
        var event1Order = new List<int>();
        var event2Order = new List<int>();

        eventBus.Subscribe<TestEvent>(_ => event1Order.Add(1), priority: 1);
        eventBus.Subscribe<TestEvent>(_ => event1Order.Add(2), priority: 2);

        eventBus.Subscribe<string>(_ => event2Order.Add(10), priority: 10);
        eventBus.Subscribe<string>(_ => event2Order.Add(20), priority: 20);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);
        eventBus.Publish("test", EventPropagation.All);

        // Assert
        Assert.That(event1Order, Is.EqualTo([2, 1]));
        Assert.That(event2Order, Is.EqualTo([20, 10]));
    }

    [Test]
    public void UnRegister_Should_Remove_Handler_From_Priority_List()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionCount = 0;
        void handler(TestEvent _) => executionCount++;

        var unregister = eventBus.Subscribe((Action<TestEvent>)handler, priority: 5);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);
        unregister.Dispose();
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionCount, Is.EqualTo(1));
    }

    [Test]
    public void Send_Without_Propagation_Should_Use_Default_Event_System()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionCount = 0;

        // 使用默认注册（无优先级）
        eventBus.Subscribe<TestEvent>(_ => executionCount++);

        // Act
        eventBus.Publish(new TestEvent()); // 不指定传播模式

        // Assert
        Assert.That(executionCount, Is.EqualTo(1));
    }

    [Test]
    public void Priority_And_Ordinary_Registrations_Share_One_Channel()
    {
        // Arrange
        var eventBus = new EventBus();
        var normalCount = 0;
        var priorityCount = 0;

        eventBus.Subscribe<TestEvent>(_ => normalCount++);
        eventBus.Subscribe<TestEvent>(_ => priorityCount++, priority: 1);

        // Act
        eventBus.Publish(new TestEvent()); // 触发普通事件
        eventBus.Publish(new TestEvent(), EventPropagation.All); // 触发优先级事件

        // Assert
        Assert.That(normalCount, Is.EqualTo(2));
        Assert.That(priorityCount, Is.EqualTo(2));
    }

    [Test]
    public void Empty_Event_Bus_Should_Not_Throw_Exception()
    {
        // Arrange
        var eventBus = new EventBus();

        // Act & Assert
        Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(), EventPropagation.All));
        Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(), EventPropagation.Highest));
    }

    [Test]
    public void Same_Priority_Handlers_Should_Execute_In_Registration_Order()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<string>();

        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("first"), priority: 5);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("second"), priority: 5);
        eventBus.Subscribe<TestEvent>(_ => executionOrder.Add("third"), priority: 5);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.All);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(["first", "second", "third"]));
    }

    [Test]
    public void UntilHandled_Should_Stop_After_MarkAsHandled()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.SubscribeWithContext<TestEvent>(ctx =>
        {
            executionOrder.Add(1);
            ctx.MarkAsHandled();
        }, priority: 10);

        eventBus.SubscribeWithContext<TestEvent>(ctx =>
        {
            executionOrder.Add(2); // 不应该执行
        }, priority: 5);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.UntilHandled);

        // Assert
        Assert.That(executionOrder, Is.EqualTo([1]));
    }

    [Test]
    public void UntilHandled_Should_Execute_All_If_Not_Handled()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.SubscribeWithContext<TestEvent>(ctx => executionOrder.Add(1), priority: 10);
        eventBus.SubscribeWithContext<TestEvent>(ctx => executionOrder.Add(2), priority: 5);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.UntilHandled);

        // Assert
        Assert.That(executionOrder, Is.EqualTo([1, 2]));
    }

    [Test]
    public void RegisterWithContext_Should_Receive_Event_Data()
    {
        // Arrange
        var eventBus = new EventBus();
        string? receivedMessage = null;

        eventBus.SubscribeWithContext<TestEvent>(ctx => { receivedMessage = ctx.Data.Message; });

        // Act
        eventBus.Publish(new TestEvent { Message = "Hello" }, EventPropagation.All);

        // Assert
        Assert.That(receivedMessage, Is.EqualTo("Hello"));
    }

    [Test]
    public void UntilHandled_Should_Respect_Priority_Order()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionOrder = new List<int>();

        eventBus.SubscribeWithContext<TestEvent>(ctx => executionOrder.Add(1), priority: 1);
        eventBus.SubscribeWithContext<TestEvent>(ctx =>
        {
            executionOrder.Add(3);
            ctx.MarkAsHandled();
        }, priority: 3);
        eventBus.SubscribeWithContext<TestEvent>(ctx => executionOrder.Add(2), priority: 2);

        // Act
        eventBus.Publish(new TestEvent(), EventPropagation.UntilHandled);

        // Assert
        Assert.That(executionOrder, Is.EqualTo([3]));
    }

    [Test]
    public void Handler_Can_Unregister_Itself_Without_Exception()
    {
        // Arrange
        var eventBus = new EventBus();
        var executionCount = 0;
        IDisposable? unregister = null;

        unregister = eventBus.Subscribe<TestEvent>(_ =>
        {
            executionCount++;
            unregister?.Dispose();
        }, priority: 1);

        // Act & Assert
        Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(), EventPropagation.All));
        Assert.That(executionCount, Is.EqualTo(1));

        // 第二次触发不应执行
        eventBus.Publish(new TestEvent(), EventPropagation.All);
        Assert.That(executionCount, Is.EqualTo(1));
    }

    [Test]
    public void Concurrent_Trigger_And_Register_Should_Be_Thread_Safe()
    {
        // Arrange
        var eventBus = new EventBus();
        var counter = 0;
        eventBus.Subscribe<TestEvent>(_ => Interlocked.Increment(ref counter), priority: 1);

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            eventBus.Publish(new TestEvent(), EventPropagation.All);
            eventBus.Subscribe<TestEvent>(_ => { }, priority: 1);
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        Assert.That(counter, Is.GreaterThan(0));
    }
}

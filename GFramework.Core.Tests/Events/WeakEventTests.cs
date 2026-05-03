// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;

namespace GFramework.Core.Tests.Events;

/// <summary>
///     弱引用事件功能测试
/// </summary>
public sealed class WeakEventTests
{
    [Test]
    public void WeakEvent_ShouldReceiveEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var listener = new EventListener();

        eventBus.RegisterWeak<TestEvent>(listener.OnEvent);

        // Act
        eventBus.SendWeak(new TestEvent { Message = "Test1" });
        eventBus.SendWeak(new TestEvent { Message = "Test2" });

        // Assert
        Assert.That(listener.ReceivedCount, Is.EqualTo(2));
    }

    [Test]
    public void WeakEvent_WhenListenerCollected_ShouldNotReceiveEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedCount = 0;

        void RegisterAndCollect()
        {
            var listener = new EventListener();
            eventBus.RegisterWeak<TestEvent>(listener.OnEvent);

            // 发送事件，监听器应该接收到
            eventBus.SendWeak(new TestEvent { Message = "Test1" });
            receivedCount = listener.ReceivedCount;
        }

        RegisterAndCollect();

        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Act - 再次发送事件
        eventBus.SendWeak(new TestEvent { Message = "Test2" });

        // Assert - 第一次应该接收到，第二次不应该接收到（因为监听器已被回收）
        Assert.That(receivedCount, Is.EqualTo(1));
    }

    [Test]
    public void WeakEvent_Cleanup_ShouldRemoveCollectedReferences()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        void RegisterAndCollect()
        {
            var listener = new EventListener();
            eventBus.RegisterWeak<TestEvent>(listener.OnEvent);
        }

        RegisterAndCollect();

        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Act
        eventBus.CleanupWeak<TestEvent>();

        // Assert - 监听器数量应该为 0
        Assert.That(eventBus.Statistics!.GetListenerCount(nameof(TestEvent)), Is.EqualTo(0));
    }

    [Test]
    public void WeakEvent_UnRegister_ShouldRemoveListener()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var listener = new EventListener();

        var unregister = eventBus.RegisterWeak<TestEvent>(listener.OnEvent);

        // Act - 发送事件
        eventBus.SendWeak(new TestEvent { Message = "Test1" });
        Assert.That(listener.ReceivedCount, Is.EqualTo(1));

        // 注销监听器
        unregister.UnRegister();

        // 再次发送事件
        eventBus.SendWeak(new TestEvent { Message = "Test2" });

        // Assert - 注销后不应该接收到事件
        Assert.That(listener.ReceivedCount, Is.EqualTo(1));
    }

    [Test]
    public void WeakEvent_MultipleListeners_ShouldAllReceiveEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var listener1 = new EventListener();
        var listener2 = new EventListener();
        var listener3 = new EventListener();

        eventBus.RegisterWeak<TestEvent>(listener1.OnEvent);
        eventBus.RegisterWeak<TestEvent>(listener2.OnEvent);
        eventBus.RegisterWeak<TestEvent>(listener3.OnEvent);

        // Act
        eventBus.SendWeak(new TestEvent { Message = "Test" });

        // Assert
        Assert.That(listener1.ReceivedCount, Is.EqualTo(1));
        Assert.That(listener2.ReceivedCount, Is.EqualTo(1));
        Assert.That(listener3.ReceivedCount, Is.EqualTo(1));
    }

    [Test]
    public void WeakEvent_WithStatistics_ShouldTrackCorrectly()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);
        var listener = new EventListener();

        eventBus.RegisterWeak<TestEvent>(listener.OnEvent);

        // Act
        eventBus.SendWeak(new TestEvent { Message = "Test1" });
        eventBus.SendWeak(new TestEvent { Message = "Test2" });

        // Assert
        Assert.That(eventBus.Statistics!.TotalPublished, Is.EqualTo(2));
        Assert.That(eventBus.Statistics.TotalHandled, Is.EqualTo(2));
        Assert.That(eventBus.Statistics.GetListenerCount(nameof(TestEvent)), Is.EqualTo(1));
    }

    [Test]
    public void WeakEvent_ExceptionInHandler_ShouldTrackFailure()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        eventBus.RegisterWeak<TestEvent>(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            eventBus.SendWeak(new TestEvent { Message = "Test" }));

        Assert.That(eventBus.Statistics!.TotalFailed, Is.EqualTo(1));
    }

    [Test]
    public void WeakEvent_AutoCleanupDuringTrigger_ShouldWork()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);
        var aliveListener = new EventListener();

        void RegisterAndCollect()
        {
            var deadListener = new EventListener();
            eventBus.RegisterWeak<TestEvent>(deadListener.OnEvent);
        }

        RegisterAndCollect();
        eventBus.RegisterWeak<TestEvent>(aliveListener.OnEvent);

        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Act - 触发事件会自动清理已回收的监听器
        eventBus.SendWeak(new TestEvent { Message = "Test" });

        // Assert - 只有存活的监听器接收到事件
        Assert.That(aliveListener.ReceivedCount, Is.EqualTo(1));
        Assert.That(eventBus.Statistics!.GetListenerCount(nameof(TestEvent)), Is.EqualTo(1));
    }

    private sealed class TestEvent
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed class EventListener
    {
        public int ReceivedCount { get; private set; }

        public void OnEvent(TestEvent e)
        {
            ReceivedCount++;
        }
    }
}
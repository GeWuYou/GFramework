// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;
using GFramework.Core.Events.Filters;

namespace GFramework.Core.Tests.Events;

/// <summary>
///     事件过滤器功能测试
/// </summary>
public sealed class EventFilterTests
{
    [Test]
    public void PredicateFilter_ShouldFilterBasedOnCondition()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new PredicateEventFilter<TestEvent>(e => e.Value < 10)); // 过滤 Value < 10 的事件

        // Act
        eventBus.SendFilterable(new TestEvent { Value = 5, Message = "Filtered" });
        eventBus.SendFilterable(new TestEvent { Value = 15, Message = "Passed" });
        eventBus.SendFilterable(new TestEvent { Value = 20, Message = "Passed" });

        // Assert
        Assert.That(receivedEvents, Has.Count.EqualTo(2));
        Assert.That(receivedEvents[0].Value, Is.EqualTo(15));
        Assert.That(receivedEvents[1].Value, Is.EqualTo(20));
    }

    [Test]
    public void SamplingFilter_ShouldFilterBasedOnSamplingRate()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new SamplingEventFilter<TestEvent>(0.5)); // 50% 采样率

        // Act
        for (var i = 0; i < 100; i++)
        {
            eventBus.SendFilterable(new TestEvent { Value = i });
        }

        // Assert - 应该接收到大约 50 个事件
        Assert.That(receivedEvents.Count, Is.InRange(45, 55));
    }

    [Test]
    public void SamplingFilter_WithZeroRate_ShouldFilterAllEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new SamplingEventFilter<TestEvent>(0.0)); // 0% 采样率

        // Act
        for (var i = 0; i < 10; i++)
        {
            eventBus.SendFilterable(new TestEvent { Value = i });
        }

        // Assert
        Assert.That(receivedEvents, Is.Empty);
    }

    [Test]
    public void SamplingFilter_WithFullRate_ShouldPassAllEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new SamplingEventFilter<TestEvent>(1.0)); // 100% 采样率

        // Act
        for (var i = 0; i < 10; i++)
        {
            eventBus.SendFilterable(new TestEvent { Value = i });
        }

        // Assert
        Assert.That(receivedEvents, Has.Count.EqualTo(10));
    }

    [Test]
    public void MultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new PredicateEventFilter<TestEvent>(e => e.Value < 10)); // 过滤 < 10
        eventBus.AddFilter(new PredicateEventFilter<TestEvent>(e => e.Value > 50)); // 过滤 > 50

        // Act
        for (var i = 0; i < 100; i++)
        {
            eventBus.SendFilterable(new TestEvent { Value = i });
        }

        // Assert - 只有 10-50 之间的事件通过
        Assert.That(receivedEvents, Has.Count.EqualTo(41)); // 10 到 50 包含边界
        Assert.That(receivedEvents.All(e => e.Value >= 10 && e.Value <= 50), Is.True);
    }

    [Test]
    public void RemoveFilter_ShouldStopFiltering()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();
        var filter = new PredicateEventFilter<TestEvent>(e => e.Value < 10);

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(filter);

        // Act - 发送事件（应该被过滤）
        eventBus.SendFilterable(new TestEvent { Value = 5 });
        Assert.That(receivedEvents, Is.Empty);

        // 移除过滤器
        eventBus.RemoveFilter(filter);

        // 再次发送事件（应该通过）
        eventBus.SendFilterable(new TestEvent { Value = 5 });

        // Assert
        Assert.That(receivedEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public void ClearFilters_ShouldRemoveAllFilters()
    {
        // Arrange
        var eventBus = new EnhancedEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.RegisterFilterable<TestEvent>(e => receivedEvents.Add(e));
        eventBus.AddFilter(new PredicateEventFilter<TestEvent>(e => e.Value < 10));
        eventBus.AddFilter(new PredicateEventFilter<TestEvent>(e => e.Value > 50));

        // Act - 发送事件（应该被过滤）
        eventBus.SendFilterable(new TestEvent { Value = 5 });
        Assert.That(receivedEvents, Is.Empty);

        // 清除所有过滤器
        eventBus.ClearFilters<TestEvent>();

        // 再次发送事件（应该通过）
        eventBus.SendFilterable(new TestEvent { Value = 5 });

        // Assert
        Assert.That(receivedEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public void SamplingFilter_InvalidRate_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SamplingEventFilter<TestEvent>(-0.1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SamplingEventFilter<TestEvent>(1.1));
    }

    [Test]
    public void PredicateFilter_NullPredicate_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PredicateEventFilter<TestEvent>(null!));
    }

    private sealed class TestEvent
    {
        public int Value { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;

namespace GFramework.Core.Tests.Events;

/// <summary>
///     事件统计功能测试
/// </summary>
public sealed class EventStatisticsTests
{
    [Test]
    public void Statistics_WhenDisabled_ShouldBeNull()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: false);

        // Act & Assert
        Assert.That(eventBus.Statistics, Is.Null);
    }

    [Test]
    public void Statistics_WhenEnabled_ShouldNotBeNull()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        // Act & Assert
        Assert.That(eventBus.Statistics, Is.Not.Null);
    }

    [Test]
    public void TotalPublished_ShouldTrackPublishedEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        // Act
        eventBus.SendFilterable(new TestEvent { Message = "Test1" });
        eventBus.SendFilterable(new TestEvent { Message = "Test2" });
        eventBus.SendFilterable(new TestEvent { Message = "Test3" });

        // Assert
        Assert.That(eventBus.Statistics!.TotalPublished, Is.EqualTo(3));
    }

    [Test]
    public void TotalHandled_ShouldTrackHandledEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);
        var handledCount = 0;

        eventBus.RegisterFilterable<TestEvent>(_ => handledCount++);
        eventBus.RegisterFilterable<TestEvent>(_ => handledCount++);

        // Act
        eventBus.SendFilterable(new TestEvent { Message = "Test" });

        // Assert
        Assert.That(eventBus.Statistics!.TotalHandled, Is.EqualTo(2));
        Assert.That(handledCount, Is.EqualTo(2));
    }

    [Test]
    public void TotalFailed_ShouldTrackFailedEvents()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        eventBus.RegisterFilterable<TestEvent>(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            eventBus.SendFilterable(new TestEvent { Message = "Test" }));

        Assert.That(eventBus.Statistics!.TotalFailed, Is.EqualTo(1));
    }

    [Test]
    public void GetPublishCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        // Act
        eventBus.SendFilterable(new TestEvent { Message = "Test1" });
        eventBus.SendFilterable(new TestEvent { Message = "Test2" });

        // Assert
        Assert.That(eventBus.Statistics!.GetPublishCount(nameof(TestEvent)), Is.EqualTo(2));
    }

    [Test]
    public void GetListenerCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        // Act
        eventBus.RegisterFilterable<TestEvent>(_ => { });
        eventBus.RegisterFilterable<TestEvent>(_ => { });

        // Assert
        Assert.That(eventBus.Statistics!.GetListenerCount(nameof(TestEvent)), Is.EqualTo(2));
    }

    [Test]
    public void Reset_ShouldClearAllStatistics()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        eventBus.RegisterFilterable<TestEvent>(_ => { });
        eventBus.SendFilterable(new TestEvent { Message = "Test" });

        // Act
        eventBus.Statistics!.Reset();

        // Assert
        Assert.That(eventBus.Statistics.TotalPublished, Is.EqualTo(0));
        Assert.That(eventBus.Statistics.TotalHandled, Is.EqualTo(0));
        Assert.That(eventBus.Statistics.TotalFailed, Is.EqualTo(0));
        Assert.That(eventBus.Statistics.GetPublishCount(nameof(TestEvent)), Is.EqualTo(0));
    }

    [Test]
    public void GenerateReport_ShouldReturnFormattedString()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        eventBus.RegisterFilterable<TestEvent>(_ => { });
        eventBus.SendFilterable(new TestEvent { Message = "Test" });

        // Act
        var report = eventBus.Statistics!.GenerateReport();

        // Assert
        Assert.That(report, Is.Not.Null);
        Assert.That(report, Does.Contain("事件统计报告"));
        Assert.That(report, Does.Contain("总发布数"));
        Assert.That(report, Does.Contain("总处理数"));
    }

    [Test]
    public void Statistics_ShouldBeThreadSafe()
    {
        // Arrange
        var eventBus = new EnhancedEventBus(enableStatistics: true);

        eventBus.RegisterFilterable<TestEvent>(_ => { });

        // Act - 并发发送事件
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    eventBus.SendFilterable(new TestEvent { Message = $"Test-{j}" });
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.That(eventBus.Statistics!.TotalPublished, Is.EqualTo(1000));
        Assert.That(eventBus.Statistics.TotalHandled, Is.EqualTo(1000));
    }

    private sealed class TestEvent
    {
        public string Message { get; init; } = string.Empty;
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Events;
using GFramework.Core.Extensions;
using GFramework.Core.Ioc;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     测试 ContextAwareEventExtensions 的单元测试类
///     验证事件发送、注册和取消注册功能
/// </summary>
[TestFixture]
public class ContextAwareEventExtensionsTests
{
    [SetUp]
    public void SetUp()
    {
        _container = new MicrosoftDiContainer();
        _eventBus = new EventBus();
        _container.Register<IEventBus>(_eventBus);
        _context = new ArchitectureContext(_container);
        _contextAware = new TestContextAware();

        ((IContextAware)_contextAware).SetContext(_context);
        _container.Freeze();
    }

    [TearDown]
    public void TearDown()
    {
        _container.Clear();
    }

    private TestContextAware _contextAware = null!;
    private ArchitectureContext _context = null!;
    private MicrosoftDiContainer _container = null!;
    private EventBus _eventBus = null!;

    [Test]
    public void SendEvent_Should_Trigger_Registered_Handler()
    {
        // Arrange
        var eventReceived = false;
        _contextAware.RegisterEvent<TestEvent>(_ => eventReceived = true);

        // Act
        _contextAware.SendEvent(new TestEvent());

        // Assert
        Assert.That(eventReceived, Is.True);
    }

    [Test]
    public void SendEvent_WithNew_Should_Create_And_Send_Event()
    {
        // Arrange
        var eventReceived = false;
        _contextAware.RegisterEvent<TestEventWithDefaultConstructor>(_ => eventReceived = true);

        // Act
        _contextAware.SendEvent<TestEventWithDefaultConstructor>();

        // Assert
        Assert.That(eventReceived, Is.True);
    }

    [Test]
    public void RegisterEvent_Should_Return_UnRegister_Interface()
    {
        // Act
        var unRegister = _contextAware.RegisterEvent<TestEvent>(_ => { });

        // Assert
        Assert.That(unRegister, Is.Not.Null);
        Assert.That(unRegister, Is.InstanceOf<IUnRegister>());
    }

    [Test]
    public void UnRegisterEvent_Should_Stop_Receiving_Events()
    {
        // Arrange
        var eventCount = 0;
        void Handler(TestEvent e) => eventCount++;

        _contextAware.RegisterEvent<TestEvent>(Handler);
        _contextAware.SendEvent(new TestEvent());

        // Act
        _contextAware.UnRegisterEvent<TestEvent>(Handler);
        _contextAware.SendEvent(new TestEvent());

        // Assert
        Assert.That(eventCount, Is.EqualTo(1));
    }

    [Test]
    public void SendEvent_Should_Pass_Event_Data()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        _contextAware.RegisterEvent<TestEvent>(e => receivedEvent = e);
        var sentEvent = new TestEvent { Data = "TestData" };

        // Act
        _contextAware.SendEvent(sentEvent);

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.Data, Is.EqualTo("TestData"));
    }

    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    private class TestEventWithDefaultConstructor
    {
    }

    private class TestContextAware : ContextAwareBase
    {
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Architectures;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     ArchitectureContext类的单元测试
///     测试内容包括：
///     - 构造函数参数验证（所有5个参数）
///     - 构造函数空参数异常
///     - SendEvent方法 - 正常事件发送
///     - SendEvent_WithInstance方法 - 正常事件发送
///     - SendEvent_WithInstance方法 - 空事件异常
///     - GetSystem方法 - 获取已注册系统
///     - GetSystem方法 - 获取未注册系统时抛出异常
///     - GetModel方法 - 获取已注册模型
///     - GetModel方法 - 获取未注册模型时抛出异常
///     - GetUtility方法 - 获取已注册工具
///     - GetUtility方法 - 获取未注册工具时抛出异常
///     - GetEnvironment方法 - 获取环境对象
/// </summary>
[NonParallelizable]
[TestFixture]
public class ArchitectureContextTests
{
    /// <summary>
    ///     初始化测试所需的容器与默认服务实例。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _container = new MicrosoftDiContainer();

        // 创建服务实例
        _eventBus = new EventBus();
        _environment = new DefaultEnvironment();

        // 将服务注册到容器
        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_environment);

        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    ///     释放当前测试创建的容器。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _container?.Dispose();
    }

    private MicrosoftDiContainer? _container;

    private ArchitectureContext? _context;
    private DefaultEnvironment? _environment;
    private EventBus? _eventBus;

    /// <summary>
    ///     测试构造函数在所有参数都有效时不应抛出异常
    /// </summary>
    [Test]
    public void Constructor_Should_NotThrow_When_AllParameters_AreValid()
    {
        Assert.That(() => new ArchitectureContext(_container!), Throws.Nothing);
    }

    /// <summary>
    ///     测试构造函数在 container 为 null 时应抛出 ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!), Throws.ArgumentNullException);
    }

    /// <summary>
    ///     测试构造函数在Container为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("container"));
    }

    /// <summary>
    ///     测试SendEvent方法在事件类型有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_Should_SendEvent_When_EventType_IsValid()
    {
        var eventReceived = false;
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     测试SendEvent方法（带实例）在事件实例有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_SendEvent_When_EventInstance_IsValid()
    {
        var eventReceived = false;
        var testEvent = new TestEventV2();
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent(testEvent);

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     测试SendEvent方法（带实例）在事件实例为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_ThrowArgumentNullException_When_EventInstance_IsNull()
    {
        Assert.That(() => _context!.SendEvent<TestEventV2>(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("e"));
    }

    /// <summary>
    ///     测试GetSystem方法在系统已注册时返回注册的系统
    /// </summary>
    [Test]
    public void GetSystem_Should_ReturnRegisteredSystem_When_SystemIsRegistered()
    {
        var testSystem = new TestSystemV2();
        _container!.RegisterPlurality(testSystem);

        var result = _context!.GetSystem<TestSystemV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testSystem));
    }

    /// <summary>
    ///     测试GetSystem方法在系统未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetSystem_Should_ThrowInvalidOperationException_When_SystemIsNotRegistered()
    {
        Assert.That(() => _context!.GetSystem<TestSystemV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetModel方法在模型已注册时返回注册的模型
    /// </summary>
    [Test]
    public void GetModel_Should_ReturnRegisteredModel_When_ModelIsRegistered()
    {
        var testModel = new TestModelV2();
        _container!.RegisterPlurality(testModel);

        var result = _context!.GetModel<TestModelV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testModel));
    }

    /// <summary>
    ///     测试GetModel方法在模型未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetModel_Should_ThrowInvalidOperationException_When_ModelIsNotRegistered()
    {
        Assert.That(() => _context!.GetModel<TestModelV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetUtility方法在工具已注册时返回注册的工具
    /// </summary>
    [Test]
    public void GetUtility_Should_ReturnRegisteredUtility_When_UtilityIsRegistered()
    {
        var testUtility = new TestUtilityV2();
        _container!.RegisterPlurality(testUtility);

        var result = _context!.GetUtility<TestUtilityV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testUtility));
    }

    /// <summary>
    ///     测试GetUtility方法在工具未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetUtility_Should_ThrowInvalidOperationException_When_UtilityIsNotRegistered()
    {
        Assert.That(() => _context!.GetUtility<TestUtilityV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetEnvironment方法返回环境实例
    /// </summary>
    [Test]
    public void GetEnvironment_Should_Return_EnvironmentInstance()
    {
        var environment = _context!.GetEnvironment();

        Assert.That(environment, Is.Not.Null);
        Assert.That(environment, Is.InstanceOf<IEnvironment>());
    }
}

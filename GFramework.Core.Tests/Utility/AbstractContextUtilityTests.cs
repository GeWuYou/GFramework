// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Query;
using GFramework.Core.Utility;

namespace GFramework.Core.Tests.Utility;

/// <summary>
///     AbstractContextUtility类的单元测试
///     测试内容包括：
///     - 抽象工具类实现
///     - IContextUtility接口实现
///     - Init方法调用
///     - 日志初始化
///     - 上下文感知功能（SetContext, GetContext）
///     - 子类继承行为
///     - 工具初始化日志记录
///     - 工具生命周期完整性
/// </summary>
[TestFixture]
public class AbstractContextUtilityTests
{
    [SetUp]
    public void SetUp()
    {
        _container = new MicrosoftDiContainer();
        _container.RegisterPlurality(new EventBus());
        _container.RegisterPlurality(new CommandExecutor());
        _container.RegisterPlurality(new QueryExecutor());
        _container.RegisterPlurality(new DefaultEnvironment());
        _container.RegisterPlurality(new AsyncQueryExecutor());
        _context = new ArchitectureContext(_container);
    }

    private ArchitectureContext _context = null!;
    private MicrosoftDiContainer _container = null!;

    /// <summary>
    ///     测试AbstractContextUtility实现IContextUtility接口
    /// </summary>
    [Test]
    public void AbstractContextUtility_Should_Implement_IContextUtility_Interface()
    {
        var utility = new TestContextUtilityV1();

        Assert.That(utility, Is.InstanceOf<IContextUtility>());
    }

    /// <summary>
    ///     测试Init方法调用
    /// </summary>
    [Test]
    public void Init_Should_Call_OnInit_Method()
    {
        var utility = new TestContextUtilityV1();

        Assert.That(utility.Initialized, Is.False, "Utility should not be initialized before OnInitialize");

        utility.Initialize();

        Assert.That(utility.Initialized, Is.True, "Utility should be initialized after OnInitialize");
    }

    /// <summary>
    ///     测试Init方法设置Logger属性
    /// </summary>
    [Test]
    public void Init_Should_Set_Logger_Property()
    {
        var utility = new TestContextUtilityV1();

        Assert.That(utility.GetLogger(), Is.Null, "Logger should be null before OnInitialize");

        utility.Initialize();

        Assert.That(utility.GetLogger(), Is.Not.Null, "Logger should be set after OnInitialize");
    }

    /// <summary>
    ///     测试Init方法记录初始化日志
    /// </summary>
    [Test]
    public void Init_Should_Log_Initialization()
    {
        var utility = new TestContextUtilityV1();

        Assert.That(utility.InitCalled, Is.False, "InitCalled should be false before OnInitialize");

        utility.Initialize();

        Assert.That(utility.InitCalled, Is.True, "InitCalled should be true after OnInitialize");
    }

    /// <summary>
    ///     测试Destroy方法调用
    /// </summary>
    [Test]
    public void Destroy_Should_Call_OnDestroy_Method()
    {
        var utility = new TestContextUtilityV1();

        utility.Initialize();
        Assert.That(utility.Destroyed, Is.False, "Utility should not be destroyed before Destroy");

        utility.Destroy();

        Assert.That(utility.Destroyed, Is.True, "Utility should be destroyed after Destroy");
    }

    /// <summary>
    ///     测试上下文感知功能 - SetContext方法
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        var utility = new TestContextUtilityV1();
        var contextAware = (IContextAware)utility;

        contextAware.SetContext(_context);

        var context = contextAware.GetContext();
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试上下文感知功能 - GetContext方法
    /// </summary>
    [Test]
    public void GetContext_Should_Return_Context_Property()
    {
        var utility = new TestContextUtilityV1();
        var contextAware = (IContextAware)utility;

        contextAware.SetContext(_context);

        var context = contextAware.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试子类继承行为
    /// </summary>
    [Test]
    public void Child_Class_Should_Override_OnInit_Method()
    {
        var utility = new TestContextUtilityV2();

        Assert.That(utility.Initialized, Is.False);
        Assert.That(utility.CustomInitializationDone, Is.False);

        utility.Initialize();

        Assert.That(utility.Initialized, Is.True);
        Assert.That(utility.CustomInitializationDone, Is.True);
    }

    /// <summary>
    ///     测试工具生命周期完整性
    /// </summary>
    [Test]
    public void ContextUtility_Should_Complete_Full_Lifecycle()
    {
        var utility = new TestContextUtilityV1();

        // 初始状态
        Assert.That(utility.Initialized, Is.False);
        Assert.That(utility.Destroyed, Is.False);

        // 初始化
        utility.Initialize();
        Assert.That(utility.Initialized, Is.True);
        Assert.That(utility.Destroyed, Is.False);

        // 销毁
        utility.Destroy();
        Assert.That(utility.Initialized, Is.True);
        Assert.That(utility.Destroyed, Is.True);
    }

    /// <summary>
    ///     测试工具类可以多次初始化和销毁
    /// </summary>
    [Test]
    public void ContextUtility_Should_Be_Initializable_And_Destroyable_Multiple_Times()
    {
        var utility = new TestContextUtilityV1();

        // 第一次初始化和销毁
        utility.Initialize();
        Assert.That(utility.Initialized, Is.True);
        utility.Destroy();
        Assert.That(utility.Destroyed, Is.True);

        // 重置状态
        utility.ResetDestroyedStateForTest();

        // 第二次初始化和销毁
        utility.Initialize();
        Assert.That(utility.Initialized, Is.True);
        utility.Destroy();
        Assert.That(utility.Destroyed, Is.True);
    }
}

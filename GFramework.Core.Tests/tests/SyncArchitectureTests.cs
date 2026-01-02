using System.Reflection;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace GFramework.Core.Tests.tests;

/// <summary>
/// 同步架构测试类，用于测试同步架构的初始化、生命周期和组件注册等功能
/// </summary>
[TestFixture]
[NonParallelizable]
public class SyncArchitectureTests
{
    /// <summary>
    /// 测试初始化方法，清理游戏上下文并创建同步测试架构实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
        _architecture = new SyncTestArchitecture();
    }

    /// <summary>
    /// 测试清理方法，销毁架构实例并清理游戏上下文
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        try
        {
            _architecture?.Destroy();
        }
        finally
        {
            GameContext.Clear();
            _architecture = null;
        }
    }

    private SyncTestArchitecture? _architecture;

    /// <summary>
    /// 测试架构是否正确初始化所有组件
    /// 验证初始化调用、运行时状态、架构阶段和模型系统注册
    /// </summary>
    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Act
        _architecture!.Initialize();

        // Assert
        Assert.That(_architecture.InitCalled, Is.True);

        Assert.That(_architecture.Runtime, Is.Not.Null);

        // 通过反射获取当前架构阶段
        var phaseProperty = typeof(Architecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(_architecture)!;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Ready));

        var context = _architecture.Context;

        var model = context.GetModel<TestModel>();
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Inited, Is.True);

        var system = context.GetSystem<TestSystem>();
        Assert.That(system, Is.Not.Null);
        Assert.That(system!.Initialized, Is.True);
    }

    /// <summary>
    /// 测试架构是否按类型正确注册上下文
    /// </summary>
    [Test]
    public void Architecture_Should_Register_Context_By_Type()
    {
        // Act
        _architecture!.Initialize();
        var ctx = GameContext.GetByType(_architecture!.GetType());

        Assert.That(ctx, Is.Not.Null);
    }

    /// <summary>
    /// 测试架构是否按正确顺序进入各个阶段
    /// 验证架构初始化过程中各阶段的执行顺序
    /// </summary>
    [Test]
    public void Architecture_Should_Enter_Phases_In_Correct_Order()
    {
        _architecture!.Initialize();

        var phases = _architecture.PhaseHistory;

        CollectionAssert.AreEqual(
            new[]
            {
                ArchitecturePhase.BeforeModelInit,
                ArchitecturePhase.AfterModelInit,
                ArchitecturePhase.BeforeSystemInit,
                ArchitecturePhase.AfterSystemInit,
                ArchitecturePhase.Ready
            },
            phases
        );
    }

    /// <summary>
    /// 测试在架构就绪后注册系统是否抛出异常（当不允许时）
    /// </summary>
    [Test]
    public void RegisterSystem_AfterReady_Should_Throw_When_NotAllowed()
    {
        _architecture!.Initialize();

        Assert.Throws<InvalidOperationException>(() => { _architecture.RegisterSystem(new TestSystem()); });
    }

    /// <summary>
    /// 测试在架构就绪后注册模型是否抛出异常（当不允许时）
    /// </summary>
    [Test]
    public void RegisterModel_AfterReady_Should_Throw_When_NotAllowed()
    {
        _architecture!.Initialize();

        Assert.Throws<InvalidOperationException>(() => { _architecture.RegisterModel(new TestModel()); });
    }

    /// <summary>
    /// 测试架构销毁功能，验证销毁后系统被正确销毁且架构进入销毁阶段
    /// </summary>
    [Test]
    public void Architecture_Destroy_Should_Destroy_All_Systems_And_Enter_Destroyed()
    {
        _architecture!.Initialize();
        _architecture.Destroy();

        var system = _architecture.Context.GetSystem<TestSystem>();
        Assert.That(system!.DestroyCalled, Is.True);

        // 通过反射获取当前架构阶段
        var phaseProperty = typeof(Architecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(_architecture)!;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }

    /// <summary>
    /// 测试当模型初始化失败时架构是否停止初始化
    /// </summary>
    [Test]
    public void Architecture_Should_Stop_Initialization_When_Model_Init_Fails()
    {
        _architecture!.RegisterModel(new FailingModel());

        Assert.Throws<InvalidOperationException>(() => { _architecture!.Initialize(); });
    }
}
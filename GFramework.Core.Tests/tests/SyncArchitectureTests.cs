using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.events;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace GFramework.Core.Tests.tests;

/// <summary>
///     同步架构测试类，用于测试同步架构的初始化、生命周期和组件注册等功能
/// </summary>
[TestFixture]
[NonParallelizable]
public class SyncArchitectureTests : ArchitectureTestsBase<SyncTestArchitecture>
{
    protected override SyncTestArchitecture CreateArchitecture()
    {
        return new SyncTestArchitecture();
    }

    /// <summary>
    ///     测试架构是否正确初始化所有组件
    ///     验证初始化调用、运行时状态、架构阶段和模型系统注册
    /// </summary>
    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Act
        Architecture!.Initialize();

        // Assert
        Assert.That(Architecture.InitCalled, Is.True);


        var phase = Architecture.CurrentPhase;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Ready));

        var context = Architecture.Context;

        var model = context.GetModel<TestModel>();
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Initialized, Is.True);

        var system = context.GetSystem<TestSystem>();
        Assert.That(system, Is.Not.Null);
        Assert.That(system!.Initialized, Is.True);
    }

    /// <summary>
    ///     测试架构是否按类型正确注册上下文
    /// </summary>
    [Test]
    public void Architecture_Should_Register_Context_By_Type()
    {
        // Act
        Architecture!.Initialize();
        var ctx = GameContext.GetByType(Architecture!.GetType());

        Assert.That(ctx, Is.Not.Null);
    }

    /// <summary>
    ///     测试架构是否按正确顺序进入各个阶段
    ///     验证架构初始化过程中各阶段的执行顺序
    /// </summary>
    [Test]
    public void Architecture_Should_Enter_Phases_In_Correct_Order()
    {
        Architecture!.Initialize();

        var phases = Architecture.PhaseHistory;

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
    ///     测试在架构就绪后注册系统是否抛出异常（当不允许时）
    /// </summary>
    [Test]
    public void RegisterSystem_AfterReady_Should_Throw_When_NotAllowed()
    {
        Architecture!.Initialize();

        Assert.Throws<InvalidOperationException>(() => { Architecture.RegisterSystem(new TestSystem()); });
    }

    /// <summary>
    ///     测试在架构就绪后注册模型是否抛出异常（当不允许时）
    /// </summary>
    [Test]
    public void RegisterModel_AfterReady_Should_Throw_When_NotAllowed()
    {
        Architecture!.Initialize();

        Assert.Throws<InvalidOperationException>(() => { Architecture.RegisterModel(new TestModel()); });
    }

    /// <summary>
    ///     测试架构销毁功能，验证销毁后系统被正确销毁且架构进入销毁阶段
    /// </summary>
    [Test]
    public void Architecture_Destroy_Should_Destroy_All_Systems_And_Enter_Destroyed()
    {
        Architecture!.Initialize();
        Architecture.Destroy();

        var system = Architecture.Context.GetSystem<TestSystem>();
        Assert.That(system!.DestroyCalled, Is.True);

        var phase = Architecture.CurrentPhase;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }

    /// <summary>
    ///     测试当模型初始化失败时架构是否停止初始化
    /// </summary>
    [Test]
    public void Architecture_Should_Stop_Initialization_When_Model_Init_Fails()
    {
        Architecture!.AddPostRegistrationHook(a =>
            a.RegisterModel(new FailingModel())
        );

        Architecture.Initialize();

        AssertInitializationFailed();
    }

    /// <summary>
    /// 测试事件是否能够被正确接收和处理
    /// </summary>
    /// <remarks>
    /// 该测试验证了事件系统的注册和发送功能，确保事件能够被正确传递给注册的处理器
    /// </remarks>
    [Test]
    public void Event_Should_Be_Received()
    {
        Architecture!.Initialize();
        var context = Architecture.Context;

        var receivedValue = 0;
        const int tagetValue = 100;
        // 注册事件处理器，将接收到的值赋给receivedValue变量
        context.RegisterEvent<TestEvent>(e => { receivedValue = e.ReceivedValue; });

        // 发送测试事件
        context.SendEvent(new TestEvent
        {
            ReceivedValue = tagetValue
        });

        Assert.That(receivedValue, Is.EqualTo(tagetValue));
    }

    /// <summary>
    /// 测试事件取消注册功能是否正常工作
    /// </summary>
    /// <remarks>
    /// 该测试验证了事件处理器的取消注册功能，确保取消注册后事件处理器不再被调用
    /// </remarks>
    [Test]
    public void Event_UnRegister_Should_Work()
    {
        Architecture!.Initialize();
        var context = Architecture.Context;

        var count = 0;

        // 注册事件处理器并获取取消注册对象
        var unRegister = context.RegisterEvent<EmptyEvent>(Handler);

        // 发送第一个事件，此时处理器应该被调用
        context.SendEvent(new EmptyEvent());

        // 验证事件处理器被调用了一次
        Assert.That(count, Is.EqualTo(1), "Handler should be called once before unregistration");

        // 取消注册事件处理器
        unRegister.UnRegister();

        // 发送第二个事件，此时处理器不应该被调用
        context.SendEvent(new EmptyEvent());

        // 验证取消注册后，计数没有增加
        Assert.That(count, Is.EqualTo(1), "Handler should not be called after unregistration");

        void Handler(EmptyEvent _)
        {
            count++;
        }
    }
}
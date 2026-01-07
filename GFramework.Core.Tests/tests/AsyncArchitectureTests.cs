using GFramework.Core.Abstractions.enums;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace GFramework.Core.Tests.tests;

/// <summary>
///     异步架构测试类，用于测试异步架构的相关功能
/// </summary>
/// <remarks>
///     该测试类使用非并行执行模式，确保测试的隔离性
/// </remarks>
[TestFixture]
[NonParallelizable]
public class AsyncArchitectureTests : ArchitectureTestsBase<AsyncTestArchitecture>
{
    /// <summary>
    ///     创建异步测试架构实例
    /// </summary>
    /// <returns>AsyncTestArchitecture实例</returns>
    protected override AsyncTestArchitecture CreateArchitecture()
    {
        return new AsyncTestArchitecture();
    }

    /// <summary>
    ///     测试架构是否正确初始化所有组件
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task Architecture_Should_Initialize_All_Components_Correctly()
    {
        await Architecture!.InitializeAsync();

        Assert.That(Architecture.InitCalled, Is.True);
        Assert.That(Architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));

        var context = Architecture.Context;

        var model = context.GetModel<AsyncTestModel>();
        Assert.That(model!.Initialized, Is.True);

        var system = context.GetSystem<AsyncTestSystem>();
        Assert.That(system!.Initialized, Is.True);
    }

    /// <summary>
    ///     测试架构是否按正确顺序进入各个阶段
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task Architecture_Should_Enter_Phases_In_Correct_Order()
    {
        await Architecture!.InitializeAsync();

        // 验证架构阶段历史记录是否符合预期顺序
        CollectionAssert.AreEqual(
            new[]
            {
                ArchitecturePhase.BeforeUtilityInit,
                ArchitecturePhase.AfterUtilityInit,
                ArchitecturePhase.BeforeModelInit,
                ArchitecturePhase.AfterModelInit,
                ArchitecturePhase.BeforeSystemInit,
                ArchitecturePhase.AfterSystemInit,
                ArchitecturePhase.Ready
            },
            Architecture.PhaseHistory
        );
    }

    /// <summary>
    ///     测试在就绪状态后注册模型是否抛出异常
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task RegisterModel_AfterReady_Should_Throw()
    {
        await Architecture!.InitializeAsync();

        Assert.Throws<InvalidOperationException>(() => Architecture.RegisterModel(new TestModel())
        );
    }

    /// <summary>
    ///     测试当模型初始化失败时架构是否停止初始化
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task Architecture_Should_Stop_Initialization_When_Model_Init_Fails()
    {
        Architecture!.AddPostRegistrationHook(a => { a.RegisterModel(new FailingModel()); });

        await Architecture.InitializeAsync();

        Assert.That(
            Architecture.CurrentPhase,
            Is.EqualTo(ArchitecturePhase.FailedInitialization)
        );
    }

    /// <summary>
    ///     测试架构销毁是否正确销毁所有系统
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task Architecture_Destroy_Should_Destroy_All_Systems()
    {
        await Architecture!.InitializeAsync();
        Architecture.Destroy();

        var system = Architecture.Context.GetSystem<TestSystem>();
        Assert.That(system, Is.Null);
        Assert.That(Architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }

    /// <summary>
    ///     测试InitializeAsync方法是否不会阻塞
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task InitializeAsync_Should_Not_Block()
    {
        var task = Architecture!.InitializeAsync();

        Assert.That(task.IsCompleted, Is.False);
        await task;
    }

    /// <summary>
    ///     测试InitializeAsync方法是否正确处理异常
    /// </summary>
    /// <returns>异步任务</returns>
    [Test]
    public async Task InitializeAsync_Should_Handle_Exception_Correctly()
    {
        Architecture!.AddPostRegistrationHook(a =>
            a.RegisterModel(new FailingModel())
        );

        await Architecture.InitializeAsync();

        AssertInitializationFailed();
    }
}
using System.Reflection;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;

namespace GFramework.Core.Tests.tests;

/// <summary>
/// 同步架构测试类，用于测试同步架构的功能和行为
/// </summary>
/// <remarks>
/// 该测试类使用非并行执行模式，确保测试的隔离性和可靠性
/// </remarks>
[TestFixture]
[NonParallelizable]
public class SyncArchitectureTests : ArchitectureTestsBase<SyncTestArchitecture>
{
    /// <summary>
    /// 创建同步测试架构实例
    /// </summary>
    /// <returns>SyncTestArchitecture实例</returns>
    protected override SyncTestArchitecture CreateArchitecture() => new SyncTestArchitecture();

    /// <summary>
    /// 初始化架构异步方法
    /// </summary>
    /// <returns>表示异步操作的Task</returns>
    protected override Task InitializeArchitecture()
    {
        Architecture!.Initialize();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 测试架构是否正确初始化所有组件
    /// 验证初始化调用、运行时状态、架构阶段和模型系统注册
    /// </summary>
    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Act
        Architecture!.Initialize();

        // Assert
        Assert.That(Architecture.InitCalled, Is.True);

        Assert.That(Architecture.Runtime, Is.Not.Null);

        // 通过反射获取当前架构阶段
        var phaseProperty = typeof(Architecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(Architecture)!;
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
    /// 测试架构是否按类型正确注册上下文
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
    /// 测试当模型初始化失败时架构是否停止初始化
    /// </summary>
    [Test]
    public void Architecture_Should_Stop_Initialization_When_Model_Init_Fails()
    {
        Architecture!.RegisterModel(new FailingModel());

        Assert.Throws<InvalidOperationException>(() => { Architecture!.Initialize(); });
    }
}
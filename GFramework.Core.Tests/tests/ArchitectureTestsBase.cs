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
/// 架构测试基类，封装同步/异步共通测试逻辑
/// </summary>
/// <typeparam name="TArchitecture">架构类型，必须继承自Architecture</typeparam>
public abstract class ArchitectureTestsBase<TArchitecture> where TArchitecture : Architecture
{
    protected TArchitecture? Architecture;

    /// <summary>
    /// 子类必须实现创建具体架构实例
    /// </summary>
    /// <returns>创建的架构实例</returns>
    protected abstract TArchitecture CreateArchitecture();

    /// <summary>
    /// 子类必须实现初始化架构（同步或异步）
    /// </summary>
    /// <returns>异步初始化任务</returns>
    protected abstract Task InitializeArchitecture();

    /// <summary>
    /// 测试设置方法，在每个测试开始前执行
    /// 清理游戏上下文并创建架构实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
        Architecture = CreateArchitecture();
    }

    /// <summary>
    /// 测试清理方法，在每个测试结束后执行
    /// 销毁架构实例并清理游戏上下文
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        try
        {
            Architecture?.Destroy();
        }
        finally
        {
            GameContext.Clear();
            Architecture = null;
        }
    }

    /// <summary>
    /// 验证架构阶段顺序
    /// </summary>
    /// <returns>异步测试任务</returns>
    [Test]
    public async Task Architecture_Should_Enter_Phases_In_Correct_Order()
    {
        await InitializeArchitecture();

        // 通过反射获取架构的阶段历史记录
        var phasesProperty = typeof(TArchitecture)
            .GetProperty("PhaseHistory", BindingFlags.Instance | BindingFlags.Public);

        var phases = (List<ArchitecturePhase>)phasesProperty!.GetValue(Architecture)!;

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
    /// 验证 Ready 后不能注册组件
    /// </summary>
    /// <returns>异步测试任务</returns>
    [Test]
    public async Task Registering_Components_AfterReady_Should_Throw()
    {
        await InitializeArchitecture();

        // 根据架构类型验证注册组件时抛出异常
        if (Architecture is SyncTestArchitecture syncArch)
        {
            Assert.Throws<InvalidOperationException>(() => syncArch.RegisterModel(new TestModel()));
            Assert.Throws<InvalidOperationException>(() => syncArch.RegisterSystem(new TestSystem()));
        }
        else if (Architecture is AsyncTestArchitecture asyncArch)
        {
            Assert.Throws<InvalidOperationException>(() => asyncArch.RegisterModel(new AsyncTestModel()));
            Assert.Throws<InvalidOperationException>(() => asyncArch.RegisterSystem(new AsyncTestSystem()));
        }
    }

    /// <summary>
    /// 验证销毁功能
    /// </summary>
    /// <returns>异步测试任务</returns>
    [Test]
    public async Task Architecture_Destroy_Should_Destroy_All_Systems_And_Enter_Destroyed()
    {
        await InitializeArchitecture();

        Architecture!.Destroy();

        // 验证系统是否被正确销毁
        if (Architecture is SyncTestArchitecture syncArch)
        {
            var system = syncArch.Context.GetSystem<TestSystem>();
            Assert.That(system!.DestroyCalled, Is.True);
        }
        else if (Architecture is AsyncTestArchitecture asyncArch)
        {
            var system = asyncArch.Context.GetSystem<AsyncTestSystem>();
            Assert.That(system!.DestroyCalled, Is.True);
        }

        // 通过反射验证当前阶段为销毁状态
        var phaseProperty = typeof(TArchitecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(Architecture)!;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }
}
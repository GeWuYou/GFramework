using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using NUnit.Framework;

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
    /// 验证架构初始化失败的断言方法
    /// 检查当前架构阶段是否为初始化失败状态
    /// </summary>
    protected void AssertInitializationFailed()
    {
        Assert.That(
            Architecture!.CurrentPhase,
            Is.EqualTo(ArchitecturePhase.FailedInitialization)
        );
    }
}
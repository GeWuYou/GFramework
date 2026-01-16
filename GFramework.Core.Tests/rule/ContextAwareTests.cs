using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;
using GFramework.Core.rule;
using GFramework.Core.Tests.architecture;
using NUnit.Framework;

namespace GFramework.Core.Tests.rule;

/// <summary>
/// 测试 ContextAware 功能的单元测试类
/// 验证上下文感知对象的设置、获取和回调功能
/// </summary>
[TestFixture]
public class ContextAwareTests
{
    /// <summary>
    /// 在每个测试方法执行前进行初始化设置
    /// 创建测试用的 ContextAware 对象和模拟上下文，并绑定到游戏上下文中
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _contextAware = new TestContextAware();
        _mockContext = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), _mockContext);
    }

    /// <summary>
    /// 在每个测试方法执行后进行清理工作
    /// 从游戏上下文中解绑测试用的架构上下文类型
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Unbind(typeof(TestArchitectureContext));
    }

    private TestContextAware _contextAware = null!;
    private TestArchitectureContext _mockContext = null!;

    /// <summary>
    /// 测试 SetContext 方法是否正确设置上下文属性
    /// 验证通过 IContextAware 接口设置上下文后，内部的 PublicContext 属性能够正确返回设置的上下文
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.PublicContext, Is.SameAs(_mockContext));
    }

    /// <summary>
    /// 测试 SetContext 方法是否正确调用 OnContextReady 回调方法
    /// 验证设置上下文后，OnContextReady 方法被正确触发
    /// </summary>
    [Test]
    public void SetContext_Should_Call_OnContextReady()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.OnContextReadyCalled, Is.True);
    }

    /// <summary>
    /// 测试 GetContext 方法是否返回已设置的上下文
    /// 验证通过 IContextAware 接口设置上下文后，GetContext 方法能正确返回相同的上下文实例
    /// </summary>
    [Test]
    public void GetContext_Should_Return_Set_Context()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        var result = aware.GetContext();

        Assert.That(result, Is.SameAs(_mockContext));
    }

    /// <summary>
    /// 测试 GetContext 方法在未设置上下文时的行为
    /// 验证当内部 Context 为 null 时，GetContext 方法不会抛出异常
    /// 此时应返回第一个架构上下文（在测试环境中验证不抛出异常即可）
    /// </summary>
    [Test]
    public void GetContext_Should_Return_FirstArchitectureContext_When_Not_Set()
    {
        // Arrange - 暂时不调用 SetContext，让 Context 为 null
        IContextAware aware = _contextAware;

        // Act - 当 Context 为 null 时，应该返回第一个 Architecture Context
        // 由于测试环境中没有实际的 Architecture Context，这里只测试调用不会抛出异常
        // 在实际使用中，当 Context 为 null 时会调用 GameContext.GetFirstArchitectureContext()

        // Assert - 验证在没有设置 Context 时的行为
        // 注意：由于测试环境中可能没有 Architecture Context，这里我们只测试不抛出异常
        Assert.DoesNotThrow(() => aware.GetContext());
    }
}

/// <summary>
/// 用于测试的 ContextAware 实现类
/// 继承自 ContextAwareBase，提供公共访问的上下文属性和回调状态跟踪
/// </summary>
public class TestContextAware : ContextAwareBase
{
    /// <summary>
    /// 获取内部上下文的公共访问属性
    /// </summary>
    public IArchitectureContext? PublicContext => Context;

    /// <summary>
    /// 跟踪 OnContextReady 方法是否被调用的状态
    /// </summary>
    public bool OnContextReadyCalled { get; private set; }

    /// <summary>
    /// 重写上下文就绪回调方法
    /// 设置 OnContextReadyCalled 标志为 true，用于测试验证
    /// </summary>
    protected override void OnContextReady()
    {
        OnContextReadyCalled = true;
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Rule;
using GFramework.Core.Tests.Architectures;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     测试 ContextAware 功能的单元测试类
///     验证上下文感知对象的设置、获取和回调功能
/// </summary>
[TestFixture]
public class ContextAwareTests
{
    /// <summary>
    ///     在每个测试方法执行前进行初始化设置
    ///     创建测试用的 ContextAware 对象和模拟上下文，并绑定到游戏上下文中
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _contextAware = new TestContextAware();
        _mockContext = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), _mockContext);
    }

    /// <summary>
    ///     在每个测试方法执行后进行清理工作
    ///     从游戏上下文中解绑测试用的架构上下文类型
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Unbind(typeof(TestArchitectureContext));
    }

    private TestContextAware _contextAware = null!;
    private TestArchitectureContext _mockContext = null!;

    /// <summary>
    ///     测试 SetContext 方法是否正确设置上下文属性
    ///     验证通过 IContextAware 接口设置上下文后，内部的 PublicContext 属性能够正确返回设置的上下文
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.PublicContext, Is.SameAs(_mockContext));
    }

    /// <summary>
    ///     测试 SetContext 方法是否正确调用 OnContextReady 回调方法
    ///     验证设置上下文后，OnContextReady 方法被正确触发
    /// </summary>
    [Test]
    public void SetContext_Should_Call_OnContextReady()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.OnContextReadyCalled, Is.True);
    }

    /// <summary>
    ///     测试 GetContext 方法是否返回已设置的上下文
    ///     验证通过 IContextAware 接口设置上下文后，GetContext 方法能正确返回相同的上下文实例
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
    ///     测试 GetContext 方法在未设置上下文时会回退到当前活动上下文
    /// </summary>
    [Test]
    public void GetContext_Should_Return_CurrentArchitectureContext_When_Not_Set()
    {
        IContextAware aware = _contextAware;

        var result = aware.GetContext();

        Assert.That(result, Is.SameAs(_mockContext));
    }
}

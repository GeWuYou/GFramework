// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
/// ContextProvider 相关类的单元测试
/// 测试内容包括：
/// - GameContextProvider 获取第一个架构上下文
/// - GameContextProvider 尝试获取指定类型的上下文
/// - ScopedContextProvider 获取绑定的上下文
/// - ScopedContextProvider 尝试获取指定类型的上下文
/// - ScopedContextProvider 类型不匹配时返回 false
/// </summary>
[TestFixture]
public class ContextProviderTests
{
    /// <summary>
    /// 测试初始化方法，在每个测试方法执行前清空 GameContext
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
    }

    /// <summary>
    /// 测试清理方法，在每个测试方法执行后清空 GameContext
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    /// 测试 GameContextProvider 是否能正确获取第一个架构上下文
    /// </summary>
    [Test]
    public void GameContextProvider_GetContext_Should_Return_First_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var provider = new GameContextProvider();
        var result = provider.GetContext();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    /// 测试 GameContextProvider 在没有上下文时是否抛出异常
    /// </summary>
    [Test]
    public void GameContextProvider_GetContext_Should_Throw_When_Empty()
    {
        var provider = new GameContextProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetContext());
    }

    /// <summary>
    /// 测试 GameContextProvider 的 TryGetContext 方法在找到上下文时返回 true
    /// </summary>
    [Test]
    public void GameContextProvider_TryGetContext_Should_Return_True_When_Found()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var provider = new GameContextProvider();
        var result = provider.TryGetContext<TestArchitectureContext>(out var foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    /// <summary>
    /// 测试 GameContextProvider 的 TryGetContext 方法在未找到上下文时返回 false
    /// </summary>
    [Test]
    public void GameContextProvider_TryGetContext_Should_Return_False_When_Not_Found()
    {
        var provider = new GameContextProvider();
        var result = provider.TryGetContext<TestArchitectureContext>(out var foundContext);

        Assert.That(result, Is.False);
        Assert.That(foundContext, Is.Null);
    }

    /// <summary>
    /// 测试 ScopedContextProvider 是否能正确返回绑定的上下文
    /// </summary>
    [Test]
    public void ScopedContextProvider_GetContext_Should_Return_Bound_Context()
    {
        var context = new TestArchitectureContext();
        var provider = new ScopedContextProvider(context);

        var result = provider.GetContext();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    /// 测试 ScopedContextProvider 的 TryGetContext 方法在类型匹配时返回 true
    /// </summary>
    [Test]
    public void ScopedContextProvider_TryGetContext_Should_Return_True_When_Type_Matches()
    {
        var context = new TestArchitectureContext();
        var provider = new ScopedContextProvider(context);

        var result = provider.TryGetContext<TestArchitectureContext>(out var foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    /// <summary>
    /// 测试 ScopedContextProvider 的 TryGetContext 方法在类型不匹配时返回 false
    /// </summary>
    [Test]
    public void ScopedContextProvider_TryGetContext_Should_Return_False_When_Type_Does_Not_Match()
    {
        var context = new TestArchitectureContext();
        var provider = new ScopedContextProvider(context);

        var result = provider.TryGetContext<AnotherTestArchitectureContext>(out var foundContext);

        Assert.That(result, Is.False);
        Assert.That(foundContext, Is.Null);
    }

    /// <summary>
    /// 测试 ScopedContextProvider 的 TryGetContext 方法支持接口类型查询
    /// </summary>
    [Test]
    public void ScopedContextProvider_TryGetContext_Should_Support_Interface_Type()
    {
        var context = new TestArchitectureContext();
        var provider = new ScopedContextProvider(context);

        var result = provider.TryGetContext<IArchitectureContext>(out var foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }
}

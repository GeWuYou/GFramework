// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Tests.Architectures;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     验证真实编译生成的 ContextAware 对象需要显式绑定并按实例隔离上下文。
/// </summary>
[TestFixture]
[NonParallelizable]
public class GeneratedContextAwareTests
{
    /// <summary>
    ///     即使存在全局上下文，新生成对象也必须经过显式绑定。
    /// </summary>
    [Test]
    public void GetContext_Unbound_Should_Not_Read_Global_Context()
    {
        var globalContext = new TestArchitectureContext();
        GameContext.Bind(typeof(GeneratedContextAwareProbe), globalContext);

        try
        {
            IContextAware probe = new GeneratedContextAwareProbe();

            Assert.That(() => probe.GetContext(),
                Throws.InvalidOperationException.With.Message.Contains("IContextAware.SetContext"));
        }
        finally
        {
            GameContext.Unbind(typeof(GeneratedContextAwareProbe));
        }
    }

    /// <summary>
    ///     同一生成类型的实例可以属于不同架构，绑定不会传播到其他实例。
    /// </summary>
    [Test]
    public void SetContext_Should_Bind_Only_Current_Instance()
    {
        IContextAware first = new GeneratedContextAwareProbe();
        IContextAware second = new GeneratedContextAwareProbe();
        var firstContext = new TestArchitectureContext();
        var secondContext = new TestArchitectureContext();

        first.SetContext(firstContext);
        Assert.That(() => second.GetContext(), Throws.InvalidOperationException);
        second.SetContext(secondContext);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetContext(), Is.SameAs(firstContext));
            Assert.That(second.GetContext(), Is.SameAs(secondContext));
        });
    }

    /// <summary>
    ///     显式重新绑定只替换目标实例，并允许消费者迁移到新的有效上下文。
    /// </summary>
    [Test]
    public void SetContext_Should_Replace_Current_Instance_Binding()
    {
        IContextAware probe = new GeneratedContextAwareProbe();
        var firstContext = new TestArchitectureContext();
        var replacement = new TestArchitectureContext();
        probe.SetContext(firstContext);

        probe.SetContext(replacement);

        Assert.That(probe.GetContext(), Is.SameAs(replacement));
    }

    /// <summary>
    ///     拒绝 null 绑定，同时保留当前有效的实例绑定。
    /// </summary>
    [Test]
    public void SetContext_Null_Should_Preserve_Existing_Binding()
    {
        IContextAware probe = new GeneratedContextAwareProbe();
        var context = new TestArchitectureContext();
        probe.SetContext(context);

        Assert.Throws<ArgumentNullException>(() => probe.SetContext(null!));
        Assert.That(probe.GetContext(), Is.SameAs(context));
    }

    /// <summary>
    ///     并发读取与显式绑定使用各实例自己的同步协议，不会互相污染。
    /// </summary>
    [Test]
    public void Concurrent_Binding_And_Read_Should_Keep_Instances_Isolated()
    {
        IContextAware first = new GeneratedContextAwareProbe();
        IContextAware second = new GeneratedContextAwareProbe();
        var firstContext = new TestArchitectureContext();
        var secondContext = new TestArchitectureContext();
        first.SetContext(firstContext);
        second.SetContext(secondContext);

        Parallel.Invoke(
            () => Parallel.For(0, 100, _ => first.SetContext(firstContext)),
            () => Parallel.For(0, 100, _ => second.SetContext(secondContext)),
            () => Parallel.For(0, 100, _ => Assert.That(first.GetContext(), Is.SameAs(firstContext))),
            () => Parallel.For(0, 100, _ => Assert.That(second.GetContext(), Is.SameAs(secondContext))));
    }
}

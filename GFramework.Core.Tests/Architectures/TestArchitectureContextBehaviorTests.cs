// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     覆盖测试架构上下文替身的共享事件契约。
/// </summary>
[TestFixture]
public class TestArchitectureContextBehaviorTests
{
    private static IEnumerable<TestCaseData> EventContextFactories()
    {
        yield return new TestCaseData(
                new Func<IArchitectureContext>(() => new TestArchitectureContext()))
            .SetArgDisplayNames(nameof(TestArchitectureContext));
        yield return new TestCaseData(
                new Func<IArchitectureContext>(() => new TestArchitectureContextV3()))
            .SetArgDisplayNames(nameof(TestArchitectureContextV3));
    }

    /// <summary>
    ///     验证测试上下文会把事件注册与发送委托到同一个事件总线实例。
    /// </summary>
    [Test]
    public void RegisterEvent_And_SendEvent_Should_Use_Shared_EventBus()
    {
        var context = new TestArchitectureContext();
        var eventReceived = false;

        context.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     验证用于 ArchitectureServices 的上下文替身也会把事件注册与发送委托到同一个事件总线实例。
    /// </summary>
    [Test]
    public void RegisterEvent_And_SendEvent_On_TestArchitectureContextV3_Should_Use_Shared_EventBus()
    {
        var context = new TestArchitectureContextV3();
        var eventReceived = false;

        context.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     验证两类架构测试替身都会对事件 API 的空参数执行显式校验。
    /// </summary>
    /// <param name="createContext">创建待验证测试上下文的工厂。</param>
    [TestCaseSource(nameof(EventContextFactories))]
    public void Event_APIs_Should_Validate_Null_Arguments(Func<IArchitectureContext> createContext)
    {
        var context = createContext();

        Assert.That(() => context.SendEvent<TestEventV2>(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => context.RegisterEvent<TestEventV2>(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => context.UnRegisterEvent<TestEventV2>(null!), Throws.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    ///     验证两类架构测试替身在注销事件处理器后都不会继续分发同一回调。
    /// </summary>
    /// <param name="createContext">创建待验证测试上下文的工厂。</param>
    [TestCaseSource(nameof(EventContextFactories))]
    public void UnRegisterEvent_Should_Stop_Dispatch(Func<IArchitectureContext> createContext)
    {
        var context = createContext();
        var eventReceived = false;

        void Handler(TestEventV2 _)
        {
            eventReceived = true;
        }

        context.RegisterEvent<TestEventV2>(Handler);
        context.UnRegisterEvent<TestEventV2>(Handler);
        context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.False);
    }

    /// <summary>
    ///     验证两类架构测试替身在接口视角下都会以 no-op 方式接受生命周期钩子。
    /// </summary>
    [Test]
    public void RegisterLifecycleHook_Via_Interface_Should_Return_Original_Hook()
    {
        IArchitecture withRegistry = new TestArchitectureWithRegistry(new TestRegistry());
        IArchitecture withoutRegistry = new TestArchitectureWithoutRegistry();
        var hook = new NoOpLifecycleHook();

        Assert.That(withRegistry.RegisterLifecycleHook(hook), Is.SameAs(hook));
        Assert.That(withoutRegistry.RegisterLifecycleHook(hook), Is.SameAs(hook));
    }

    /// <summary>
    ///     为生命周期钩子接口提供空实现的测试替身。
    /// </summary>
    private sealed class NoOpLifecycleHook : IArchitectureLifecycleHook
    {
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
        {
            ArgumentNullException.ThrowIfNull(architecture);
        }
    }
}

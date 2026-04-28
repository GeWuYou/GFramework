using System;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Query;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     覆盖测试架构上下文替身的共享事件与显式失败契约。
/// </summary>
[TestFixture]
public class TestArchitectureContextBehaviorTests
{
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
    ///     验证测试上下文的旧版命令与查询入口会显式抛出未支持异常。
    /// </summary>
    [Test]
    public async Task Legacy_Entries_Should_Throw_Or_Return_Faulted_Tasks()
    {
        var context = new TestArchitectureContext();

        Assert.That(() => context.SendCommand(new TestCommandV2()), Throws.TypeOf<NotSupportedException>());
        Assert.That(
            () => context.SendCommand(new TestCommandWithResultV2 { Result = 1 }),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(() => context.SendQuery(new TestQueryV2 { Result = 1 }), Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendCommandAsync(new TestAsyncCommand()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendCommandAsync(new TestAsyncCommandWithResult()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendQueryAsync(new TestAsyncQuery()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
    }

    /// <summary>
    ///     验证用于 ArchitectureServices 的上下文替身也会把旧版入口显式标记为不支持。
    /// </summary>
    [Test]
    public async Task Legacy_Entries_On_TestArchitectureContextV3_Should_Throw_Or_Return_Faulted_Tasks()
    {
        var context = new TestArchitectureContextV3();

        Assert.That(() => context.SendCommand(new TestCommandV2()), Throws.TypeOf<NotSupportedException>());
        Assert.That(
            () => context.SendCommand(new TestCommandWithResultV2 { Result = 1 }),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(() => context.SendQuery(new TestQueryV2 { Result = 1 }), Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendCommandAsync(new TestAsyncCommand()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendCommandAsync(new TestAsyncCommandWithResult()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
        Assert.That(
            async () => await context.SendQueryAsync(new TestAsyncQuery()).ConfigureAwait(false),
            Throws.TypeOf<NotSupportedException>());
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
    ///     为旧版异步命令入口提供最小实现的测试命令。
    /// </summary>
    private sealed class TestAsyncCommand : IAsyncCommand
    {
        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }

        public IArchitectureContext GetContext()
        {
            throw new NotSupportedException();
        }

        public void SetContext(IArchitectureContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }

    /// <summary>
    ///     为旧版异步命令入口提供最小实现的带结果测试命令。
    /// </summary>
    private sealed class TestAsyncCommandWithResult : IAsyncCommand<int>
    {
        public Task<int> ExecuteAsync()
        {
            return Task.FromResult(1);
        }

        public IArchitectureContext GetContext()
        {
            throw new NotSupportedException();
        }

        public void SetContext(IArchitectureContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }

    /// <summary>
    ///     为旧版异步查询入口提供最小实现的测试查询。
    /// </summary>
    private sealed class TestAsyncQuery : IAsyncQuery<int>
    {
        public Task<int> DoAsync()
        {
            return Task.FromResult(1);
        }
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

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs.Request;
using GFramework.Cqrs.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 <see cref="ArchitectureContext" /> 与可选安装的 CQRS runtime 之间的集成边界。
///     覆盖四类契约：
///     - 通过 <see cref="ArchitectureContextCqrsExtensions" /> 进入 request / notification / stream 分发；
///     - <see cref="ArchitectureCqrsDispatchContext" /> 把架构上下文注入 <c>IContextAware</c> 处理器；
///     - 架构销毁后旧上下文引用立即失效（生命周期窗口）；
///     - 未安装 <see cref="CqrsRuntimeModule" /> 时显式失败。
/// </summary>
[NonParallelizable]
[TestFixture]
public class ArchitectureContextCqrsIntegrationTests
{
    /// <summary>
    ///     初始化测试所需的日志工厂解析器。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
    }

    /// <summary>
    ///     清理测试过程中写入的全局上下文状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     验证请求通过架构上下文扩展进入 runtime 并返回处理器结果。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task SendRequestAsync_Should_Return_Handler_Result_Through_Architecture_Context()
    {
        var (context, container) = CreateFrozenContext(services =>
            services.AddSingleton<IRequestHandler<PingRequest, int>>(new PingRequestHandler()));

        try
        {
            var result = await context.SendRequestAsync(new PingRequest()).ConfigureAwait(false);

            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     验证通知通过架构上下文扩展进入 runtime 并调用全部已注册处理器。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task PublishAsync_Should_Invoke_Registered_Notification_Handler()
    {
        var handler = new ProbeNotificationHandler();
        var (context, container) = CreateFrozenContext(services =>
            services.AddSingleton<INotificationHandler<ProbeNotification>>(handler));

        try
        {
            await context.PublishAsync(new ProbeNotification()).ConfigureAwait(false);

            Assert.That(handler.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     验证流式请求通过架构上下文扩展建流，并按处理器声明的顺序产出元素。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task CreateStream_Should_Yield_Handler_Elements_Through_Architecture_Context()
    {
        var (context, container) = CreateFrozenContext(services =>
            services.AddSingleton<IStreamRequestHandler<CountingStreamRequest, int>, CountingStreamRequestHandler>());

        try
        {
            var elements = new List<int>();
            await foreach (var element in context.CreateStream(new CountingStreamRequest())
                               .ConfigureAwait(false))
            {
                elements.Add(element);
            }

            Assert.That(elements, Is.EqualTo(new[] { 1, 2, 3 }));
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     验证通过架构上下文分发时，runtime 会经由 <see cref="ArchitectureCqrsDispatchContext" />
    ///     把当前架构上下文注入声明 <c>IContextAware</c> 的处理器。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task SendRequestAsync_Should_Inject_Architecture_Context_Into_ContextAware_Handler()
    {
        var handler = new ContextAwareProbeHandler();
        var (context, container) = CreateFrozenContext(services =>
            services.AddSingleton<IRequestHandler<ContextProbeRequest, string>>(handler));

        try
        {
            var result = await context.SendRequestAsync(new ContextProbeRequest()).ConfigureAwait(false);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo("aware"));
                Assert.That(handler.ObservedContext, Is.SameAs(context));
            });
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     验证 <c>IContextAware</c> 处理器在未经架构上下文分发（未提供 context）时会显式失败，
    ///     而不是拿到隐式空上下文。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task SendAsync_Without_Context_Should_Throw_When_Handler_Is_ContextAware()
    {
        var (context, container) = CreateFrozenContext(services =>
            services.AddSingleton<IRequestHandler<ContextProbeRequest, string>, ContextAwareProbeHandler>());

        try
        {
            var runtime = container.GetRequired<ICqrsRuntime>();

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await runtime.SendAsync(new ContextProbeRequest()).ConfigureAwait(false));
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     验证架构销毁后，外部保留的上下文引用无法再发起新的 CQRS 分发。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task SendRequestAsync_Should_Throw_ObjectDisposedException_After_Architecture_Destroyed()
    {
        var architecture = new CqrsWiredArchitecture();
        await architecture.InitializeAsync().ConfigureAwait(false);
        var context = architecture.Context;

        // 销毁前分发可用，确认该用例覆盖的是生命周期窗口而不是注册缺失。
        var alive = await context.SendRequestAsync(new PingRequest()).ConfigureAwait(false);
        Assert.That(alive, Is.EqualTo(42));

        await architecture.DestroyAsync().ConfigureAwait(false);

        Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await context.SendRequestAsync(new PingRequest()).ConfigureAwait(false));
    }

    /// <summary>
    ///     验证未安装 <see cref="CqrsRuntimeModule" /> 时，上下文分发入口会显式抛出
    ///     <see cref="InvalidOperationException" />，提示安装 runtime。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task SendRequestAsync_Should_Throw_InvalidOperationException_When_Runtime_Not_Installed()
    {
        var container = new MicrosoftDiContainer();
        container.Freeze();
        var context = new ArchitectureContext(container);

        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await context.SendRequestAsync(new PingRequest()).ConfigureAwait(false));
        }
        finally
        {
            container.Dispose();
        }
    }

    /// <summary>
    ///     创建一个已冻结、显式接线默认 CQRS runtime 的最小架构上下文。
    /// </summary>
    /// <param name="configure">
    ///     额外的 handler / behavior 注册；发生在容器冻结前，遵循标准组合根顺序。
    /// </param>
    /// <returns>架构上下文与承载它的容器；调用方负责释放容器。</returns>
    private static (ArchitectureContext Context, MicrosoftDiContainer Container) CreateFrozenContext(
        Action<IServiceCollection>? configure = null)
    {
        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterInfrastructure(container);
        configure?.Invoke(container.GetServicesUnsafe);
        container.Freeze();
        return (new ArchitectureContext(container), container);
    }

    /// <summary>
    ///     显式接线 CQRS runtime 的最小测试架构，用于验证真实架构生命周期窗口。
    /// </summary>
    private sealed class CqrsWiredArchitecture : Architecture
    {
        /// <summary>
        ///     在组合根阶段安装默认 runtime 与请求处理器，模拟用户显式启用 CQRS 的标准路径。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => services =>
        {
            services.AddCqrsRuntime(LoggerFactoryResolver.Provider);
            services.AddSingleton<IRequestHandler<PingRequest, int>, PingRequestHandler>();
        };

        /// <summary>
        ///     保持空配置，聚焦生命周期窗口行为。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
        }
    }

    /// <summary>
    ///     最小测试请求。
    /// </summary>
    private sealed class PingRequest : IRequest<int>
    {
    }

    /// <summary>
    ///     返回固定结果的测试请求处理器。
    /// </summary>
    private sealed class PingRequestHandler : IRequestHandler<PingRequest, int>
    {
        /// <summary>
        ///     返回固定结果 42。
        /// </summary>
        /// <param name="request">请求实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定结果。</returns>
        public ValueTask<int> Handle(PingRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(42);
        }
    }

    /// <summary>
    ///     记录发布次数的最小测试通知。
    /// </summary>
    private sealed record ProbeNotification : INotification;

    /// <summary>
    ///     记录调用次数的通知处理器。
    /// </summary>
    private sealed class ProbeNotificationHandler : INotificationHandler<ProbeNotification>
    {
        /// <summary>
        ///     获取处理器被调用的次数。
        /// </summary>
        public int InvocationCount { get; private set; }

        /// <summary>
        ///     处理通知并递增调用计数。
        /// </summary>
        /// <param name="notification">通知实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示处理完成的值任务。</returns>
        public ValueTask Handle(ProbeNotification notification, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     最小流式测试请求。
    /// </summary>
    private sealed class CountingStreamRequest : IStreamRequest<int>
    {
    }

    /// <summary>
    ///     依次产出 1、2、3 的流式请求处理器。
    /// </summary>
    private sealed class CountingStreamRequestHandler : IStreamRequestHandler<CountingStreamRequest, int>
    {
        /// <summary>
        ///     产出固定序列。
        /// </summary>
        /// <param name="request">请求实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>包含 1、2、3 的异步序列。</returns>
        public async IAsyncEnumerable<int> Handle(
            CountingStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var element in new[] { 1, 2, 3 })
            {
                yield return element;
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     需要架构上下文注入的最小测试请求。
    /// </summary>
    private sealed class ContextProbeRequest : IRequest<string>
    {
    }

    /// <summary>
    ///     声明 <c>IContextAware</c>（经 <see cref="AbstractRequestHandler{TRequest,TResponse}" />）的测试处理器，
    ///     记录分发期间注入的架构上下文。
    /// </summary>
    private sealed class ContextAwareProbeHandler : AbstractRequestHandler<ContextProbeRequest, string>
    {
        /// <summary>
        ///     获取分发期间注入的架构上下文。
        /// </summary>
        public IArchitectureContext? ObservedContext { get; private set; }

        /// <summary>
        ///     记录当前注入的上下文并返回固定结果。
        /// </summary>
        /// <param name="request">请求实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定结果。</returns>
        public override ValueTask<string> Handle(ContextProbeRequest request, CancellationToken cancellationToken)
        {
            ObservedContext = Context;
            return ValueTask.FromResult("aware");
        }
    }
}

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;
using GFramework.Cqrs.Notification;
using GFramework.Cqrs.Tests.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证默认 CQRS runtime 的通知发布策略接缝。
/// </summary>
[TestFixture]
internal sealed class CqrsNotificationPublisherTests
{
    /// <summary>
    ///     验证当调用方显式提供自定义通知发布器时，dispatcher 会按该发布器定义的顺序执行处理器。
    /// </summary>
    [Test]
    public async Task PublishAsync_Should_Use_Custom_NotificationPublisher_When_Runtime_Is_Created_With_It()
    {
        var invocationOrder = new List<string>();
        var handlers = new object[]
        {
            new RecordingNotificationHandler("first", invocationOrder),
            new RecordingNotificationHandler("second", invocationOrder)
        };
        var runtime = CreateRuntime(
            container =>
            {
                container
                    .Setup(currentContainer => currentContainer.GetAll(typeof(INotificationHandler<PublisherNotification>)))
                    .Returns(handlers);
            },
            new ReverseOrderNotificationPublisher());

        await runtime.PublishAsync(new FakeCqrsContext(), new PublisherNotification()).ConfigureAwait(false);

        Assert.That(invocationOrder, Is.EqualTo(["second", "first"]));
    }

    /// <summary>
    ///     验证当容器在 runtime 创建前已显式注册自定义通知发布器时，
    ///     `RegisterInfrastructure` 这条默认接线会复用该策略。
    /// </summary>
    [Test]
    public async Task RegisterInfrastructure_Should_Use_PreRegistered_NotificationPublisher()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        var container = new MicrosoftDiContainer();
        var publisher = new TrackingNotificationPublisher();
        container.Register<INotificationPublisher>(publisher);
        container.Register<INotificationHandler<PublisherNotification>>(new RecordingNotificationHandler("only", []));
        CqrsTestRuntime.RegisterInfrastructure(container);
        container.Freeze();

        var context = new ArchitectureContext(container);

        await context.PublishAsync(new PublisherNotification()).ConfigureAwait(false);

        Assert.That(publisher.WasCalled, Is.True);
    }

    /// <summary>
    ///     验证自定义通知发布器通过发布上下文回调执行处理器时，dispatcher 仍会在调用前注入当前架构上下文。
    /// </summary>
    [Test]
    public async Task PublishAsync_Should_Prepare_Context_Before_Custom_Publisher_Invokes_Handler()
    {
        var handler = new ContextAwarePublisherTestHandler();
        var architectureContext = new Mock<IArchitectureContext>(MockBehavior.Strict);
        var runtime = CreateRuntime(
            container =>
            {
                container
                    .Setup(currentContainer => currentContainer.GetAll(typeof(INotificationHandler<PublisherNotification>)))
                    .Returns([handler]);
            },
            new PassthroughNotificationPublisher());

        await runtime.PublishAsync(architectureContext.Object, new PublisherNotification()).ConfigureAwait(false);

        Assert.That(handler.ObservedContext, Is.SameAs(architectureContext.Object));
    }

    /// <summary>
    ///     验证默认通知发布器在零处理器场景下会保持静默完成。
    /// </summary>
    [Test]
    public void PublishAsync_Should_Complete_When_No_Handlers_Are_Registered()
    {
        var runtime = CreateRuntime(
            container =>
            {
                container
                    .Setup(currentContainer => currentContainer.GetAll(typeof(INotificationHandler<PublisherNotification>)))
                    .Returns(Array.Empty<object>());
            });

        Assert.That(
            async () => await runtime.PublishAsync(new FakeCqrsContext(), new PublisherNotification()).ConfigureAwait(false),
            Throws.Nothing);
    }

    /// <summary>
    ///     验证默认通知发布器会保持“首个异常立即中断后续处理器”的既有语义。
    /// </summary>
    [Test]
    public void PublishAsync_Should_Stop_After_First_Handler_Exception_When_Using_Default_Publisher()
    {
        var trailingHandler = new RecordingNotificationHandler("second", []);
        var runtime = CreateRuntime(
            container =>
            {
                container
                    .Setup(currentContainer => currentContainer.GetAll(typeof(INotificationHandler<PublisherNotification>)))
                    .Returns(
                    [
                        new ThrowingNotificationHandler(),
                        trailingHandler
                    ]);
            });

        Assert.That(
            async () => await runtime.PublishAsync(new FakeCqrsContext(), new PublisherNotification()).ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        Assert.That(trailingHandler.Invoked, Is.False);
    }

    /// <summary>
    ///     创建一个只满足当前测试最小依赖面的 dispatcher runtime。
    /// </summary>
    /// <param name="configureContainer">对容器 mock 的额外配置。</param>
    /// <param name="notificationPublisher">要注入的自定义通知发布器；若为 <see langword="null" /> 则使用默认发布器。</param>
    /// <returns>默认 CQRS runtime。</returns>
    private static GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime CreateRuntime(
        Action<Mock<IIocContainer>> configureContainer,
        INotificationPublisher? notificationPublisher = null)
    {
        var container = new Mock<IIocContainer>(MockBehavior.Strict);
        var logger = new TestLogger(nameof(CqrsNotificationPublisherTests), LogLevel.Debug);

        configureContainer(container);
        return CqrsRuntimeFactory.CreateRuntime(container.Object, logger, notificationPublisher);
    }

    /// <summary>
    ///     为当前测试提供最小的 CQRS 上下文标记。
    /// </summary>
    private sealed class FakeCqrsContext : ICqrsContext
    {
    }

    /// <summary>
    ///     为通知发布器测试提供最小通知类型。
    /// </summary>
    private sealed record PublisherNotification : INotification;

    /// <summary>
    ///     按传入顺序直接执行处理器的测试发布器。
    /// </summary>
    private sealed class PassthroughNotificationPublisher : INotificationPublisher
    {
        /// <summary>
        ///     按当前处理器集合顺序执行所有处理器。
        /// </summary>
        /// <typeparam name="TNotification">通知类型。</typeparam>
        /// <param name="context">当前发布上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示通知发布完成的值任务。</returns>
        public async ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            foreach (var handler in context.Handlers)
            {
                await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     按逆序执行处理器的测试发布器，用于证明 dispatcher 已真正委托给自定义策略。
    /// </summary>
    private sealed class ReverseOrderNotificationPublisher : INotificationPublisher
    {
        /// <summary>
        ///     按逆序执行当前发布上下文中的所有处理器。
        /// </summary>
        /// <typeparam name="TNotification">通知类型。</typeparam>
        /// <param name="context">当前发布上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示通知发布完成的值任务。</returns>
        public async ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            for (var index = context.Handlers.Count - 1; index >= 0; index--)
            {
                await context.InvokeHandlerAsync(context.Handlers[index], cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     仅记录自身是否被调用的测试发布器，用于验证默认接线是否已接管到自定义策略。
    /// </summary>
    private sealed class TrackingNotificationPublisher : INotificationPublisher
    {
        /// <summary>
        ///     获取当前发布器是否至少执行过一次发布。
        /// </summary>
        public bool WasCalled { get; private set; }

        /// <summary>
        ///     记录当前发布器已被调用，并继续按当前顺序执行所有处理器。
        /// </summary>
        /// <typeparam name="TNotification">通知类型。</typeparam>
        /// <param name="context">当前发布上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示通知发布完成的值任务。</returns>
        public async ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            WasCalled = true;

            foreach (var handler in context.Handlers)
            {
                await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     记录调用顺序的最小通知处理器。
    /// </summary>
    private sealed class RecordingNotificationHandler : INotificationHandler<PublisherNotification>
    {
        private readonly List<string> _invocationOrder;
        private readonly string _name;

        /// <summary>
        ///     初始化一个记录调用顺序的测试处理器。
        /// </summary>
        /// <param name="name">当前处理器对应的名称。</param>
        /// <param name="invocationOrder">承载调用顺序的列表。</param>
        public RecordingNotificationHandler(string name, List<string> invocationOrder)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(invocationOrder);

            _name = name;
            _invocationOrder = invocationOrder;
        }

        /// <summary>
        ///     获取当前处理器是否已被调用。
        /// </summary>
        public bool Invoked { get; private set; }

        /// <summary>
        ///     把当前处理器名称追加到调用顺序列表。
        /// </summary>
        /// <param name="notification">当前通知。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成的值任务。</returns>
        public ValueTask Handle(PublisherNotification notification, CancellationToken cancellationToken)
        {
            Invoked = true;
            _invocationOrder.Add(_name);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     在被调用时主动抛出异常的测试处理器。
    /// </summary>
    private sealed class ThrowingNotificationHandler : INotificationHandler<PublisherNotification>
    {
        /// <summary>
        ///     抛出固定异常，验证默认发布器的失败即停语义。
        /// </summary>
        /// <param name="notification">当前通知。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>不会成功返回。</returns>
        /// <exception cref="InvalidOperationException">始终抛出，表示当前处理器失败。</exception>
        public ValueTask Handle(PublisherNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    /// <summary>
    ///     记录 dispatcher 是否在自定义发布器路径中完成上下文注入的测试处理器。
    /// </summary>
    private sealed class ContextAwarePublisherTestHandler
        : CqrsContextAwareHandlerBase,
            INotificationHandler<PublisherNotification>
    {
        /// <summary>
        ///     获取当前处理器在执行时观察到的架构上下文。
        /// </summary>
        public IArchitectureContext? ObservedContext { get; private set; }

        /// <summary>
        ///     记录当前执行时观察到的架构上下文。
        /// </summary>
        /// <param name="notification">当前通知。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成的值任务。</returns>
        public ValueTask Handle(PublisherNotification notification, CancellationToken cancellationToken)
        {
            ObservedContext = Context;
            return ValueTask.CompletedTask;
        }
    }
}

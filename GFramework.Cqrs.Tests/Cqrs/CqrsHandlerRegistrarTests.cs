using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Tests.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS 处理器自动注册在顺序与容错层面的可观察行为。
/// </summary>
[TestFixture]
internal sealed class CqrsHandlerRegistrarTests
{
    /// <summary>
    ///     初始化测试容器并重置共享状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        DeterministicNotificationHandlerState.Reset();

        _container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(
            _container,
            typeof(CqrsHandlerRegistrarTests).Assembly);

        _container.Freeze();
        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    ///     清理测试过程中创建的上下文与共享状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context = null;
        _container = null;
        DeterministicNotificationHandlerState.Reset();
    }

    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;

    /// <summary>
    ///     验证自动扫描到的通知处理器会按稳定名称顺序执行，而不是依赖反射枚举顺序。
    /// </summary>
    [Test]
    public async Task PublishAsync_Should_Run_Notification_Handlers_In_Deterministic_Name_Order()
    {
        await _context!.PublishAsync(new DeterministicOrderNotification());

        Assert.That(
            DeterministicNotificationHandlerState.InvocationOrder,
            Is.EqualTo(
            [
                nameof(AlphaDeterministicNotificationHandler),
                nameof(ZetaDeterministicNotificationHandler)
            ]));
    }

    /// <summary>
    ///     验证部分类型加载失败时仍能保留可加载类型，并记录诊断日志。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Loadable_Types_And_Log_Warnings_When_Assembly_Load_Partially_Fails()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var capturingProvider = new CapturingLoggerFactoryProvider(LogLevel.Warning);
        var reflectionTypeLoadException = new ReflectionTypeLoadException(
            [typeof(AlphaDeterministicNotificationHandler), null],
            [new TypeLoadException("Missing optional dependency for registrar test.")]);
        var partiallyLoadableAssembly = new Mock<Assembly>();
        partiallyLoadableAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.PartiallyLoadableAssembly, Version=1.0.0.0");
        partiallyLoadableAssembly
            .Setup(static assembly => assembly.GetTypes())
            .Throws(reflectionTypeLoadException);

        LoggerFactoryResolver.Provider = capturingProvider;
        try
        {
            var container = new MicrosoftDiContainer();
            CqrsTestRuntime.RegisterHandlers(container, partiallyLoadableAssembly.Object);
            container.Freeze();

            var handlers = container.GetAll<INotificationHandler<DeterministicOrderNotification>>();
            var warningLogs = capturingProvider.Loggers
                .SelectMany(static logger => logger.Logs)
                .Where(static log => log.Level == LogLevel.Warning)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(
                    handlers.Select(static handler => handler.GetType()),
                    Is.EqualTo([typeof(AlphaDeterministicNotificationHandler)]));
                Assert.That(warningLogs.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(
                    warningLogs.Any(log => log.Message.Contains("partially failed", StringComparison.Ordinal)),
                    Is.True);
                Assert.That(
                    warningLogs.Any(log =>
                        log.Message.Contains("Missing optional dependency", StringComparison.Ordinal)),
                    Is.True);
            });
        }
        finally
        {
            LoggerFactoryResolver.Provider = originalProvider;
        }
    }

    /// <summary>
    ///     验证当程序集提供源码生成的注册器时，运行时会优先使用该注册器而不是反射扫描类型列表。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Use_Generated_Registry_When_Available()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.GeneratedRegistryAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedNotificationHandlerRegistry))]);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var handlers = container.GetAll<INotificationHandler<GeneratedRegistryNotification>>();

        Assert.That(
            handlers.Select(static handler => handler.GetType()),
            Is.EqualTo([typeof(GeneratedRegistryNotificationHandler)]));
    }

    /// <summary>
    ///     验证当生成注册器元数据损坏时，运行时会记录告警并回退到反射扫描路径。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Fall_Back_To_Reflection_When_Generated_Registry_Is_Invalid()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var capturingProvider = new CapturingLoggerFactoryProvider(LogLevel.Warning);
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.InvalidGeneratedRegistryAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(string))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetTypes())
            .Returns([typeof(AlphaDeterministicNotificationHandler)]);

        LoggerFactoryResolver.Provider = capturingProvider;
        try
        {
            var container = new MicrosoftDiContainer();
            CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
            container.Freeze();

            var handlers = container.GetAll<INotificationHandler<DeterministicOrderNotification>>();
            var warningLogs = capturingProvider.Loggers
                .SelectMany(static logger => logger.Logs)
                .Where(static log => log.Level == LogLevel.Warning)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(
                    handlers.Select(static handler => handler.GetType()),
                    Is.EqualTo([typeof(AlphaDeterministicNotificationHandler)]));
                Assert.That(
                    warningLogs.Any(log =>
                        log.Message.Contains("does not implement", StringComparison.Ordinal)),
                    Is.True);
            });
        }
        finally
        {
            LoggerFactoryResolver.Provider = originalProvider;
        }
    }
}

/// <summary>
///     记录确定性通知处理器的实际执行顺序。
/// </summary>
internal static class DeterministicNotificationHandlerState
{
    /// <summary>
    ///     获取当前测试中的通知处理器执行顺序。
    /// </summary>
    public static List<string> InvocationOrder { get; } = [];

    /// <summary>
    ///     重置共享的执行顺序状态。
    /// </summary>
    public static void Reset()
    {
        InvocationOrder.Clear();
    }
}

/// <summary>
///     用于验证同一通知的多个处理器是否按稳定顺序执行。
/// </summary>
internal sealed record DeterministicOrderNotification : INotification;

/// <summary>
///     故意放在 Alpha 之前声明，用于验证注册器不会依赖源码声明顺序。
/// </summary>
internal sealed class ZetaDeterministicNotificationHandler : INotificationHandler<DeterministicOrderNotification>
{
    /// <summary>
    ///     记录当前处理器已执行。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(DeterministicOrderNotification notification, CancellationToken cancellationToken)
    {
        DeterministicNotificationHandlerState.InvocationOrder.Add(nameof(ZetaDeterministicNotificationHandler));
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     名称排序上应先于 Zeta 处理器执行的通知处理器。
/// </summary>
internal sealed class AlphaDeterministicNotificationHandler : INotificationHandler<DeterministicOrderNotification>
{
    /// <summary>
    ///     记录当前处理器已执行。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(DeterministicOrderNotification notification, CancellationToken cancellationToken)
    {
        DeterministicNotificationHandlerState.InvocationOrder.Add(nameof(AlphaDeterministicNotificationHandler));
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     为 CQRS 注册测试捕获真实启动路径中创建的日志记录器。
/// </summary>
/// <remarks>
///     处理器注册入口会分别为测试运行时、容器和注册器创建日志器。
///     该提供程序统一保留这些测试日志器，以便断言警告是否经由公开入口真正发出。
/// </remarks>
internal sealed class CapturingLoggerFactoryProvider : ILoggerFactoryProvider
{
    private readonly List<TestLogger> _loggers = [];

    /// <summary>
    ///     使用指定的最小日志级别初始化一个新的捕获型日志工厂提供程序。
    /// </summary>
    /// <param name="minLevel">要应用到新建测试日志器的最小日志级别。</param>
    public CapturingLoggerFactoryProvider(LogLevel minLevel = LogLevel.Info)
    {
        MinLevel = minLevel;
    }

    /// <summary>
    ///     获取通过当前提供程序创建的全部测试日志器。
    /// </summary>
    public IReadOnlyList<TestLogger> Loggers => _loggers;

    /// <summary>
    ///     获取或设置新建测试日志器的最小日志级别。
    /// </summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    ///     创建一个测试日志器并将其纳入捕获集合。
    /// </summary>
    /// <param name="name">日志记录器名称。</param>
    /// <returns>用于后续断言的测试日志器。</returns>
    public ILogger CreateLogger(string name)
    {
        var logger = new TestLogger(name, MinLevel);
        _loggers.Add(logger);
        return logger;
    }
}

/// <summary>
///     用于验证生成注册器路径的通知消息。
/// </summary>
internal sealed record GeneratedRegistryNotification : INotification;

/// <summary>
///     由模拟的源码生成注册器显式注册的通知处理器。
/// </summary>
internal sealed class GeneratedRegistryNotificationHandler : INotificationHandler<GeneratedRegistryNotification>
{
    /// <summary>
    ///     处理生成注册器测试中的通知。
    /// </summary>
    /// <param name="notification">通知实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(GeneratedRegistryNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     模拟源码生成器为某个程序集生成的 CQRS 处理器注册器。
/// </summary>
internal sealed class GeneratedNotificationHandlerRegistry : ICqrsHandlerRegistry
{
    /// <summary>
    ///     将测试通知处理器注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(INotificationHandler<GeneratedRegistryNotification>),
            typeof(GeneratedRegistryNotificationHandler));
        logger.Debug(
            $"Registered CQRS handler {typeof(GeneratedRegistryNotificationHandler).FullName} as {typeof(INotificationHandler<GeneratedRegistryNotification>).FullName}.");
    }
}

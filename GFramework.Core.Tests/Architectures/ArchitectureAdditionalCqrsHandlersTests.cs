using System.Reflection;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证架构初始化阶段可以显式接入默认程序集之外的 CQRS handlers。
/// </summary>
[TestFixture]
public sealed class ArchitectureAdditionalCqrsHandlersTests
{
    /// <summary>
    ///     初始化日志工厂和共享测试状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _previousLoggerFactoryProvider = LoggerFactoryResolver.Provider;
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
        AdditionalAssemblyNotificationHandlerState.Reset();
    }

    /// <summary>
    ///     清理测试过程中写入的共享状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        AdditionalAssemblyNotificationHandlerState.Reset();
        GameContext.Clear();
        LoggerFactoryResolver.Provider = _previousLoggerFactoryProvider
                                         ?? throw new InvalidOperationException(
                                             "LoggerFactoryResolver.Provider should be captured during setup.");
    }

    private ILoggerFactoryProvider? _previousLoggerFactoryProvider;

    /// <summary>
    ///     验证显式声明的额外程序集会在初始化阶段接入当前架构容器。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task RegisterCqrsHandlersFromAssembly_Should_Register_Handlers_From_Explicit_Assembly()
    {
        var generatedAssembly = CreateGeneratedHandlerAssembly();
        var architecture = CreateArchitecture(target =>
            target.RegisterCqrsHandlersFromAssembly(generatedAssembly.Object));

        await architecture.InitializeAsync();
        try
        {
            await architecture.Context.PublishAsync(new AdditionalAssemblyNotification());

            Assert.That(AdditionalAssemblyNotificationHandlerState.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     验证不同 <see cref="Assembly" /> 实例只要解析到相同程序集键，就不会向容器重复写入相同 handler 映射。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task RegisterCqrsHandlersFromAssembly_Should_Deduplicate_Repeated_Assembly_Registration()
    {
        var generatedAssemblyA = CreateGeneratedHandlerAssembly();
        var generatedAssemblyB = CreateGeneratedHandlerAssembly();
        var architecture = CreateArchitecture(target =>
        {
            target.RegisterCqrsHandlersFromAssembly(generatedAssemblyA.Object);
            target.RegisterCqrsHandlersFromAssemblies([generatedAssemblyB.Object]);
        });

        await architecture.InitializeAsync();
        try
        {
            await architecture.Context.PublishAsync(new AdditionalAssemblyNotification());

            Assert.That(AdditionalAssemblyNotificationHandlerState.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     创建一个仅暴露程序集级 CQRS registry 元数据的 mocked Assembly。
    ///     该测试替身模拟“扩展程序集已经挂接 source-generator，运行时只需显式接入该程序集”的真实路径。
    /// </summary>
    /// <returns>包含程序集级 handler registry 元数据的 mocked Assembly。</returns>
    private static Mock<Assembly> CreateGeneratedHandlerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Architectures.ExplicitAdditionalHandlers, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(AdditionalAssemblyNotificationHandlerRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建复用现有测试架构基建的测试架构，并在注册阶段后执行额外程序集接入逻辑。
    /// </summary>
    /// <param name="configure">初始化阶段执行的额外 CQRS 程序集接入逻辑。</param>
    /// <returns>带有注册后钩子的测试架构实例。</returns>
    private static SyncTestArchitecture CreateArchitecture(Action<TestArchitectureBase> configure)
    {
        var architecture = new SyncTestArchitecture();
        architecture.AddPostRegistrationHook(configure);
        return architecture;
    }
}

/// <summary>
///     用于验证额外程序集接入是否成功的测试通知。
/// </summary>
public sealed record AdditionalAssemblyNotification : INotification;

/// <summary>
///     记录模拟扩展程序集通知处理器的执行次数。
/// </summary>
public static class AdditionalAssemblyNotificationHandlerState
{
    private static int _invocationCount;

    /// <summary>
    ///     获取当前测试进程中该处理器的执行次数。
    /// </summary>
    /// <remarks>
    ///     该计数器通过原子读写维护，以支持 NUnit 并行执行环境中的并发访问。
    /// </remarks>
    public static int InvocationCount => Volatile.Read(ref _invocationCount);

    /// <summary>
    ///     记录一次通知处理，供测试断言显式程序集接入后的运行时行为。
    /// </summary>
    public static void RecordInvocation()
    {
        Interlocked.Increment(ref _invocationCount);
    }

    /// <summary>
    ///     清理共享计数器，避免测试间相互污染。
    /// </summary>
    public static void Reset()
    {
        Interlocked.Exchange(ref _invocationCount, 0);
    }
}

/// <summary>
///     模拟由 source-generator 为扩展程序集生成的 CQRS handler registry。
/// </summary>
internal sealed class AdditionalAssemblyNotificationHandlerRegistry : ICqrsHandlerRegistry
{
    /// <summary>
    ///     将扩展程序集中的通知处理器映射写入服务集合。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="logger">日志记录器。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="services" /> 或 <paramref name="logger" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient<INotificationHandler<AdditionalAssemblyNotification>>(_ => CreateHandler());
        logger.Debug(
            $"Registered CQRS handler proxy for {typeof(INotificationHandler<AdditionalAssemblyNotification>).FullName}.");
    }

    /// <summary>
    ///     创建一个仅供显式程序集注册路径使用的动态通知处理器。
    /// </summary>
    /// <returns>用于记录通知触发次数的测试替身处理器。</returns>
    private static INotificationHandler<AdditionalAssemblyNotification> CreateHandler()
    {
        var handler = new Mock<INotificationHandler<AdditionalAssemblyNotification>>();
        handler
            .Setup(target => target.Handle(It.IsAny<AdditionalAssemblyNotification>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                AdditionalAssemblyNotificationHandlerState.RecordInvocation();
                return ValueTask.CompletedTask;
            });
        return handler.Object;
    }
}

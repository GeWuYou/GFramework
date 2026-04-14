using System.Reflection;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Tests.Logging;

namespace GFramework.Core.Tests.Cqrs;

/// <summary>
///     验证 CQRS 处理器自动注册在顺序与容错层面的可观察行为。
/// </summary>
[TestFixture]
internal sealed class CqrsHandlerRegistrarTests
{
    private static readonly MethodInfo RecoverLoadableTypesMethod = typeof(ArchitectureContext).Assembly
                                                                        .GetType(
                                                                            "GFramework.Core.Cqrs.Internal.CqrsHandlerRegistrar",
                                                                            throwOnError: true)!
                                                                        .GetMethod("RecoverLoadableTypes",
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.Static)!
                                                                    ?? throw new InvalidOperationException(
                                                                        "Failed to locate CqrsHandlerRegistrar.RecoverLoadableTypes.");

    private MicrosoftDiContainer? _container;

    private ArchitectureContext? _context;

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
            typeof(CqrsHandlerRegistrarTests).Assembly,
            typeof(ArchitectureContext).Assembly);

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
    public void RecoverLoadableTypes_Should_Return_Loadable_Types_And_Log_Warnings()
    {
        var logger = new TestLogger(nameof(CqrsHandlerRegistrarTests), LogLevel.Warning);
        var reflectionTypeLoadException = new ReflectionTypeLoadException(
            [typeof(AlphaDeterministicNotificationHandler), null],
            [new TypeLoadException("Missing optional dependency for registrar test.")]);

        var recoveredTypes = (IReadOnlyList<Type>)RecoverLoadableTypesMethod.Invoke(
            null,
            [typeof(CqrsHandlerRegistrarTests).Assembly, reflectionTypeLoadException, logger])!;

        Assert.Multiple(() =>
        {
            Assert.That(recoveredTypes, Is.EqualTo([typeof(AlphaDeterministicNotificationHandler)]));
            Assert.That(logger.Logs.Count(log => log.Level == LogLevel.Warning), Is.GreaterThanOrEqualTo(2));
            Assert.That(
                logger.Logs.Any(log => log.Message.Contains("partially failed", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                logger.Logs.Any(log => log.Message.Contains("Missing optional dependency", StringComparison.Ordinal)),
                Is.True);
        });
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

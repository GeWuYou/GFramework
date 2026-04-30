using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Tests.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS handler registrar 在 reflection fallback 元数据失效时的可观察告警行为。
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class CqrsHandlerRegistrarFallbackFailureTests
{
    private ILoggerFactoryProvider? _originalLoggerFactoryProvider;
    private CapturingLoggerFactoryProvider? _capturingLoggerFactoryProvider;

    /// <summary>
    ///     切换为捕获型日志工厂，并清空 registrar 进程级缓存，避免跨用例共享状态污染断言。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _originalLoggerFactoryProvider = LoggerFactoryResolver.Provider;
        _capturingLoggerFactoryProvider = new CapturingLoggerFactoryProvider(LogLevel.Warning);
        LoggerFactoryResolver.Provider = _capturingLoggerFactoryProvider;
        ClearRegistrarCaches();
    }

    /// <summary>
    ///     恢复测试前的日志工厂，并清理 registrar 缓存。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        LoggerFactoryResolver.Provider = _originalLoggerFactoryProvider!;
        _capturingLoggerFactoryProvider = null;
        _originalLoggerFactoryProvider = null;
        ClearRegistrarCaches();
    }

    /// <summary>
    ///     验证当 fallback 类型名无法解析时，registrar 会跳过该条目并记录告警。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Skip_Unresolvable_Named_Fallback_And_Log_Warning()
    {
        const string missingTypeName =
            "GFramework.Cqrs.Tests.Cqrs.MissingGeneratedRegistryNotificationHandler";
        var generatedAssembly = CreateGeneratedFallbackAssembly(
            "GFramework.Cqrs.Tests.Cqrs.NamedFallbackMissingAssembly, Version=1.0.0.0",
            new CqrsReflectionFallbackAttribute(missingTypeName));
        generatedAssembly
            .Setup(static assembly => assembly.GetType(missingTypeName, false, false))
            .Returns((Type?)null);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        Assert.Multiple(() =>
        {
            Assert.That(
                GetGeneratedRegistryNotificationHandlerTypes(container),
                Is.EqualTo([typeof(GeneratedRegistryNotificationHandler)]));
            Assert.That(
                GetWarningLogs().Any(log =>
                    log.Message.Contains(
                        $"Generated CQRS reflection fallback type {missingTypeName} could not be resolved",
                        StringComparison.Ordinal)),
                Is.True);
        });
    }

    /// <summary>
    ///     验证当 fallback 类型名解析抛出异常时，registrar 会记录该加载失败告警并继续跳过条目。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Log_Warning_When_Named_Fallback_Resolution_Throws()
    {
        const string failingTypeName =
            "GFramework.Cqrs.Tests.Cqrs.ThrowingGeneratedRegistryNotificationHandler";
        const string exceptionMessage = "Fallback resolution exploded.";
        var generatedAssembly = CreateGeneratedFallbackAssembly(
            "GFramework.Cqrs.Tests.Cqrs.NamedFallbackThrowingAssembly, Version=1.0.0.0",
            new CqrsReflectionFallbackAttribute(failingTypeName));
        generatedAssembly
            .Setup(static assembly => assembly.GetType(failingTypeName, false, false))
            .Throws(new TypeLoadException(exceptionMessage));

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        Assert.Multiple(() =>
        {
            Assert.That(
                GetGeneratedRegistryNotificationHandlerTypes(container),
                Is.EqualTo([typeof(GeneratedRegistryNotificationHandler)]));
            Assert.That(
                GetWarningLogs().Any(log =>
                    log.Message.Contains(
                        $"Generated CQRS reflection fallback type {failingTypeName} failed to load",
                        StringComparison.Ordinal) &&
                    log.Message.Contains(exceptionMessage, StringComparison.Ordinal)),
                Is.True);
        });
    }

    /// <summary>
    ///     验证当 direct fallback 类型属于其他程序集时，registrar 会跳过该条目并记录跨程序集告警。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Skip_Cross_Assembly_Direct_Fallback_Type_And_Log_Warning()
    {
        var crossAssemblyFallbackType = ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType;
        var generatedAssembly = CreateGeneratedFallbackAssembly(
            "GFramework.Cqrs.Tests.Cqrs.DirectFallbackMismatchAssembly, Version=1.0.0.0",
            new CqrsReflectionFallbackAttribute(crossAssemblyFallbackType));

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        Assert.Multiple(() =>
        {
            Assert.That(
                GetGeneratedRegistryNotificationHandlerTypes(container),
                Is.EqualTo([typeof(GeneratedRegistryNotificationHandler)]));
            Assert.That(
                GetWarningLogs().Any(log =>
                    log.Message.Contains(
                        $"Generated CQRS reflection fallback type {crossAssemblyFallbackType.FullName} was declared on assembly",
                        StringComparison.Ordinal) &&
                    log.Message.Contains("Skipping mismatched fallback entry.", StringComparison.Ordinal)),
                Is.True);
        });
    }

    /// <summary>
    ///     创建一个仅通过 generated registry 注册主 handler、并附带指定 fallback 元数据的程序集替身。
    /// </summary>
    /// <param name="assemblyName">用于日志与缓存键的程序集名。</param>
    /// <param name="fallbackAttribute">要暴露给 registrar 的 fallback attribute。</param>
    /// <returns>已完成基础接线的程序集 mock。</returns>
    private static Mock<Assembly> CreateGeneratedFallbackAssembly(
        string assemblyName,
        CqrsReflectionFallbackAttribute fallbackAttribute)
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns(assemblyName);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PartialGeneratedNotificationHandlerRegistry))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false))
            .Returns([fallbackAttribute]);
        return generatedAssembly;
    }

    /// <summary>
    ///     提取容器中针对 generated notification 注册的处理器实现类型。
    /// </summary>
    /// <param name="container">已执行注册的测试容器。</param>
    /// <returns>按注册顺序返回的处理器类型数组。</returns>
    private static Type[] GetGeneratedRegistryNotificationHandlerTypes(MicrosoftDiContainer container)
    {
        return container.GetServicesUnsafe
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<GeneratedRegistryNotification>) &&
                descriptor.ImplementationType is not null)
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToArray();
    }

    /// <summary>
    ///     清空本测试依赖的 registrar 静态缓存，确保每个用例都会重新执行 fallback 元数据解析。
    ///     这些字段名直接耦合 <c>CqrsHandlerRegistrar</c> 当前内部实现；若后续重构缓存布局，需要同步更新这里。
    /// </summary>
    private static void ClearRegistrarCaches()
    {
        ClearCache(GetRegistrarCacheField("AssemblyMetadataCache"));
        ClearCache(GetRegistrarCacheField("RegistryActivationMetadataCache"));
        ClearCache(GetRegistrarCacheField("LoadableTypesCache"));
        ClearCache(GetRegistrarCacheField("SupportedHandlerInterfacesCache"));
    }

    /// <summary>
    ///     通过反射读取 registrar 的静态缓存字段。
    /// </summary>
    /// <param name="fieldName">缓存字段名。</param>
    /// <returns>缓存实例。</returns>
    private static object GetRegistrarCacheField(string fieldName)
    {
        var field = GetRegistrarType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(
            field,
            Is.Not.Null,
            $"Expected field '{fieldName}' on CqrsHandlerRegistrar not found; rename/refactor may require test update.");

        return field!.GetValue(null)
               ?? throw new InvalidOperationException(
                   $"Registrar cache field '{fieldName}' on CqrsHandlerRegistrar returned null.");
    }

    /// <summary>
    ///     清空缓存对象中的已保存条目。
    /// </summary>
    /// <param name="cache">目标缓存实例。</param>
    private static void ClearCache(object cache)
    {
        _ = InvokeInstanceMethod(cache, "Clear");
    }

    /// <summary>
    ///     调用缓存对象上的实例方法。
    /// </summary>
    /// <param name="target">目标对象。</param>
    /// <param name="methodName">方法名。</param>
    /// <param name="arguments">方法参数。</param>
    /// <returns>方法返回值。</returns>
    private static object? InvokeInstanceMethod(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing cache method {target.GetType().FullName}.{methodName}.");

        return method!.Invoke(target, arguments);
    }

    /// <summary>
    ///     获取 CQRS handler registrar 的运行时类型。
    /// </summary>
    /// <returns>registrar 实现类型。</returns>
    private static Type GetRegistrarType()
    {
        return typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!;
    }

    /// <summary>
    ///     汇总当前测试期间捕获到的 warning 日志。
    /// </summary>
    /// <returns>所有 warning 级别日志条目。</returns>
    private IReadOnlyList<TestLogger.LogEntry> GetWarningLogs()
    {
        Assert.That(_capturingLoggerFactoryProvider, Is.Not.Null);

        return _capturingLoggerFactoryProvider!.Loggers
            .SelectMany(static logger => logger.Logs)
            .Where(static log => log.Level == LogLevel.Warning)
            .ToArray();
    }
}

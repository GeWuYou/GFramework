// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
            typeof(CqrsHandlerRegistrarTests).Assembly);

        _container.Freeze();
        _context = new ArchitectureContext(_container);
        ClearRegistrarCaches();
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
        ClearRegistrarCaches();
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
    ///     验证 generated registry 使用私有无参构造器时，运行时仍可激活它并完成处理器注册。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Activate_Generated_Registry_With_Private_Parameterless_Constructor()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.PrivateGeneratedRegistryAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PrivateConstructorNotificationHandlerRegistry))]);

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

    /// <summary>
    ///     验证捕获型日志工厂在更新最小日志级别后，会将新值应用到后续创建的日志器。
    /// </summary>
    [Test]
    public void CapturingLoggerFactoryProvider_Should_Apply_Updated_MinLevel_To_Subsequent_Loggers()
    {
        var provider = new CapturingLoggerFactoryProvider(LogLevel.Warning);
        var warningLogger = (TestLogger)provider.CreateLogger("warning");

        provider.MinLevel = LogLevel.Debug;

        var debugLogger = (TestLogger)provider.CreateLogger("debug");

        Assert.Multiple(() =>
        {
            Assert.That(warningLogger.IsDebugEnabled(), Is.False);
            Assert.That(debugLogger.IsDebugEnabled(), Is.True);
            Assert.That(provider.Loggers, Has.Count.EqualTo(2));
        });
    }

    /// <summary>
    ///     验证当生成注册器提供精确 fallback 类型名时，运行时会定向补扫剩余 handlers，
    ///     而不是重新枚举整个程序集的类型列表。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Use_Targeted_Type_Lookups_For_Reflection_Fallback_Without_Duplicates()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.PartialGeneratedRegistryAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PartialGeneratedNotificationHandlerRegistry))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false))
            .Returns(
            [
                new CqrsReflectionFallbackAttribute(
                    ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!)
            ]);
        generatedAssembly
            .Setup(static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false))
            .Returns(ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var registrations = container.GetServicesUnsafe
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<GeneratedRegistryNotification>) &&
                descriptor.ImplementationType is not null)
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToList();

        Assert.That(
            registrations,
            Is.EqualTo(
            [
                typeof(GeneratedRegistryNotificationHandler),
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType
            ]));

        generatedAssembly.Verify(
            static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false),
            Times.Once);
        generatedAssembly.Verify(static assembly => assembly.GetTypes(), Times.Never);
    }

    /// <summary>
    ///     验证手写 fallback metadata 直接提供 handler 类型时，运行时会复用这些类型，
    ///     而不会再通过程序集名称查找或整程序集扫描补齐映射。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Use_Direct_Fallback_Types_Without_GetType_Or_GetTypes()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns(ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.Assembly.FullName);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PartialGeneratedNotificationHandlerRegistry))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false))
            .Returns(
            [
                new CqrsReflectionFallbackAttribute(
                    ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType)
            ]);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var registrations = container.GetServicesUnsafe
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<GeneratedRegistryNotification>) &&
                descriptor.ImplementationType is not null)
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToList();

        Assert.That(
            registrations,
            Is.EqualTo(
            [
                typeof(GeneratedRegistryNotificationHandler),
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType
            ]));

        generatedAssembly.Verify(
            static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false),
            Times.Never);
        generatedAssembly.Verify(static assembly => assembly.GetTypes(), Times.Never);
    }

    /// <summary>
    ///     验证当程序集同时声明直接 <see cref="Type" /> fallback 与字符串名称 fallback 时，
    ///     运行时会优先复用直接类型，并只对名称 fallback 做定向 <c>GetType(...)</c> 查找。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Use_Mixed_Fallback_Metadata_With_Targeted_Type_Lookups_Only_For_Named_Entries()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns(ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.Assembly.FullName);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PartialGeneratedNotificationHandlerRegistry))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false))
            .Returns(
            [
                new CqrsReflectionFallbackAttribute(
                    ReflectionFallbackNotificationContainer.DirectFallbackHandlerType),
                new CqrsReflectionFallbackAttribute(
                    ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!)
            ]);
        generatedAssembly
            .Setup(static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false))
            .Returns(ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var registrations = container.GetServicesUnsafe
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<GeneratedRegistryNotification>) &&
                descriptor.ImplementationType is not null)
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToList();

        Assert.That(
            registrations,
            Is.EqualTo(
            [
                typeof(GeneratedRegistryNotificationHandler),
                ReflectionFallbackNotificationContainer.DirectFallbackHandlerType,
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType
            ]));

        generatedAssembly.Verify(
            static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false),
            Times.Once);
        generatedAssembly.Verify(static assembly => assembly.GetTypes(), Times.Never);
    }

    /// <summary>
    ///     验证同一程序集对象重复接入多个容器时，会复用已解析的 registry / fallback 元数据，
    ///     而不是重复读取程序集级 attribute 或重复执行 type-name lookup。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Cache_Assembly_Metadata_Across_Containers()
    {
        var generatedAssembly = CreateCachedMetadataAssembly();
        var firstContainer = new MicrosoftDiContainer();
        var secondContainer = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(firstContainer, generatedAssembly.Object);
        CqrsTestRuntime.RegisterHandlers(secondContainer, generatedAssembly.Object);
        firstContainer.Freeze();
        secondContainer.Freeze();

        AssertGeneratedRegistryNotificationHandlers(firstContainer, secondContainer);
        VerifyCachedMetadataAssemblyLookups(generatedAssembly);
    }

    /// <summary>
    ///     验证同一程序集对象在未命中 generated registry 时，会复用首次扫描得到的可加载类型列表，
    ///     而不是为每个容器重复执行整程序集 <c>GetTypes()</c>。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Cache_Loadable_Types_Across_Containers()
    {
        var reflectionTypeLoadException = new ReflectionTypeLoadException(
            [typeof(AlphaDeterministicNotificationHandler), null],
            [new TypeLoadException("Cached loadable-type probe.")]);
        var partiallyLoadableAssembly = new Mock<Assembly>();
        partiallyLoadableAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.CachedLoadableTypesAssembly, Version=1.0.0.0");
        partiallyLoadableAssembly
            .Setup(static assembly => assembly.GetTypes())
            .Throws(reflectionTypeLoadException);

        var firstContainer = new MicrosoftDiContainer();
        var secondContainer = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(firstContainer, partiallyLoadableAssembly.Object);
        CqrsTestRuntime.RegisterHandlers(secondContainer, partiallyLoadableAssembly.Object);
        firstContainer.Freeze();
        secondContainer.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(
                firstContainer.GetAll<INotificationHandler<DeterministicOrderNotification>>()
                    .Select(static handler => handler.GetType())
                    .ToArray(),
                Is.EqualTo([typeof(AlphaDeterministicNotificationHandler)]));
            Assert.That(
                secondContainer.GetAll<INotificationHandler<DeterministicOrderNotification>>()
                    .Select(static handler => handler.GetType())
                    .ToArray(),
                Is.EqualTo([typeof(AlphaDeterministicNotificationHandler)]));
        });

        partiallyLoadableAssembly.Verify(static assembly => assembly.GetTypes(), Times.Once);
    }

    /// <summary>
    ///     验证同一 handler 类型跨容器重复注册时，会复用已筛选的 supported handler interface 列表，
    ///     而不是为每个容器重新执行接口反射分析。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Cache_Supported_Handler_Interfaces_Across_Containers()
    {
        var supportedHandlerInterfacesCache = GetRegistrarCacheField("SupportedHandlerInterfacesCache");
        var firstHandlerType = typeof(AlphaDeterministicNotificationHandler);
        var secondHandlerType = typeof(ZetaDeterministicNotificationHandler);
        var handlerAssembly = new Mock<Assembly>();
        handlerAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.CachedHandlerInterfacesAssembly, Version=1.0.0.0");
        handlerAssembly
            .Setup(static assembly => assembly.GetTypes())
            .Returns([firstHandlerType, secondHandlerType]);

        Assert.Multiple(() =>
        {
            Assert.That(GetSingleKeyCacheValue(supportedHandlerInterfacesCache, firstHandlerType), Is.Null);
            Assert.That(GetSingleKeyCacheValue(supportedHandlerInterfacesCache, secondHandlerType), Is.Null);
        });

        var firstContainer = new MicrosoftDiContainer();
        var secondContainer = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(firstContainer, handlerAssembly.Object);
        var firstHandlerInterfaces =
            GetSingleKeyCacheValue(supportedHandlerInterfacesCache, firstHandlerType);
        var secondHandlerInterfaces =
            GetSingleKeyCacheValue(supportedHandlerInterfacesCache, secondHandlerType);

        CqrsTestRuntime.RegisterHandlers(secondContainer, handlerAssembly.Object);

        Assert.Multiple(() =>
        {
            Assert.That(firstHandlerInterfaces, Is.Not.Null);
            Assert.That(secondHandlerInterfaces, Is.Not.Null);
            Assert.That(
                GetSingleKeyCacheValue(supportedHandlerInterfacesCache, firstHandlerType),
                Is.SameAs(firstHandlerInterfaces));
            Assert.That(
                GetSingleKeyCacheValue(supportedHandlerInterfacesCache, secondHandlerType),
                Is.SameAs(secondHandlerInterfaces));
        });

        handlerAssembly.Verify(static assembly => assembly.GetTypes(), Times.Once);
    }

    /// <summary>
    ///     验证当程序集枚举结果包含重复 handler 类型时，registrar 仍只会写入一份 handler 映射。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Skip_Duplicate_Handler_Mappings_When_Assembly_Returns_Duplicate_Types()
    {
        var handlerType = typeof(AlphaDeterministicNotificationHandler);
        var handlerAssembly = new Mock<Assembly>();
        handlerAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.DuplicateHandlerMappingsAssembly, Version=1.0.0.0");
        handlerAssembly
            .Setup(static assembly => assembly.GetTypes())
            .Returns([handlerType, handlerType]);

        var container = new MicrosoftDiContainer();
        CqrsTestRuntime.RegisterHandlers(container, handlerAssembly.Object);

        var registrations = container.GetServicesUnsafe
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<DeterministicOrderNotification>) &&
                descriptor.ImplementationType == typeof(AlphaDeterministicNotificationHandler))
            .ToArray();

        Assert.That(registrations, Has.Length.EqualTo(1));
    }

    /// <summary>
    ///     清空本测试依赖的 registrar 静态缓存，避免跨用例共享进程级状态导致断言漂移。
    /// </summary>
    private static void ClearRegistrarCaches()
    {
        ClearCache(GetRegistrarCacheField("AssemblyMetadataCache"));
        ClearCache(GetRegistrarCacheField("RegistryActivationMetadataCache"));
        ClearCache(GetRegistrarCacheField("LoadableTypesCache"));
        ClearCache(GetRegistrarCacheField("SupportedHandlerInterfacesCache"));
    }

    /// <summary>
    ///     创建一个携带 generated registry 与 reflection fallback 元数据的程序集替身，
    ///     用于验证 registrar 是否会跨容器复用程序集级元数据。
    /// </summary>
    private static Mock<Assembly> CreateCachedMetadataAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Core.Tests.Cqrs.CachedMetadataAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(PartialGeneratedNotificationHandlerRegistry))]);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false))
            .Returns(
            [
                new CqrsReflectionFallbackAttribute(
                    ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!)
            ]);
        generatedAssembly
            .Setup(static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false))
            .Returns(ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType);
        return generatedAssembly;
    }

    /// <summary>
    ///     断言两个容器都获得了相同的 generated-registry 与 reflection-fallback 处理器集合。
    /// </summary>
    private static void AssertGeneratedRegistryNotificationHandlers(
        MicrosoftDiContainer firstContainer,
        MicrosoftDiContainer secondContainer)
    {
        var firstRegistrations = GetGeneratedRegistryNotificationHandlerTypes(firstContainer);
        var secondRegistrations = GetGeneratedRegistryNotificationHandlerTypes(secondContainer);

        Assert.Multiple(() =>
        {
            Assert.That(firstRegistrations, Is.EqualTo(GetExpectedGeneratedRegistryNotificationHandlerTypes()));
            Assert.That(secondRegistrations, Is.EqualTo(GetExpectedGeneratedRegistryNotificationHandlerTypes()));
        });
    }

    /// <summary>
    ///     读取容器中针对 generated notification 的 handler 运行时类型列表。
    /// </summary>
    private static Type[] GetGeneratedRegistryNotificationHandlerTypes(MicrosoftDiContainer container)
    {
        return container.GetAll<INotificationHandler<GeneratedRegistryNotification>>()
            .Select(static handler => handler.GetType())
            .ToArray();
    }

    /// <summary>
    ///     获取 generated registry 与 reflection fallback 共同组成的预期 handler 顺序。
    /// </summary>
    private static Type[] GetExpectedGeneratedRegistryNotificationHandlerTypes()
    {
        return
        [
            typeof(GeneratedRegistryNotificationHandler),
            ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType
        ];
    }

    /// <summary>
    ///     断言程序集级 generated registry / fallback 元数据只会被读取一次。
    /// </summary>
    private static void VerifyCachedMetadataAssemblyLookups(Mock<Assembly> generatedAssembly)
    {
        generatedAssembly.Verify(
            static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false),
            Times.Once);
        generatedAssembly.Verify(
            static assembly => assembly.GetCustomAttributes(typeof(CqrsReflectionFallbackAttribute), false),
            Times.Once);
        generatedAssembly.Verify(
            static assembly => assembly.GetType(
                ReflectionFallbackNotificationContainer.ReflectionOnlyHandlerType.FullName!,
                false,
                false),
            Times.Once);
    }

    /// <summary>
    ///     通过反射读取 registrar 的静态缓存对象。
    /// </summary>
    private static object GetRegistrarCacheField(string fieldName)
    {
        var registrarType = GetRegistrarType();
        var field = registrarType.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing registrar cache field {fieldName}.");

        return field!.GetValue(null)
               ?? throw new InvalidOperationException(
                   $"Registrar cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     清空指定缓存对象。
    /// </summary>
    private static void ClearCache(object cache)
    {
        _ = InvokeInstanceMethod(cache, "Clear");
    }

    /// <summary>
    ///     读取单键缓存中当前保存的对象。
    /// </summary>
    private static object? GetSingleKeyCacheValue(object cache, Type key)
    {
        return InvokeInstanceMethod(cache, "GetValueOrDefaultForTesting", key);
    }

    /// <summary>
    ///     调用缓存对象上的实例方法。
    /// </summary>
    private static object? InvokeInstanceMethod(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing cache method {target.GetType().FullName}.{methodName}.");

        return method!.Invoke(target, arguments);
    }

    /// <summary>
    ///     获取 CQRS handler registrar 运行时类型。
    /// </summary>
    private static Type GetRegistrarType()
    {
        return typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!;
    }
}

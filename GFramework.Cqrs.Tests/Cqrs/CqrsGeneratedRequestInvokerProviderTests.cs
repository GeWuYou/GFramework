using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 generated request invoker provider 的 registrar 接线与 dispatcher 消费语义。
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class CqrsGeneratedRequestInvokerProviderTests
{
    private ILoggerFactoryProvider? _previousLoggerFactoryProvider;

    /// <summary>
    ///     在每个用例前重置 registrar / dispatcher 的静态缓存，避免跨用例共享状态影响断言。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _previousLoggerFactoryProvider = LoggerFactoryResolver.Provider;
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     在每个用例后清理静态缓存。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        LoggerFactoryResolver.Provider = _previousLoggerFactoryProvider ?? new ConsoleLoggerFactoryProvider();
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     验证 registrar 激活 generated registry 后，会把 request invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Request_Invoker_Provider()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var providers = container.GetAll<ICqrsRequestInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(GeneratedRequestInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 dispatcher 在首次创建 request binding 时，会优先消费 generated request invoker provider。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload"));
        Assert.That(response, Is.EqualTo("generated:payload"));
    }

    /// <summary>
    ///     创建带有 generated request invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateGeneratedRequestInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.GeneratedRequestInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedRequestInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     清空 registrar 静态缓存。
    /// </summary>
    private static void ClearRegistrarCaches()
    {
        ClearCache(GetRegistrarCacheField("AssemblyMetadataCache"));
        ClearCache(GetRegistrarCacheField("RegistryActivationMetadataCache"));
        ClearCache(GetRegistrarCacheField("LoadableTypesCache"));
        ClearCache(GetRegistrarCacheField("SupportedHandlerInterfacesCache"));
    }

    /// <summary>
    ///     清空 dispatcher 静态缓存。
    /// </summary>
    private static void ClearDispatcherCaches()
    {
        ClearCache(GetDispatcherCacheField("NotificationDispatchBindings"));
        ClearCache(GetDispatcherCacheField("RequestDispatchBindings"));
        ClearCache(GetDispatcherCacheField("StreamDispatchBindings"));
        ClearCache(GetDispatcherCacheField("GeneratedRequestInvokers"));
    }

    /// <summary>
    ///     通过反射读取 registrar 的静态缓存字段。
    /// </summary>
    private static object GetRegistrarCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing registrar cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Registrar cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     通过反射读取 dispatcher 的静态缓存字段。
    /// </summary>
    private static object GetDispatcherCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing dispatcher cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Dispatcher cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     清空目标缓存实例。
    /// </summary>
    private static void ClearCache(object cache)
    {
        _ = cache.GetType()
            .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(cache, Array.Empty<object>());
    }

}

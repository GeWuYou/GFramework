using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;
using GFramework.Cqrs;
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

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Extensions;
using GFramework.Cqrs.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 Architecture 在 <c>ConfigureServices</c> 阶段暴露出的模块安装与 CQRS 行为接入能力。
///     这些测试覆盖模块安装回调、请求/流式管道行为接入与自定义 notification publisher 复用，
///     确保显式安装 <see cref="CqrsRuntimeModule" /> 后模块管理器保持可观察行为不变。
/// </summary>
[NonParallelizable]
[TestFixture]
public class ArchitectureModulesBehaviorTests
{
    /// <summary>
    ///     初始化全局上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
        AdditionalAssemblyNotificationHandlerState.Reset();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
        TrackingStreamPipelineBehavior<ModuleStreamBehaviorRequest, int>.InvocationCount = 0;
    }

    /// <summary>
    ///     清理测试过程中写入的全局上下文状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        AdditionalAssemblyNotificationHandlerState.Reset();
        GameContext.Clear();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
        TrackingStreamPipelineBehavior<ModuleStreamBehaviorRequest, int>.InvocationCount = 0;
    }

    /// <summary>
    ///     验证安装模块时会把当前架构实例传给模块，并允许模块在安装阶段注册组件。
    /// </summary>
    [Test]
    public async Task InstallModule_Should_Invoke_Module_Install_With_Current_Architecture()
    {
        var module = new TrackingArchitectureModule();
        var architecture = new ModuleTestArchitecture(target => target.InstallModule(module));

        await architecture.InitializeAsync();
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(module.InstalledArchitecture, Is.SameAs(architecture));
                Assert.That(module.InstallCallCount, Is.EqualTo(1));
                Assert.That(architecture.Context.GetUtility<InstalledByModuleUtility>(), Is.Not.Null);
            });
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     验证在配置阶段接入的 CQRS 请求行为会参与请求管道执行。
    /// </summary>
    [Test]
    public async Task AddCqrsPipelineBehavior_Should_Apply_Pipeline_Behavior_To_Request()
    {
        var architecture = new CqrsWiringArchitecture(services =>
        {
            services.AddCqrsPipelineBehavior<TrackingPipelineBehavior<ModuleBehaviorRequest, string>>();
            services.AddCqrsHandlersFromAssembly(typeof(ModuleBehaviorRequest).Assembly);
        });

        await architecture.InitializeAsync();
        try
        {
            var response = await architecture.Context.SendRequestAsync(new ModuleBehaviorRequest());

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo("handled"));
                Assert.That(TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount, Is.EqualTo(1));
            });
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     验证在配置阶段接入的 CQRS stream 行为会参与建流处理流程。
    /// </summary>
    [Test]
    public async Task AddCqrsStreamPipelineBehavior_Should_Apply_Pipeline_Behavior_To_Stream_Request()
    {
        var architecture = new CqrsWiringArchitecture(services =>
        {
            services.AddCqrsStreamPipelineBehavior<TrackingStreamPipelineBehavior<ModuleStreamBehaviorRequest, int>>();
            services.AddCqrsHandlersFromAssembly(typeof(ModuleBehaviorRequest).Assembly);
        });

        await architecture.InitializeAsync();
        try
        {
            var response = await DrainAsync(architecture.Context.CreateStream(new ModuleStreamBehaviorRequest()));

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo([7]));
                Assert.That(
                    TrackingStreamPipelineBehavior<ModuleStreamBehaviorRequest, int>.InvocationCount,
                    Is.EqualTo(1));
            });
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     验证标准架构启动路径会复用通过 <see cref="Architecture.Configurator" /> 声明的自定义 notification publisher，
    ///     而不是在 <see cref="CqrsRuntimeModule" /> 创建 runtime 时提前固化默认顺序策略。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_Reuse_Custom_NotificationPublisher_From_Configurator()
    {
        var generatedAssembly = CreateGeneratedHandlerAssembly();
        var architecture = new ConfiguredNotificationPublisherArchitecture(generatedAssembly.Object);

        await architecture.InitializeAsync();
        try
        {
            var probe = architecture.Context.GetService<ArchitectureNotificationPublisherProbe>();

            await architecture.Context.PublishAsync(new AdditionalAssemblyNotification());

            Assert.Multiple(() =>
            {
                Assert.That(probe.WasCalled, Is.True);
                Assert.That(AdditionalAssemblyNotificationHandlerState.InvocationCount, Is.EqualTo(1));
            });
        }
        finally
        {
            await architecture.DestroyAsync();
        }
    }

    /// <summary>
    ///     用于测试模块行为的最小架构实现。
    /// </summary>
    private sealed class ModuleTestArchitecture(Action<ModuleTestArchitecture> registrationAction) : Architecture
    {
        /// <summary>
        ///     在初始化阶段执行测试注入的模块注册逻辑。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            registrationAction(this);
        }
    }

    /// <summary>
    ///     显式接线 CQRS runtime 与测试声明行为的架构封装。
    ///     行为与 handler 注册统一走标准 <see cref="IServiceCollection" /> 组合根入口。
    /// </summary>
    private sealed class CqrsWiringArchitecture(Action<IServiceCollection> configureCqrs) : Architecture
    {
        /// <summary>
        ///     在服务配置阶段安装默认 runtime，并执行测试注入的 CQRS 行为接线。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddCqrsRuntime();
            configureCqrs(services);
        }
    }

    /// <summary>
    ///     通过标准架构启动路径声明自定义 notification publisher 的最小架构。
    /// </summary>
    private sealed class ConfiguredNotificationPublisherArchitecture(Assembly generatedAssembly) : Architecture
    {
        /// <summary>
        ///     在服务钩子阶段注册 probe 与自定义 publisher，
        ///     以模拟真实项目在组合根里通过 <see cref="IServiceCollection" /> 覆盖默认策略的路径。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => services =>
        {
            services.AddCqrsRuntime();
            services.AddSingleton<ArchitectureNotificationPublisherProbe>();
            services.AddSingleton<INotificationPublisher, ArchitectureTrackingNotificationPublisher>();
        };

        /// <summary>
        ///     在用户初始化阶段显式接入额外程序集里的 notification handler，
        ///     让测试聚焦“publisher 是否被复用”，而不是依赖当前测试文件自己的 handler 扫描形状。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddCqrsHandlersFromAssembly(generatedAssembly);
        }
    }

    /// <summary>
    ///     记录模块安装调用情况的测试模块。
    /// </summary>
    private sealed class TrackingArchitectureModule : IArchitectureModule
    {
        /// <summary>
        ///     获取模块安装调用次数。
        /// </summary>
        public int InstallCallCount { get; private set; }

        /// <summary>
        ///     获取最近一次接收到的架构实例。
        /// </summary>
        public IArchitecture? InstalledArchitecture { get; private set; }

        /// <summary>
        ///     记录安装调用，并在安装阶段注册一个工具验证调用链可用。
        /// </summary>
        /// <param name="architecture">目标架构实例。</param>
        public void Install(IArchitecture architecture)
        {
            InstallCallCount++;
            InstalledArchitecture = architecture;
            architecture.RegisterUtility(new InstalledByModuleUtility());
        }
    }

    /// <summary>
    ///     由测试模块安装时注册的简单工具。
    /// </summary>
    private sealed class InstalledByModuleUtility : IUtility
    {
    }

    /// <summary>
    ///     创建一个仅暴露程序集级 CQRS registry 元数据的 mocked Assembly。
    ///     该测试替身模拟扩展程序集已经提供 notification handler registry，而架构只需在初始化时显式接入该程序集。
    /// </summary>
    /// <returns>包含程序集级 notification handler registry 元数据的 mocked Assembly。</returns>
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
    ///     记录自定义 notification publisher 是否真正参与了标准架构启动路径下的 publish 调用。
    /// </summary>
    private sealed class ArchitectureNotificationPublisherProbe
    {
        /// <summary>
        ///     获取 probe 是否已被 publisher 标记为执行过。
        /// </summary>
        public bool WasCalled { get; private set; }

        /// <summary>
        ///     记录当前 publish 调用已经命中了自定义 publisher。
        /// </summary>
        public void MarkCalled()
        {
            WasCalled = true;
        }
    }

    /// <summary>
    ///     依赖容器内 probe 的自定义 notification publisher。
    ///     该类型通过显式标记 + 正常转发处理器执行，验证标准架构启动路径不会把自定义策略短路成默认顺序发布器。
    /// </summary>
    private sealed class ArchitectureTrackingNotificationPublisher(
        ArchitectureNotificationPublisherProbe probe) : INotificationPublisher
    {
        /// <summary>
        ///     记录自定义 publisher 已参与当前发布调用，并继续按处理器解析顺序转发执行。
        /// </summary>
        public async ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            probe.MarkCalled();

            foreach (var handler in context.Handlers)
            {
                await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     物化异步流为只读列表，便于断言 stream pipeline 行为的最终可观察结果。
    /// </summary>
    /// <typeparam name="T">流元素类型。</typeparam>
    /// <param name="stream">要物化的异步流。</param>
    /// <returns>按枚举顺序收集的元素列表。</returns>
    private static async Task<IReadOnlyList<T>> DrainAsync<T>(IAsyncEnumerable<T> stream)
    {
        var results = new List<T>();

        await foreach (var item in stream.ConfigureAwait(false))
        {
            results.Add(item);
        }

        return results;
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 Architecture 通过 <c>ArchitectureModules</c> 暴露出的模块安装与 CQRS 行为注册能力。
///     这些测试覆盖模块安装回调和请求管道行为接入，确保模块管理器仍然保持可观察行为不变。
/// </summary>
[NonParallelizable]
[TestFixture]
public class ArchitectureModulesBehaviorTests
{
    /// <summary>
    ///     初始化日志工厂和全局上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
    }

    /// <summary>
    ///     清理测试过程中写入的全局上下文状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
        LegacyBridgePipelineTracker.Reset();
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
    ///     验证注册的 CQRS 行为会参与请求管道执行。
    /// </summary>
    [Test]
    public async Task RegisterCqrsPipelineBehavior_Should_Apply_Pipeline_Behavior_To_Request()
    {
        var architecture = new ModuleTestArchitecture(target =>
            target.RegisterCqrsPipelineBehavior<TrackingPipelineBehavior<ModuleBehaviorRequest, string>>());

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
    ///     验证默认架构初始化路径会自动扫描 Core 程序集里的 legacy bridge handler，
    ///     使旧 <c>SendCommand</c> / <c>SendQuery</c> 入口也能进入统一 CQRS pipeline。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_AutoRegister_LegacyBridgeHandlers_For_Default_Core_Assemblies()
    {
        LegacyBridgePipelineTracker.Reset();
        var architecture = new LegacyBridgeArchitecture();

        await architecture.InitializeAsync();
        try
        {
            var query = new LegacyArchitectureBridgeQuery();
            var command = new LegacyArchitectureBridgeCommand();

            var queryResult = architecture.Context.SendQuery(query);
            architecture.Context.SendCommand(command);

            Assert.Multiple(() =>
            {
                Assert.That(queryResult, Is.EqualTo(24));
                Assert.That(query.ObservedContext, Is.SameAs(architecture.Context));
                Assert.That(command.Executed, Is.True);
                Assert.That(command.ObservedContext, Is.SameAs(architecture.Context));
                Assert.That(LegacyBridgePipelineTracker.InvocationCount, Is.EqualTo(2));
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
        protected override void OnInitialize()
        {
            registrationAction(this);
        }
    }

    /// <summary>
    ///     通过公开初始化入口注册测试 pipeline behavior 的最小架构，
    ///     用于验证默认 Core 程序集扫描是否会自动接入 legacy bridge handler。
    /// </summary>
    private sealed class LegacyBridgeArchitecture : Architecture
    {
        /// <summary>
        ///     在容器钩子阶段注册 open-generic pipeline behavior，
        ///     以便 bridge request 走真实的架构初始化与 handler 自动扫描链路。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => services =>
            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LegacyBridgeTrackingPipelineBehavior<,>));

        /// <summary>
        ///     保持空初始化，让测试只聚焦默认 CQRS 接线与 legacy bridge handler 自动发现。
        /// </summary>
        protected override void OnInitialize()
        {
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
}

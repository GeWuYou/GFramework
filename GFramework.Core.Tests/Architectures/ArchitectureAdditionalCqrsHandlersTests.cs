// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证架构初始化阶段可以通过标准组合根入口显式接入默认程序集之外的 CQRS handlers。
/// </summary>
[TestFixture]
public sealed class ArchitectureAdditionalCqrsHandlersTests
{
    /// <summary>
    ///     初始化共享测试状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
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
    }

    /// <summary>
    ///     验证显式声明的额外程序集会在初始化阶段接入当前架构容器。
    /// </summary>
    /// <returns>The asynchronous test task.</returns>
    [Test]
    public async Task AddCqrsHandlersFromAssembly_Should_Register_Handlers_From_Explicit_Assembly()
    {
        var generatedAssembly = CreateGeneratedHandlerAssembly();
        var architecture = CreateArchitecture(services =>
            services.AddCqrsHandlersFromAssembly(generatedAssembly.Object));

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
    public async Task AddCqrsHandlersFromAssembly_Should_Deduplicate_Repeated_Assembly_Registration()
    {
        var generatedAssemblyA = CreateGeneratedHandlerAssembly();
        var generatedAssemblyB = CreateGeneratedHandlerAssembly();
        var architecture = CreateArchitecture(services =>
        {
            services.AddCqrsHandlersFromAssembly(generatedAssemblyA.Object);
            services.AddCqrsHandlersFromAssemblies([generatedAssemblyB.Object]);
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
    ///     创建在配置阶段显式安装 CQRS runtime 并接入测试声明的 handler 程序集的测试架构。
    /// </summary>
    /// <param name="configureHandlers">配置阶段执行的额外 CQRS 程序集接入逻辑。</param>
    /// <returns>已接线 runtime 与额外 handler 注册的测试架构实例。</returns>
    private static HandlerWiringArchitecture CreateArchitecture(Action<IServiceCollection> configureHandlers)
    {
        return new HandlerWiringArchitecture(configureHandlers);
    }

    /// <summary>
    ///     通过标准组合根入口安装 CQRS runtime 的最小测试架构。
    /// </summary>
    private sealed class HandlerWiringArchitecture(Action<IServiceCollection> configureHandlers) : Architecture
    {
        /// <summary>
        ///     在组合根阶段安装默认 CQRS runtime。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => services =>
            services.AddCqrsRuntime();

        /// <summary>
        ///     在用户配置阶段接入测试声明的 handler 程序集。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            configureHandlers(services);
        }
    }
}

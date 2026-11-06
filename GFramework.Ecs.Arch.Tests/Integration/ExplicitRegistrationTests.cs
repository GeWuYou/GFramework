// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Components;
using GFramework.Ecs.Arch.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Tests.Integration;

/// <summary>
///     验证配置期声明、单实例 World 所有权和两个架构的 ECS 隔离。
/// </summary>
[TestFixture]
public class ExplicitRegistrationTests
{
    /// <summary>
    ///     配置完成后，模块和 World 可由同一实例上下文解析。
    /// </summary>
    /// <returns>测试生命周期任务。</returns>
    [Test]
    public async Task ArchEcsModule_Should_Be_Explicitly_Registered()
    {
        await using var architecture = new TestArchitecture(target => target.UseArch());
        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(architecture.Context.GetService<World>(), Is.Not.Null);
            Assert.That(architecture.Context.GetService<IArchEcsModule>(), Is.Not.Null);
            Assert.That(architecture.Context.GetService<World>(),
                Is.SameAs(architecture.Context.GetService<World>()));
        });
    }

    /// <summary>
    ///     配置委托在本架构的配置阶段执行。
    /// </summary>
    /// <returns>测试生命周期任务。</returns>
    [Test]
    public async Task UseArch_Should_Accept_Configuration()
    {
        var configCalled = false;
        await using var architecture = new TestArchitecture(target => target.UseArch(options =>
        {
            options.WorldCapacity = 2000;
            options.EnableStatistics = true;
            configCalled = true;
        }));

        Assert.That(configCalled, Is.False);
        await architecture.InitializeAsync();
        Assert.That(configCalled, Is.True);
    }

    /// <summary>
    ///     扩展仍返回当前实例以支持配置期链式声明。
    /// </summary>
    /// <returns>测试生命周期任务。</returns>
    [Test]
    public async Task UseArch_Should_Support_Chaining()
    {
        await using var architecture = new TestArchitecture(target =>
        {
            var returned = target.UseArch().UseArch(options => options.WorldCapacity = 2000);
            Assert.That(returned, Is.SameAs(target));
        });

        await architecture.InitializeAsync();
        Assert.That(architecture.Context.GetService<World>(), Is.Not.Null);
    }

    /// <summary>
    ///     关闭一个实例不会清空另一实例的 World 或服务模块。
    /// </summary>
    /// <returns>两个独立架构的测试生命周期任务。</returns>
    [Test]
    public async Task UseArch_Should_Isolate_Worlds_Between_Architecture_Instances()
    {
        await using var first = new TestArchitecture(target => target.UseArch());
        await using var second = new TestArchitecture(target => target.UseArch());
        await first.InitializeAsync();
        await second.InitializeAsync();
        var firstWorld = first.Context.GetService<World>();
        var secondWorld = second.Context.GetService<World>();

        Assert.That(firstWorld, Is.Not.SameAs(secondWorld));
        await first.DestroyAsync();
        var entity = secondWorld.Create(new Position(1, 2));

        Assert.Multiple(() =>
        {
            Assert.That(secondWorld.IsAlive(entity), Is.True);
            Assert.That(second.Context.GetService<World>(), Is.SameAs(secondWorld));
        });
    }

    /// <summary>
    ///     已就绪架构不能再改变模块注册图。
    /// </summary>
    /// <returns>测试生命周期任务。</returns>
    [Test]
    public async Task UseArch_Should_Reject_Runtime_Registration()
    {
        await using var architecture = new TestArchitecture();
        await architecture.InitializeAsync();

        Assert.Throws<InvalidOperationException>(() => architecture.UseArch());
    }

    /// <summary>
    ///     注册模块不预先创建 World；正式 provider 只按一个 canonical 描述创建它。
    /// </summary>
    [Test]
    public void Register_Should_Defer_World_Creation_Until_Provider_Resolution()
    {
        using var container = new MicrosoftDiContainer();
        var module = new ArchEcsModule();
        module.Register(container);

        Assert.That(container.Get<World>(), Is.Null);
        container.Freeze();
        var world = container.GetRequired<World>();
        Assert.That(container.GetRequired<World>(), Is.SameAs(world));
    }

    /// <summary>
    ///     部分系统初始化失败后仍停止先前成功的系统；资源随后由 provider 负责释放。
    /// </summary>
    /// <returns>模块失败清理任务。</returns>
    [Test]
    public async Task DestroyAsync_Should_Stop_Started_Systems_After_Partial_Initialization_Failure()
    {
        using var container = new MicrosoftDiContainer();
        container.Register<ILoggerFactoryProvider>(new ConsoleLoggerFactoryProvider());
        ((IContextAware)container).SetContext(new ArchitectureContext(container));
        var started = new StartedSystem();
        var failing = new FailingSystem();
        container.Register<ArchSystemAdapter<float>>(started);
        container.Register<ArchSystemAdapter<float>>(failing);
        var module = new ArchEcsModule();
        module.Register(container);
        container.Freeze();

        Assert.Throws<InvalidOperationException>(() => module.Initialize());
        await module.DestroyAsync();
        await module.DestroyAsync();

        Assert.That(started.StopCount, Is.EqualTo(1));
        ((IDisposable)started).Dispose();
        ((IDisposable)failing).Dispose();
    }

    /// <summary>
    ///     在框架配置阶段声明测试所需的服务模块。
    /// </summary>
    /// <param name="configure">模块配置动作。</param>
    private sealed class TestArchitecture(Action<TestArchitecture>? configure = null) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            configure?.Invoke(this);
        }
    }

    /// <summary>
    ///     记录已经启动系统的业务停止次数。
    /// </summary>
    private sealed class StartedSystem : ArchSystemAdapter<float>
    {
        /// <summary>
        ///     获取业务停止次数。
        /// </summary>
        public int StopCount { get; private set; }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopCount++;
        }
    }

    /// <summary>
    ///     模拟系统初始化中途失败。
    /// </summary>
    private sealed class FailingSystem : ArchSystemAdapter<float>
    {
        /// <inheritdoc />
        protected override void OnArchInitialize()
        {
            throw new InvalidOperationException("Expected ECS initialization failure.");
        }
    }
}

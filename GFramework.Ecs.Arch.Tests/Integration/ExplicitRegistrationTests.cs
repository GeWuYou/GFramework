using Arch.Core;
using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;

namespace GFramework.Ecs.Arch.Tests.Integration;

/// <summary>
/// 显式注册集成测试
/// </summary>
[TestFixture]
public class ExplicitRegistrationTests
{
    [SetUp]
    public void Setup()
    {
        _container = new MicrosoftDiContainer();
        _context = new ArchitectureContext(_container);

        // 清空注册表，确保测试隔离
        ArchitectureModuleRegistry.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        _container?.Clear();
        _context = null;
        ArchitectureModuleRegistry.Clear();
        GameContext.Clear();
    }

    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;

    /// <summary>
    /// 测试 Arch ECS 模块显式注册
    /// </summary>
    [Test]
    public void ArchEcsModule_Should_Be_Explicitly_Registered()
    {
        // Arrange
        var architecture = new TestArchitecture();

        // Act - 显式注册
        architecture.UseArch();
        architecture.Initialize();

        // Assert - 验证 World 已注册（由 ArchEcsModule 注册）
        var world = architecture.Context.GetService<World>();
        Assert.That(world, Is.Not.Null, "World should be registered by ArchEcsModule");

        // 验证 IArchEcsModule 已注册
        var ecsModule = architecture.Context.GetService<IArchEcsModule>();
        Assert.That(ecsModule, Is.Not.Null, "IArchEcsModule should be registered");
    }

    /// <summary>
    /// 测试 World 是否正确注册到容器
    /// </summary>
    [Test]
    public void World_Should_Be_Registered_In_Container()
    {
        // Arrange
        var architecture = new TestArchitecture();

        // Act - 显式注册
        architecture.UseArch();
        architecture.Initialize();

        // Assert
        var world = architecture.Context.GetService<World>();
        Assert.That(world, Is.Not.Null, "World should be registered in container");
    }

    /// <summary>
    /// 测试带配置的注册
    /// </summary>
    [Test]
    public void UseArch_Should_Accept_Configuration()
    {
        // Arrange
        var architecture = new TestArchitecture();
        var configCalled = false;

        // Act
        architecture.UseArch(options =>
        {
            options.WorldCapacity = 2000;
            options.EnableStatistics = true;
            configCalled = true;
        });

        // Assert
        Assert.That(configCalled, Is.True, "Configuration delegate should be called");
    }

    /// <summary>
    /// 测试链式调用
    /// </summary>
    [Test]
    public void UseArch_Should_Support_Chaining()
    {
        // Arrange & Act
        var architecture = new TestArchitecture()
            .UseArch()
            .UseArch(options => options.WorldCapacity = 2000);

        architecture.Initialize();

        // Assert - 验证模块已注册
        var world = architecture.Context.GetService<World>();
        Assert.That(world, Is.Not.Null, "World should be registered");

        var ecsModule = architecture.Context.GetService<IArchEcsModule>();
        Assert.That(ecsModule, Is.Not.Null, "IArchEcsModule should be registered");
    }

    /// <summary>
    /// 测试架构类，用于测试
    /// </summary>
    private class TestArchitecture : Architecture
    {
        public TestArchitecture() : base(new ArchitectureConfiguration())
        {
        }

        protected override void OnInitialize()
        {
            // 测试架构，无需额外初始化
        }
    }
}
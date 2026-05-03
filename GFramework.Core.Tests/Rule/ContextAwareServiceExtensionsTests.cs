// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Rule;
using GFramework.Core.Tests.Architectures;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     测试 ContextAwareServiceExtensions 的单元测试类
///     验证服务、系统、模型、工具的单例和批量获取功能
/// </summary>
[TestFixture]
public class ContextAwareServiceExtensionsTests
{
    private MicrosoftDiContainer _container = null!;
    private ArchitectureContext _context = null!;

    private TestContextAware _contextAware = null!;

    [SetUp]
    public void SetUp()
    {
        _container = new MicrosoftDiContainer();
        _context = new ArchitectureContext(_container);
        _contextAware = new TestContextAware();

        ((IContextAware)_contextAware).SetContext(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _container.Clear();
    }

    [Test]
    public void GetService_Should_Return_Registered_Service()
    {
        // Arrange
        var service = new TestService();
        _container.Register(service);
        _container.Freeze();

        // Act
        var result = _contextAware.GetService<TestService>();

        // Assert
        Assert.That(result, Is.SameAs(service));
    }

    [Test]
    public void GetService_Should_Throw_When_Context_Returns_Null_Service()
    {
        // Arrange
        var contextAware = new TestContextAware();
        ((IContextAware)contextAware).SetContext(new TestArchitectureContextV3());

        // Act / Assert
        Assert.That(() => contextAware.GetService<TestService>(),
            Throws.InvalidOperationException.With.Message.Contains("Service"));
    }

    [Test]
    public void GetSystem_Should_Return_Registered_System()
    {
        // Arrange
        var system = new TestSystem();
        _container.RegisterSystem(system);
        _container.Freeze();

        // Act
        var result = _contextAware.GetSystem<TestSystem>();

        // Assert
        Assert.That(result, Is.SameAs(system));
    }

    [Test]
    public void GetSystem_Should_Throw_When_Context_Returns_Null_System()
    {
        // Arrange
        var contextAware = new TestContextAware();
        ((IContextAware)contextAware).SetContext(new TestArchitectureContextV3());

        // Act / Assert
        Assert.That(() => contextAware.GetSystem<TestSystem>(),
            Throws.InvalidOperationException.With.Message.Contains("System"));
    }

    [Test]
    public void GetModel_Should_Return_Registered_Model()
    {
        // Arrange
        var model = new TestModel();
        _container.Register(model);
        _container.Freeze();

        // Act
        var result = _contextAware.GetModel<TestModel>();

        // Assert
        Assert.That(result, Is.SameAs(model));
    }

    [Test]
    public void GetModel_Should_Throw_When_Context_Returns_Null_Model()
    {
        // Arrange
        var contextAware = new TestContextAware();
        ((IContextAware)contextAware).SetContext(new TestArchitectureContextV3());

        // Act / Assert
        Assert.That(() => contextAware.GetModel<TestModel>(),
            Throws.InvalidOperationException.With.Message.Contains("Model"));
    }

    [Test]
    public void GetUtility_Should_Return_Registered_Utility()
    {
        // Arrange
        var utility = new TestUtility();
        _container.Register(utility);
        _container.Freeze();

        // Act
        var result = _contextAware.GetUtility<TestUtility>();

        // Assert
        Assert.That(result, Is.SameAs(utility));
    }

    [Test]
    public void GetUtility_Should_Throw_When_Context_Returns_Null_Utility()
    {
        // Arrange
        var contextAware = new TestContextAware();
        ((IContextAware)contextAware).SetContext(new TestArchitectureContextV3());

        // Act / Assert
        Assert.That(() => contextAware.GetUtility<TestUtility>(),
            Throws.InvalidOperationException.With.Message.Contains("Utility"));
    }

    [Test]
    public void GetServices_Should_Return_All_Registered_Services()
    {
        // Arrange
        var service1 = new TestService { Name = "Service1" };
        var service2 = new TestService { Name = "Service2" };
        _container.Register<TestService>(service1);
        _container.Register<TestService>(service2);
        _container.Freeze();

        // Act
        var results = _contextAware.GetServices<TestService>();

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Contains.Item(service1));
        Assert.That(results, Contains.Item(service2));
    }

    [Test]
    public void GetSystems_Should_Return_All_Registered_Systems()
    {
        // Arrange
        var system1 = new TestSystem { Name = "System1" };
        var system2 = new TestSystem { Name = "System2" };
        _container.RegisterSystem(system1);
        _container.RegisterSystem(system2);
        _container.Freeze();

        // Act
        var results = _contextAware.GetSystems<ISystem>();

        // Assert
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(results.Any(s => s is TestSystem ts && string.Equals(ts.Name, "System1", System.StringComparison.Ordinal)), Is.True);
        Assert.That(results.Any(s => s is TestSystem ts && string.Equals(ts.Name, "System2", System.StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void GetModels_Should_Return_All_Registered_Models()
    {
        // Arrange
        var model1 = new TestModel { Name = "Model1" };
        var model2 = new TestModel { Name = "Model2" };
        _container.Register(model1);
        _container.Register(model2);
        _container.Freeze();

        // Act
        var results = _contextAware.GetModels<TestModel>();

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Any(m => string.Equals(m.Name, "Model1", System.StringComparison.Ordinal)), Is.True);
        Assert.That(results.Any(m => string.Equals(m.Name, "Model2", System.StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void GetUtilities_Should_Return_All_Registered_Utilities()
    {
        // Arrange
        var utility1 = new TestUtility { Name = "Utility1" };
        var utility2 = new TestUtility { Name = "Utility2" };
        _container.Register(utility1);
        _container.Register(utility2);
        _container.Freeze();

        // Act
        var results = _contextAware.GetUtilities<TestUtility>();

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Any(u => string.Equals(u.Name, "Utility1", System.StringComparison.Ordinal)), Is.True);
        Assert.That(results.Any(u => string.Equals(u.Name, "Utility2", System.StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void GetServices_Should_Return_Empty_List_When_No_Services_Registered()
    {
        // Arrange
        _container.Freeze();

        // Act
        var results = _contextAware.GetServices<TestService>();

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void GetSystems_Should_Return_Empty_List_When_No_Systems_Registered()
    {
        // Arrange
        _container.Freeze();

        // Act
        var results = _contextAware.GetSystems<TestSystem>();

        // Assert
        Assert.That(results, Is.Empty);
    }

    private class TestService
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestSystem : ISystem
    {
        public string Name { get; set; } = string.Empty;

        public void SetContext(IArchitectureContext context)
        {
        }

        public IArchitectureContext GetContext() => null!;

        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        public void Initialize()
        {
        }

        public void Destroy()
        {
        }
    }

    private class TestModel : IModel
    {
        public string Name { get; set; } = string.Empty;

        public void SetContext(IArchitectureContext context)
        {
        }

        public IArchitectureContext GetContext() => null!;

        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        public void Initialize()
        {
        }
    }

    private class TestUtility : IUtility
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestContextAware : ContextAwareBase
    {
    }
}

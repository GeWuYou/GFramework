using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Extensions;
using GFramework.Core.Ioc;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     测试 ContextAwareServiceExtensions 的单元测试类
///     验证服务、系统、模型、工具的单例和批量获取功能
/// </summary>
[TestFixture]
public class ContextAwareServiceExtensionsTests
{
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

    private TestContextAware _contextAware = null!;
    private ArchitectureContext _context = null!;
    private MicrosoftDiContainer _container = null!;

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
        Assert.That(results.Any(s => s is TestSystem ts && ts.Name == "System1"), Is.True);
        Assert.That(results.Any(s => s is TestSystem ts && ts.Name == "System2"), Is.True);
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
        Assert.That(results.Any(m => m.Name == "Model1"), Is.True);
        Assert.That(results.Any(m => m.Name == "Model2"), Is.True);
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
        Assert.That(results.Any(u => u.Name == "Utility1"), Is.True);
        Assert.That(results.Any(u => u.Name == "Utility2"), Is.True);
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
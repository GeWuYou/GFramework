using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;
using GFramework.Core.extensions;
using GFramework.Core.ioc;
using GFramework.Core.rule;
using NUnit.Framework;

namespace GFramework.Core.Tests.rule;

/// <summary>
///     测试 ContextAwareEnvironmentExtensions 的单元测试类
///     验证环境对象的获取功能
/// </summary>
[TestFixture]
public class ContextAwareEnvironmentExtensionsTests
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
    public void GetEnvironment_Should_Return_Registered_Environment()
    {
        // Arrange
        var environment = new TestEnvironment();
        _container.Register<IEnvironment>(environment);
        _container.Freeze();

        // Act
        var result = _contextAware.GetEnvironment();

        // Assert
        Assert.That(result, Is.SameAs(environment));
    }

    [Test]
    public void GetEnvironment_Generic_Should_Return_Typed_Environment()
    {
        // Arrange
        var environment = new TestEnvironment { Name = "TestEnv" };
        _container.Register<IEnvironment>(environment);
        _container.Freeze();

        // Act
        var result = _contextAware.GetEnvironment<TestEnvironment>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(environment));
        Assert.That(result!.Name, Is.EqualTo("TestEnv"));
    }

    [Test]
    public void GetEnvironment_Generic_Should_Return_Null_When_Type_Mismatch()
    {
        // Arrange
        var environment = new TestEnvironment();
        _container.Register<IEnvironment>(environment);
        _container.Freeze();

        // Act
        var result = _contextAware.GetEnvironment<AnotherEnvironment>();

        // Assert
        Assert.That(result, Is.Null);
    }

    private class TestEnvironment : IEnvironment
    {
        public string Name { get; set; } = string.Empty;

        public T? Get<T>(string key) where T : class => default;

        public bool TryGet<T>(string key, out T value) where T : class
        {
            value = default!;
            return false;
        }

        public T GetRequired<T>(string key) where T : class => throw new NotImplementedException();

        public void Register(string key, object value)
        {
        }

        public void Initialize()
        {
        }
    }

    private class AnotherEnvironment : IEnvironment
    {
        public string Name => "Another";
        public T? Get<T>(string key) where T : class => default;

        public bool TryGet<T>(string key, out T value) where T : class
        {
            value = default!;
            return false;
        }

        public T GetRequired<T>(string key) where T : class => throw new NotImplementedException();

        public void Register(string key, object value)
        {
        }

        public void Initialize()
        {
        }
    }

    private class TestContextAware : ContextAwareBase
    {
    }
}
using GFramework.Core.Abstractions.environment;
using GFramework.Core.environment;
using NUnit.Framework;

namespace GFramework.Core.Tests.environment;

[TestFixture]
public class EnvironmentTests
{
    [SetUp]
    public void SetUp()
    {
        _environment = new TestEnvironment();
        _environment.Initialize();
    }

    private TestEnvironment _environment = null!;

    [Test]
    public void DefaultEnvironment_Name_Should_ReturnDefault()
    {
        var env = new DefaultEnvironment();

        Assert.That(env.Name, Is.EqualTo("Default"));
    }

    [Test]
    public void DefaultEnvironment_Initialize_Should_NotThrow()
    {
        var env = new DefaultEnvironment();

        Assert.DoesNotThrow(() => env.Initialize());
    }

    [Test]
    public void Get_Should_Return_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.Get<string>("testKey");

        Assert.That(result, Is.EqualTo("testValue"));
    }

    [Test]
    public void Get_Should_ReturnNull_When_Key_Not_Exists()
    {
        var result = _environment.Get<string>("nonExistentKey");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Get_Should_ReturnNull_When_Type_Does_Not_Match()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.Get<List<int>>("testKey");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryGet_Should_ReturnTrue_And_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.TryGet<string>("testKey", out var value);

        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("testValue"));
    }

    [Test]
    public void TryGet_Should_ReturnFalse_When_Key_Not_Exists()
    {
        var result = _environment.TryGet<string>("nonExistentKey", out var value);

        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryGet_Should_ReturnFalse_When_Type_Does_Not_Match()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.TryGet<List<int>>("testKey", out var value);

        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void GetRequired_Should_Return_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.GetRequired<string>("testKey");

        Assert.That(result, Is.EqualTo("testValue"));
    }

    [Test]
    public void GetRequired_Should_ThrowInvalidOperationException_When_Key_Not_Exists()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _environment.GetRequired<string>("nonExistentKey"));
    }

    [Test]
    public void Register_Should_Add_Value_To_Dictionary()
    {
        _environment.Register("newKey", "newValue");

        var result = _environment.Get<string>("newKey");

        Assert.That(result, Is.EqualTo("newValue"));
    }

    [Test]
    public void Register_Should_Overwrite_Existing_Value()
    {
        _environment.Register("testKey", "value1");
        _environment.Register("testKey", "value2");

        var result = _environment.Get<string>("testKey");

        Assert.That(result, Is.EqualTo("value2"));
    }

    [Test]
    public void IEnvironment_Register_Should_Add_Value()
    {
        IEnvironment env = _environment;
        env.Register("interfaceKey", "interfaceValue");

        var result = env.Get<string>("interfaceKey");

        Assert.That(result, Is.EqualTo("interfaceValue"));
    }
}

public class TestEnvironment : EnvironmentBase
{
    public override string Name { get; } = "TestEnvironment";

    public new void Register(string key, object value)
    {
        base.Register(key, value);
    }

    public override void Initialize()
    {
    }
}
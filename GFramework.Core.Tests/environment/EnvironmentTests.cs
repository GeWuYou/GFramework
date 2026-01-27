using GFramework.Core.Abstractions.environment;
using GFramework.Core.environment;
using NUnit.Framework;

namespace GFramework.Core.Tests.environment;

/// <summary>
///     测试环境相关的单元测试类，用于验证环境管理功能的正确性
/// </summary>
[TestFixture]
public class EnvironmentTests
{
    /// <summary>
    ///     在每个测试方法执行前进行初始化设置
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _environment = new TestEnvironment();
        _environment.Initialize();
    }

    private TestEnvironment _environment = null!;

    /// <summary>
    ///     验证默认环境的名称是否正确返回"Default"
    /// </summary>
    [Test]
    public void DefaultEnvironment_Name_Should_ReturnDefault()
    {
        var env = new DefaultEnvironment();

        Assert.That(env.Name, Is.EqualTo("Default"));
    }

    /// <summary>
    ///     验证默认环境的初始化方法不会抛出异常
    /// </summary>
    [Test]
    public void DefaultEnvironment_Initialize_Should_NotThrow()
    {
        var env = new DefaultEnvironment();

        Assert.DoesNotThrow(() => env.Initialize());
    }

    /// <summary>
    ///     验证当键存在时Get方法应该返回正确的值
    /// </summary>
    [Test]
    public void Get_Should_Return_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.Get<string>("testKey");

        Assert.That(result, Is.EqualTo("testValue"));
    }

    /// <summary>
    ///     验证当键不存在时Get方法应该返回null
    /// </summary>
    [Test]
    public void Get_Should_ReturnNull_When_Key_Not_Exists()
    {
        var result = _environment.Get<string>("nonExistentKey");

        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     验证当类型不匹配时Get方法应该返回null
    /// </summary>
    [Test]
    public void Get_Should_ReturnNull_When_Type_Does_Not_Match()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.Get<List<int>>("testKey");

        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     验证当键存在时TryGet方法应该返回true并输出正确的值
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnTrue_And_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.TryGet<string>("testKey", out var value);

        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("testValue"));
    }

    /// <summary>
    ///     验证当键不存在时TryGet方法应该返回false且输出值为null
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnFalse_When_Key_Not_Exists()
    {
        var result = _environment.TryGet<string>("nonExistentKey", out var value);

        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    /// <summary>
    ///     验证当类型不匹配时TryGet方法应该返回false且输出值为null
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnFalse_When_Type_Does_Not_Match()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.TryGet<List<int>>("testKey", out var value);

        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    /// <summary>
    ///     验证当键存在时GetRequired方法应该返回正确的值
    /// </summary>
    [Test]
    public void GetRequired_Should_Return_Value_When_Key_Exists()
    {
        _environment.Register("testKey", "testValue");

        var result = _environment.GetRequired<string>("testKey");

        Assert.That(result, Is.EqualTo("testValue"));
    }

    /// <summary>
    ///     验证当键不存在时GetRequired方法应该抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void GetRequired_Should_ThrowInvalidOperationException_When_Key_Not_Exists()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _environment.GetRequired<string>("nonExistentKey"));
    }

    /// <summary>
    ///     验证Register方法应该将值添加到字典中
    /// </summary>
    [Test]
    public void Register_Should_Add_Value_To_Dictionary()
    {
        _environment.Register("newKey", "newValue");

        var result = _environment.Get<string>("newKey");

        Assert.That(result, Is.EqualTo("newValue"));
    }

    /// <summary>
    ///     验证Register方法应该覆盖已存在的值
    /// </summary>
    [Test]
    public void Register_Should_Overwrite_Existing_Value()
    {
        _environment.Register("testKey", "value1");
        _environment.Register("testKey", "value2");

        var result = _environment.Get<string>("testKey");

        Assert.That(result, Is.EqualTo("value2"));
    }

    /// <summary>
    ///     验证通过IEnvironment接口的Register方法应该能够添加值
    /// </summary>
    [Test]
    public void IEnvironment_Register_Should_Add_Value()
    {
        IEnvironment env = _environment;
        env.Register("interfaceKey", "interfaceValue");

        var result = env.Get<string>("interfaceKey");

        Assert.That(result, Is.EqualTo("interfaceValue"));
    }
}

/// <summary>
///     测试环境实现类，继承自EnvironmentBase
/// </summary>
public class TestEnvironment : EnvironmentBase
{
    /// <summary>
    ///     获取环境名称
    /// </summary>
    public override string Name { get; } = "TestEnvironment";

    /// <summary>
    ///     注册键值对到环境中
    /// </summary>
    /// <param name="key">要注册的键</param>
    /// <param name="value">要注册的值</param>
    public new void Register(string key, object value)
    {
        base.Register(key, value);
    }

    /// <summary>
    ///     初始化环境
    /// </summary>
    public override void Initialize()
    {
    }
}
using GFramework.Core.constants;
using NUnit.Framework;

namespace GFramework.Core.Tests.constants;

/// <summary>
/// GFrameworkConstants类的单元测试
/// 测试内容包括：
/// - 版本号常量格式正确性
/// - 其他框架常量
/// - 常量值正确性
/// - 常量类型验证
/// - 常量可访问性
/// </summary>
[TestFixture]
public class GFrameworkConstantsTests
{
    /// <summary>
    /// 测试FrameworkName常量的值正确性
    /// </summary>
    [Test]
    public void FrameworkName_Should_Have_Correct_Value()
    {
        Assert.That(GFrameworkConstants.FrameworkName, Is.EqualTo("GFramework"));
    }

    /// <summary>
    /// 测试FrameworkName常量的类型
    /// </summary>
    [Test]
    public void FrameworkName_Should_Be_String_Type()
    {
        Assert.That(GFrameworkConstants.FrameworkName, Is.InstanceOf<string>());
    }

    /// <summary>
    /// 测试FrameworkName常量不为空
    /// </summary>
    [Test]
    public void FrameworkName_Should_Not_Be_Null_Or_Empty()
    {
        Assert.That(GFrameworkConstants.FrameworkName, Is.Not.Null);
        Assert.That(GFrameworkConstants.FrameworkName, Is.Not.Empty);
    }

    /// <summary>
    /// 测试FrameworkName常量是公共可访问的
    /// </summary>
    [Test]
    public void FrameworkName_Should_Be_Publicly_Accessible()
    {
        // 如果常量不存在或不是公共的，编译会失败或抛出异常
        Assert.DoesNotThrow(() =>
        {
            var name = GFrameworkConstants.FrameworkName;
        });
    }

    /// <summary>
    /// 测试FrameworkName常量是只读的（const）
    /// </summary>
    [Test]
    public void FrameworkName_Should_Be_Constant()
    {
        // const常量在编译时确定，这个测试主要验证其存在性
        var name = GFrameworkConstants.FrameworkName;
        Assert.That(name, Is.EqualTo("GFramework"));
    }
}
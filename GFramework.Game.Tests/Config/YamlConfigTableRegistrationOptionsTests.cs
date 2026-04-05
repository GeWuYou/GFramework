using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置表注册选项会在构造阶段建立最小不变量，避免非法路径状态继续向后传播。
/// </summary>
[TestFixture]
public class YamlConfigTableRegistrationOptionsTests
{
    /// <summary>
    ///     验证构造函数会拒绝空的或仅空白字符的表名。
    /// </summary>
    /// <param name="tableName">待验证的表名。</param>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_Table_Name_Is_Null_Or_Whitespace(string? tableName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new YamlConfigTableRegistrationOptions<int, string>(
                tableName!,
                "monster",
                static config => config.Length));

        Assert.That(exception!.ParamName, Is.EqualTo("tableName"));
    }

    /// <summary>
    ///     验证构造函数会拒绝空的或仅空白字符的相对目录路径。
    /// </summary>
    /// <param name="relativePath">待验证的相对目录路径。</param>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_Relative_Path_Is_Null_Or_Whitespace(string? relativePath)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new YamlConfigTableRegistrationOptions<int, string>(
                "monster",
                relativePath!,
                static config => config.Length));

        Assert.That(exception!.ParamName, Is.EqualTo("relativePath"));
    }
}
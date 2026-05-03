// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证公开 YAML 文本序列化入口的换行与参数契约。
/// </summary>
[TestFixture]
public sealed class YamlConfigTextSerializerTests
{
    /// <summary>
    ///     验证序列化结果会稳定地以 LF 作为尾随换行，
    ///     避免不同宿主平台的行尾约定影响生成内容。
    /// </summary>
    [Test]
    public void Serialize_Should_Use_Trailing_Lf_Newline()
    {
        var yaml = YamlConfigTextSerializer.Serialize(new MonsterYamlStub
        {
            Id = 1,
            Name = "Slime"
        });

        Assert.Multiple(() =>
        {
            Assert.That(yaml, Does.Contain("id: 1"));
            Assert.That(yaml, Does.Contain("name: Slime"));
            Assert.That(yaml.EndsWith("\n", StringComparison.Ordinal), Is.True);
            Assert.That(yaml.EndsWith("\r\n", StringComparison.Ordinal), Is.False);
        });
    }

    /// <summary>
    ///     验证空对象引用会继续通过参数异常暴露给调用方。
    /// </summary>
    [Test]
    public void Serialize_Should_Throw_When_Value_Is_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            YamlConfigTextSerializer.Serialize<MonsterYamlStub>(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("value"));
    }

    /// <summary>
    ///     用于 YAML 序列化测试的最小配置对象。
    /// </summary>
    private sealed class MonsterYamlStub
    {
        /// <summary>
        ///     获取或设置配置标识。
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        ///     获取或设置配置名称。
        /// </summary>
        public string Name { get; init; } = string.Empty;
    }
}

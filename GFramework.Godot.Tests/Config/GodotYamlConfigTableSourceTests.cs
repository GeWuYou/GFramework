// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.Config;

namespace GFramework.Godot.Tests.Config;

/// <summary>
///     验证 Godot YAML 配置表来源描述会拒绝可能逃逸缓存根目录的不安全相对路径。
/// </summary>
[TestFixture]
public sealed class GodotYamlConfigTableSourceTests
{
    /// <summary>
    ///     验证配置目录路径必须保持为无根、无遍历段的安全相对路径。
    /// </summary>
    /// <param name="configRelativePath">待验证的配置目录路径。</param>
    [TestCase("../outside")]
    [TestCase(@"..\outside")]
    [TestCase("./monster")]
    [TestCase(@".\monster")]
    [TestCase("monster/../outside")]
    [TestCase(@"monster\..\outside")]
    [TestCase("monster/./child")]
    [TestCase(@"monster\.\child")]
    [TestCase("/monster")]
    [TestCase("C:/monster")]
    [TestCase(@"C:\monster")]
    [TestCase("res://monster")]
    [TestCase("user://monster")]
    [TestCase("schemas:bad/monster")]
    [TestCase(@"schemas:bad\monster")]
    public void Constructor_Should_Throw_When_Config_Relative_Path_Is_Not_Safe(string configRelativePath)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new GodotYamlConfigTableSource("monster", configRelativePath));

        Assert.That(exception!.ParamName, Is.EqualTo("configRelativePath"));
    }

    /// <summary>
    ///     验证 schema 路径在提供时也必须满足同样的安全相对路径约束。
    /// </summary>
    /// <param name="schemaRelativePath">待验证的 schema 路径。</param>
    [TestCase("../schemas/monster.schema.json")]
    [TestCase(@"..\schemas\monster.schema.json")]
    [TestCase("./schemas/monster.schema.json")]
    [TestCase(@".\schemas\monster.schema.json")]
    [TestCase("schemas/../monster.schema.json")]
    [TestCase(@"schemas\..\monster.schema.json")]
    [TestCase("schemas/./monster.schema.json")]
    [TestCase(@"schemas\.\monster.schema.json")]
    [TestCase("/schemas/monster.schema.json")]
    [TestCase("C:/schemas/monster.schema.json")]
    [TestCase(@"C:\schemas\monster.schema.json")]
    [TestCase("res://schemas/monster.schema.json")]
    [TestCase("user://schemas/monster.schema.json")]
    [TestCase("schemas:bad/monster.schema.json")]
    [TestCase(@"schemas:bad\monster.schema.json")]
    public void Constructor_Should_Throw_When_Schema_Relative_Path_Is_Not_Safe(string schemaRelativePath)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new GodotYamlConfigTableSource("monster", "monster", schemaRelativePath));

        Assert.That(exception!.ParamName, Is.EqualTo("schemaRelativePath"));
    }

    /// <summary>
    ///     验证合法的相对目录和 schema 路径仍可正常构造元数据对象。
    /// </summary>
    [Test]
    public void Constructor_Should_Accept_Safe_Relative_Paths()
    {
        var source = new GodotYamlConfigTableSource(
            "monster",
            "monster/configs",
            "schemas/monster.schema.json");

        Assert.Multiple(() =>
        {
            Assert.That(source.TableName, Is.EqualTo("monster"));
            Assert.That(source.ConfigRelativePath, Is.EqualTo("monster/configs"));
            Assert.That(source.SchemaRelativePath, Is.EqualTo("schemas/monster.schema.json"));
        });
    }
}

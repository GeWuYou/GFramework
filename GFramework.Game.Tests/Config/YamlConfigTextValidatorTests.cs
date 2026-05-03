// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证公开的 YAML 文本校验入口可以在保存前复用运行时同一套 schema 规则。
/// </summary>
[TestFixture]
public sealed class YamlConfigTextValidatorTests
{
    /// <summary>
    ///     为每个测试准备独立临时目录。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.TextValidatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理测试临时目录。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private string _rootPath = null!;

    /// <summary>
    ///     验证合法 YAML 文本会通过公开校验入口。
    /// </summary>
    [Test]
    public void Validate_Should_Succeed_When_Yaml_Matches_Schema()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        YamlConfigTextValidator.Validate(
            "monster",
            schemaPath,
            "monster/generated.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
    }

    /// <summary>
    ///     验证结构错误会继续通过稳定的配置异常类型暴露给宿主。
    /// </summary>
    [Test]
    public void Validate_Should_Throw_ConfigLoadException_When_Yaml_Contains_Unknown_Field()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" }
              }
            }
            """);

        var exception = Assert.Throws<ConfigLoadException>(() =>
            YamlConfigTextValidator.Validate(
                "monster",
                schemaPath,
                "monster/generated.yaml",
                """
                id: 1
                name: Slime
                hp: 10
                """));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.TableName, Is.EqualTo("monster"));
            Assert.That(exception.Diagnostic.SchemaPath, Is.EqualTo(schemaPath));
            Assert.That(exception.Diagnostic.YamlPath, Is.EqualTo("monster/generated.yaml"));
            Assert.That(exception.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.UnknownProperty));
        });
    }

    /// <summary>
    ///     验证异步入口与同步入口共享相同校验语义。
    /// </summary>
    [Test]
    public void ValidateAsync_Should_Throw_ConfigLoadException_When_Required_Field_Is_Missing()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" }
              }
            }
            """);

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() =>
            YamlConfigTextValidator.ValidateAsync(
                "monster",
                schemaPath,
                "monster/generated.yaml",
                """
                id: 1
                """));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.MissingRequiredProperty));
            Assert.That(exception.Diagnostic.SchemaPath, Is.EqualTo(schemaPath));
            Assert.That(exception.Diagnostic.YamlPath, Is.EqualTo("monster/generated.yaml"));
        });
    }

    /// <summary>
    ///     验证公开校验入口会在 schema 文件发生变化后失效旧缓存，
    ///     避免保存路径持续沿用过期的字段约束。
    /// </summary>
    [Test]
    public void Validate_Should_Refresh_Cached_Schema_When_File_Timestamp_Changes()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);
        var yaml = """
                   id: 1
                   name: Slime
                   hp: 10
                   """;

        YamlConfigTextValidator.Validate("monster", schemaPath, "monster/generated.yaml", yaml);

        File.WriteAllText(
            schemaPath,
            """
                {
                  "type": "object",
                  "required": ["id", "name"],
                  "properties": {
                    "id": { "type": "integer" },
                    "name": { "type": "string" }
                  }
                }
                """.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(schemaPath, new DateTime(2040, 1, 1, 0, 0, 1, DateTimeKind.Utc));

        var exception = Assert.Throws<ConfigLoadException>(() =>
            YamlConfigTextValidator.Validate("monster", schemaPath, "monster/generated.yaml", yaml));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.UnknownProperty));
            Assert.That(exception.Diagnostic.SchemaPath, Is.EqualTo(schemaPath));
        });
    }

    /// <summary>
    ///     在临时目录中创建 schema 文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的路径。</param>
    /// <param name="content">文件内容。</param>
    /// <returns>写入后的绝对路径。</returns>
    private string CreateSchemaFile(
        string relativePath,
        string content)
    {
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(fullPath, content.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
        return fullPath;
    }
}

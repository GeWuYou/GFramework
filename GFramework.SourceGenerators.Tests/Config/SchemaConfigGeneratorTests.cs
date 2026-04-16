namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器的错误诊断行为。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorTests
{
    /// <summary>
    ///     验证缺失必填 id 字段时会产生命名明确的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Id_Property_Is_Missing()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["name"],
                                "properties": {
                                  "name": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostics = result.Results.Single().Diagnostics;
        var diagnostic = diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_003"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("monster.schema.json"));
        });
    }

    /// <summary>
    ///     验证空字符串 <c>const</c> 不会在生成 XML 文档时被当成“缺失约束”跳过。
    /// </summary>
    [Test]
    public void Run_Should_Preserve_Empty_String_Const_In_Generated_Documentation()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": {
                                    "type": "string",
                                    "const": ""
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"], Does.Contain("Constraints: const = \"\"."));
    }

    /// <summary>
    ///     验证共享支持的字符串 <c>format</c> 会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_Supported_Duration_Format_Into_Generated_Documentation()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "respawnDelay"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "respawnDelay": {
                                    "type": "string",
                                    "format": "duration"
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"], Does.Contain("Constraints: format = 'duration'."));
    }

    /// <summary>
    ///     验证未纳入共享子集的字符串 <c>format</c> 会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_String_Format_Is_Not_Supported()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "address"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "address": {
                                    "type": "string",
                                    "format": "ipv4"
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("address"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("ipv4"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("date-time"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("duration"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("time"));
        });
    }

    /// <summary>
    ///     验证根节点在非字符串 schema 上声明 <c>format</c> 时也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Root_Node_Uses_Format()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "format": "uuid",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("<root>"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证数组 <c>contains</c> 子 schema 内的非法 <c>format</c> 也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Contains_Schema_Uses_Format_On_Non_String_Node()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "dropIds"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "dropIds": {
                                    "type": "array",
                                    "items": { "type": "integer" },
                                    "contains": {
                                      "type": "integer",
                                      "format": "uuid"
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("dropIds[contains]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证 <c>not</c> 子 schema 的约束会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_Not_Constraint_Into_Generated_Documentation()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": {
                                    "type": "string",
                                    "not": {
                                      "type": "string",
                                      "const": "Deprecated"
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Constraints: not = string (const = \"Deprecated\")."));
    }

    /// <summary>
    ///     验证 <c>not</c> 子 schema 内的非法 <c>format</c> 也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Not_Schema_Uses_Format_On_Non_String_Node()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "hp"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "hp": {
                                    "type": "integer",
                                    "not": {
                                      "type": "integer",
                                      "format": "uuid"
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("hp[not]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证深层不支持的数组嵌套会带着完整字段路径产生命名明确的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Nested_Array_Type_Is_Not_Supported()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "waves": {
                                    "type": "array",
                                    "items": {
                                      "type": "array",
                                      "items": { "type": "integer" }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_004"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("waves"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("array<array>"));
        });
    }

    /// <summary>
    ///     验证 schema 字段名无法映射为合法 C# 标识符时会直接给出诊断，而不是生成不可编译代码。
    /// </summary>
    /// <param name="schemaKey">会映射为非法 C# 标识符的 schema key。</param>
    /// <param name="generatedIdentifier">当前命名规范化逻辑生成出的非法标识符。</param>
    [TestCase("drop$item", "Drop$item")]
    [TestCase("1-phase", "1Phase")]
    public void Run_Should_Report_Diagnostic_When_Schema_Key_Maps_To_Invalid_CSharp_Identifier(
        string schemaKey,
        string generatedIdentifier)
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        var schema = $$"""
                       {
                         "type": "object",
                         "required": ["id", "{{schemaKey}}"],
                         "properties": {
                           "id": { "type": "integer" },
                           "{{schemaKey}}": { "type": "string" }
                         }
                       }
                       """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_006"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain(schemaKey));
            Assert.That(diagnostic.GetMessage(), Does.Contain(generatedIdentifier));
        });
    }

    /// <summary>
    ///     验证 schema 顶层允许通过元数据覆盖默认配置目录，并会统一路径分隔符。
    /// </summary>
    [Test]
    public void Run_Should_Use_Custom_Config_Path_Metadata_For_Generated_Registration()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "x-gframework-config-path": "config\\monster",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("public const string ConfigRelativePath = \"config/monster\";"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"], Does.Contain("Metadata.ConfigRelativePath,"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("public static string SerializeToYaml(MonsterConfig config)"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("public static string GetSchemaPath(string configRootPath)"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("public static void ValidateYaml(string configRootPath, string yamlPath, string yamlText)"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("public static global::System.Threading.Tasks.Task ValidateYamlAsync("));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Contain("GeneratedConfigCatalog.ResolveAbsolutePath(configRootPath, Metadata.ConfigRelativePath)"));
        Assert.That(generatedSources["MonsterConfigBindings.g.cs"],
            Does.Not.Contain("private static string ResolveAbsolutePath"));
        Assert.That(generatedSources["GeneratedConfigCatalog.g.cs"],
            Does.Contain("MonsterConfigBindings.Metadata.ConfigRelativePath"));
        Assert.That(generatedSources["GeneratedConfigCatalog.g.cs"],
            Does.Contain("internal static string ResolveAbsolutePath(string configRootPath, string relativePath)"));
    }

    /// <summary>
    ///     验证生成的索引构建逻辑会跳过运行时空 key，避免 Lazy 索引因格式错误数据永久失效。
    /// </summary>
    [Test]
    public void Run_Should_Skip_Runtime_Null_Keys_When_Generating_Indexed_Lookups()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": {
                                    "type": "string",
                                    "x-gframework-index": true
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterTable.g.cs"], Does.Contain("if (key is null)"));
        Assert.That(generatedSources["MonsterTable.g.cs"],
            Does.Contain("Throwing here would permanently poison the cached index for this wrapper instance."));
    }

    /// <summary>
    ///     验证生成器对 <c>required</c> 名称保持大小写敏感，避免与运行时 validator 对同一 schema 产生分歧。
    /// </summary>
    [Test]
    public void Run_Should_Treat_Required_Property_Names_As_Case_Sensitive()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "Name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"], Does.Contain("public string? Name { get; set; }"));
    }

    /// <summary>
    ///     验证 schema 顶层自定义配置目录元数据不能逃逸配置根目录。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Custom_Config_Path_Metadata_Is_Invalid()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "x-gframework-config-path": "../monster",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_007"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("x-gframework-config-path"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("relative"));
        });
    }

    /// <summary>
    ///     验证查询索引元数据必须是布尔值，避免 schema 作者误以为字符串或数字也会被解释为开关。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Lookup_Index_Metadata_Is_Not_Boolean()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": {
                                    "type": "string",
                                    "x-gframework-index": "yes"
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_008"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("x-gframework-index"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("boolean"));
        });
    }

    /// <summary>
    ///     验证查询索引元数据不能绑定到不满足约束的字段上，避免为嵌套字段生成误导性 API。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Lookup_Index_Metadata_Target_Is_Not_Eligible()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "required": ["rarity"],
                                    "properties": {
                                      "rarity": {
                                        "type": "string",
                                        "x-gframework-index": true
                                      }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_008"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward.rarity"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("top-level required non-key scalar"));
        });
    }

    /// <summary>
    ///     验证根对象直接字段即使 schema key 本身包含点号，也不会被错误识别为嵌套字段。
    /// </summary>
    [Test]
    public void Run_Should_Allow_Lookup_Index_For_Direct_Root_Property_With_Dotted_Schema_Key()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "display.name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "display.name": {
                                    "type": "string",
                                    "x-gframework-index": true
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterTable.g.cs"], Does.Contain("FindByDisplayName(string value)"));
        Assert.That(generatedSources["MonsterTable.g.cs"], Does.Contain("_displayNameIndex"));
    }

    /// <summary>
    ///     验证引用元数据成员名在不同路径规范化后发生碰撞时，生成器仍会分配全局唯一的成员名。
    /// </summary>
    [Test]
    public void Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "drop-items": {
                                    "type": "array",
                                    "items": { "type": "string" },
                                    "x-gframework-ref-table": "item"
                                  },
                                  "drop_items": {
                                    "type": "array",
                                    "items": { "type": "string" },
                                    "x-gframework-ref-table": "item"
                                  },
                                  "dropItems1": {
                                    "type": "string",
                                    "x-gframework-ref-table": "item"
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources.TryGetValue("MonsterConfigBindings.g.cs", out var bindingsSource), Is.True);
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems1 ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems11 ="));
    }

    /// <summary>
    ///     验证生成器只为顶层非主键标量字段生成轻量查询辅助，
    ///     避免把数组、对象和引用字段误生成为查询 API。
    /// </summary>
    [Test]
    public void Run_Should_Generate_Query_Helpers_Only_For_Top_Level_Scalar_Properties()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": {
                                    "type": "string",
                                    "x-gframework-index": true
                                  },
                                  "hp": { "type": "integer" },
                                  "dropItems": {
                                    "type": "array",
                                    "items": { "type": "string" }
                                  },
                                  "targetId": {
                                    "type": "string",
                                    "x-gframework-ref-table": "monster"
                                  },
                                  "reward": {
                                    "type": "object",
                                    "properties": {
                                      "gold": { "type": "integer" }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources.TryGetValue("MonsterTable.g.cs", out var tableSource), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(tableSource, Does.Contain("FindByName(string value)"));
            Assert.That(tableSource, Does.Contain("TryFindFirstByName(string value, out MonsterConfig? result)"));
            Assert.That(tableSource, Does.Contain("_nameIndex"));
            Assert.That(tableSource, Does.Contain("BuildNameIndex"));
            Assert.That(tableSource, Does.Contain("if (value is null)"));
            Assert.That(tableSource, Does.Contain("_nameIndex.Value.TryGetValue(value, out var matches)"));
            Assert.That(tableSource, Does.Contain("materialized.Add(pair.Key, pair.Value.AsReadOnly());"));
            Assert.That(tableSource, Does.Not.Contain("pair.Value.ToArray()"));
            Assert.That(tableSource, Does.Contain("FindByHp(int? value)"));
            Assert.That(tableSource, Does.Contain("TryFindFirstByHp(int? value, out MonsterConfig? result)"));
            Assert.That(tableSource, Does.Not.Contain("_hpIndex"));
            Assert.That(tableSource, Does.Not.Contain("FindById("));
            Assert.That(tableSource, Does.Not.Contain("FindByDropItems("));
            Assert.That(tableSource, Does.Not.Contain("FindByTargetId("));
            Assert.That(tableSource, Does.Not.Contain("FindByReward("));
            Assert.That(tableSource, Does.Not.Contain("TryFindFirstById("));
            Assert.That(tableSource, Does.Not.Contain("TryFindFirstByDropItems("));
            Assert.That(tableSource, Does.Not.Contain("TryFindFirstByTargetId("));
            Assert.That(tableSource, Does.Not.Contain("TryFindFirstByReward("));
        });
    }

    /// <summary>
    ///     验证生成器会为当前消费者项目内的全部 schema 额外产出聚合注册入口，
    ///     让 C# 启动代码可以一行注册所有生成表。
    /// </summary>
    [Test]
    public void Run_Should_Generate_Project_Level_Registration_Catalog()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string monsterSchema = """
                                     {
                                       "type": "object",
                                       "required": ["id"],
                                       "properties": {
                                         "id": { "type": "integer" },
                                         "name": { "type": "string" }
                                       }
                                     }
                                     """;

        const string itemSchema = """
                                  {
                                    "type": "object",
                                    "required": ["id"],
                                    "properties": {
                                      "id": { "type": "string" },
                                      "rarity": { "type": "string" }
                                    }
                                  }
                                  """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", monsterSchema),
            ("item.schema.json", itemSchema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources.TryGetValue("GeneratedConfigCatalog.g.cs", out var catalogSource), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(catalogSource, Does.Contain("public static class GeneratedConfigCatalog"));
            Assert.That(catalogSource, Does.Contain("public sealed class GeneratedConfigRegistrationOptions"));
            Assert.That(catalogSource, Does.Contain("public static class GeneratedConfigRegistrationExtensions"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public global::System.Collections.Generic.IReadOnlyCollection<string>? IncludedConfigDomains { get; init; }"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public global::System.Collections.Generic.IReadOnlyCollection<string>? IncludedTableNames { get; init; }"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public global::System.Predicate<GeneratedConfigCatalog.TableMetadata>? TableFilter { get; init; }"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public global::System.Collections.Generic.IEqualityComparer<string>? ItemComparer { get; init; }"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public global::System.Collections.Generic.IEqualityComparer<int>? MonsterComparer { get; init; }"));
            Assert.That(catalogSource, Does.Contain("return RegisterAllGeneratedConfigTables(loader, options: null);"));
            Assert.That(catalogSource, Does.Contain("GeneratedConfigRegistrationOptions? options"));
            Assert.That(catalogSource, Does.Contain("loader.RegisterItemTable(effectiveOptions.ItemComparer);"));
            Assert.That(catalogSource,
                Does.Contain(
                    "if (GeneratedConfigCatalog.MatchesRegistrationOptions(GeneratedConfigCatalog.Tables[1], effectiveOptions))"));
            Assert.That(catalogSource, Does.Contain("loader.RegisterMonsterTable(effectiveOptions.MonsterComparer);"));
            Assert.That(catalogSource, Does.Contain("ItemConfigBindings.Metadata.TableName"));
            Assert.That(catalogSource, Does.Contain("MonsterConfigBindings.Metadata.TableName"));
            Assert.That(catalogSource,
                Does.Contain("public static bool TryGetByTableName(string tableName, out TableMetadata metadata)"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public static global::System.Collections.Generic.IReadOnlyList<TableMetadata> GetTablesInConfigDomain(string configDomain)"));
            Assert.That(catalogSource,
                Does.Contain(
                    "public static global::System.Collections.Generic.IReadOnlyList<TableMetadata> GetTablesForRegistration(GeneratedConfigRegistrationOptions? options = null)"));
            Assert.That(catalogSource,
                Does.Contain("public static bool MatchesRegistrationOptions("));
            Assert.That(catalogSource,
                Does.Contain(
                    "if (GeneratedConfigCatalog.MatchesRegistrationOptions(GeneratedConfigCatalog.Tables[0], effectiveOptions))"));
            Assert.That(catalogSource, Does.Contain("private static bool MatchesOptionalAllowList("));
        });
    }
}

namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器的错误诊断行为。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorTests
{
    // Keep shared fixture sources at class scope so MA0051 reduction does not change generator inputs.
    private const string DummySource = """
                                       namespace TestApp
                                       {
                                           public sealed class Dummy
                                           {
                                           }
                                       }
                                       """;

    // These runtime contracts mirror the minimal consumer surface the generator expects when emitting registration helpers.
    private const string ConfigRuntimeSource = """
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

    /// <summary>
    ///     验证 AdditionalFiles 读取被取消时会向上传播取消，而不是伪造成 schema 诊断。
    /// </summary>
    [Test]
    public void Run_Should_Propagate_Cancellation_When_AdditionalText_Read_Is_Cancelled()
    {
        var method = typeof(global::GFramework.Game.SourceGenerators.Config.SchemaConfigGenerator)
            .GetMethod(
                "TryReadSchemaText",
                global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var invocationArguments = new object?[]
        {
            new ThrowingAdditionalText("monster.schema.json"),
            cancellationTokenSource.Token,
            null,
            null
        };

        var exception = Assert.Throws<global::System.Reflection.TargetInvocationException>(() =>
            method!.Invoke(null, invocationArguments));

        Assert.That(exception!.InnerException, Is.TypeOf<OperationCanceledException>());
    }

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
    ///     验证根节点 <c>type</c> 元数据不是字符串时，会返回根对象约束诊断，而不是抛出 JSON 访问异常。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Root_Type_Metadata_Is_Not_A_String()
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
                                "type": 123,
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_002"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("monster.schema.json"));
        });
    }

    /// <summary>
    ///     验证 schema 文件名若生成无效根类型标识符时，会在生成前产生命名明确的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Schema_File_Name_Generates_Invalid_Root_Type_Identifier()
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
                                  "name": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("123-monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_006"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("<root>"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("123-monster"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("123MonsterConfig"));
        });
    }

    /// <summary>
    ///     用于模拟 AdditionalFiles 读取阶段直接收到取消请求的测试桩。
    /// </summary>
    private sealed class ThrowingAdditionalText : AdditionalText
    {
        /// <summary>
        ///     创建一个在读取时抛出取消异常的 AdditionalText。
        /// </summary>
        /// <param name="path">虚拟 schema 路径。</param>
        public ThrowingAdditionalText(string path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
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
    ///     验证对象 <c>dependentRequired</c> 会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_DependentRequired_Constraint_Into_Generated_Documentation()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" },
                                      "bonusId": { "type": "string" },
                                      "bonusCount": { "type": "integer" }
                                    },
                                    "dependentRequired": {
                                      "itemId": ["itemCount"],
                                      "bonusId": ["bonusCount"]
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
        Assert.That(
            generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Constraints: dependentRequired = { itemId =&gt; [itemCount]; bonusId =&gt; [bonusCount] }."));
    }

    /// <summary>
    ///     验证生成器会拒绝引用未声明对象字段的 <c>dependentRequired</c> 元数据。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_DependentRequired_Target_Is_Not_Declared()
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
                                    "properties": {
                                      "itemId": { "type": "string" }
                                    },
                                    "dependentRequired": {
                                      "itemId": ["itemCount"]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_010"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("itemCount"));
        });
    }

    /// <summary>
    ///     验证对象 <c>dependentSchemas</c> 会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_DependentSchemas_Constraint_Into_Generated_Documentation()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" }
                                    },
                                    "dependentSchemas": {
                                      "itemId": {
                                        "type": "object",
                                        "required": ["itemCount"],
                                        "properties": {
                                          "itemCount": { "type": "integer" }
                                        }
                                      }
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
        Assert.That(
            generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Constraints: dependentSchemas = { itemId =&gt; object (required = [itemCount]) }."));
    }

    /// <summary>
    ///     验证生成器会拒绝非 object-typed 的 <c>dependentSchemas</c> 子 schema。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Is_Not_Object_Typed()
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
                                    "properties": {
                                      "itemId": { "type": "string" }
                                    },
                                    "dependentSchemas": {
                                      "itemId": {
                                        "type": "string",
                                        "const": "potion"
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_011"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("object-typed 'dependentSchemas' schema"));
        });
    }

    /// <summary>
    ///     验证只有 object 节点允许声明 <c>dependentSchemas</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_DependentSchemas()
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
                                "required": ["id", "tag"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "tag": {
                                    "type": "string",
                                    "dependentSchemas": {
                                      "itemId": {
                                        "type": "object",
                                        "properties": {}
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_011"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("tag"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only object schemas can declare 'dependentSchemas'."));
        });
    }

    /// <summary>
    ///     验证 <c>dependentSchemas</c> 子 schema 内的非法 <c>format</c> 也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Uses_Format_On_Non_String_Node()
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
                                    "properties": {
                                      "itemId": { "type": "string" }
                                    },
                                    "dependentSchemas": {
                                      "itemId": {
                                        "type": "object",
                                        "properties": {
                                          "bonus": {
                                            "type": "integer",
                                            "format": "uuid"
                                          }
                                        }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[dependentSchemas:itemId].bonus"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证对象 <c>allOf</c> 会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "required": ["itemCount"],
                                        "properties": {
                                          "itemCount": { "type": "integer" }
                                        }
                                      }
                                    ]
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
        Assert.That(
            generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Constraints: allOf = [ object (required = [itemCount]) ]."));
    }

    /// <summary>
    ///     验证只有 object 节点允许声明 <c>allOf</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf()
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
                                "required": ["id", "tag"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "tag": {
                                    "type": "string",
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "properties": {}
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("tag"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only object schemas can declare 'allOf'."));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝非数组的 <c>allOf</c> 声明。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": {
                                      "type": "object",
                                      "properties": {
                                        "itemCount": { "type": "integer" }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("The 'allOf' value must be an array."));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝非对象值的 <c>allOf</c> 条目。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Valued()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [123]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Entry #1 in 'allOf' must be an object-valued schema."));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝非 object-typed 的 <c>allOf</c> 条目。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "string",
                                        "const": "potion"
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Entry #1 in 'allOf' must declare an object-typed schema."));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝把 <c>allOf.properties</c> 声明为非对象映射。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Properties_Is_Not_Object_Valued()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "properties": 1
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must declare 'properties' as an object-valued map"));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝把 <c>allOf.required</c> 声明为非数组。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Is_Not_An_Array()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "required": {}
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must declare 'required' as an array of parent property names"));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝把 <c>allOf.required</c> 条目声明为非字符串。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Item_Is_Not_A_String()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "required": [1]
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must declare 'required' entries as parent property-name strings"));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝把 <c>allOf.required</c> 条目声明为空白字段名。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Item_Is_Blank()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "required": [""]
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("cannot declare blank property names in 'required'"));
        });
    }

    /// <summary>
    ///     验证生成器会拒绝在 <c>allOf</c> 中引入父对象未声明的字段。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_AllOf_Entry_Targets_Undeclared_Parent_Property()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "required": ["bonus"]
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_012"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("requires property 'bonus'"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("parent object schema"));
        });
    }

    /// <summary>
    ///     验证 allOf 内层递归诊断路径会与运行时保持一致。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_AllOf_Inner_Schema_Is_Invalid()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "allOf": [
                                      {
                                        "type": "object",
                                        "properties": {
                                          "itemCount": {
                                            "type": "integer",
                                            "format": "uuid"
                                          }
                                        }
                                      }
                                    ]
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
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[allOf[0]].itemCount"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证 object-focused <c>if</c> / <c>then</c> / <c>else</c> 会写入生成 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_IfThenElse_Constraint_Into_Generated_Documentation()
    {
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" },
                                      "bonus": { "type": "integer" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "properties": {
                                        "itemId": {
                                          "type": "string",
                                          "const": "potion"
                                        }
                                      }
                                    },
                                    "then": {
                                      "type": "object",
                                      "required": ["itemCount"],
                                      "properties": {
                                        "itemCount": { "type": "integer" }
                                      }
                                    },
                                    "else": {
                                      "type": "object",
                                      "required": ["bonus"],
                                      "properties": {
                                        "bonus": { "type": "integer" }
                                      }
                                    }
                                  }
                                }
                              }
                              """;

        var generatedSources = RunAndCollectGeneratedSources(
            DummySource,
            ("monster.schema.json", schema));
        Assert.That(
            generatedSources["MonsterConfig.g.cs"],
            Does.Contain(
                "Constraints: if/then/else = if object; properties = { itemId: string (const = \"potion\") }; " +
                "then object (required = [itemCount]); properties = { itemCount: integer }; " +
                "else object (required = [bonus]); properties = { bonus: integer }."));
    }

    /// <summary>
    ///     验证缺少 <c>if</c> 时生成器会拒绝孤立的 <c>then</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Then_Is_Declared_Without_If()
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
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "then": {
                                      "type": "object",
                                      "required": ["itemCount"],
                                      "properties": {
                                        "itemCount": { "type": "integer" }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_013"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must also declare 'if'"));
        });
    }

    /// <summary>
    ///     验证缺少 <c>if</c> 时生成器也会拒绝孤立的 <c>else</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Else_Is_Declared_Without_If()
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
                                    "properties": {
                                      "bonus": { "type": "integer" }
                                    },
                                    "else": {
                                      "type": "object",
                                      "required": ["bonus"],
                                      "properties": {
                                        "bonus": { "type": "integer" }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_013"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must also declare 'if'"));
        });
    }

    /// <summary>
    ///     验证只声明 <c>if</c> 而没有分支时，生成器会给出对齐运行时的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_If_Is_Declared_Without_Then_Or_Else()
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
                                    "properties": {
                                      "itemId": { "type": "string" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "properties": {
                                        "itemId": {
                                          "type": "string",
                                          "const": "potion"
                                        }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_013"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must also declare at least one of 'then' or 'else'"));
        });
    }

    /// <summary>
    ///     验证条件分支不是 object schema 时，诊断路径会定位到具体分支而不是父对象。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_With_Branch_Path_When_Then_Schema_Is_Not_Object()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "properties": {
                                        "itemId": {
                                          "type": "string",
                                          "const": "potion"
                                        }
                                      }
                                    },
                                    "then": []
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_013"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[then]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("must be an object-valued schema"));
        });
    }

    /// <summary>
    ///     验证条件分支不能引用父对象未声明的字段。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Conditional_Schema_Requires_Undeclared_Parent_Property()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "required": ["bonusCount"],
                                      "properties": {
                                        "itemId": { "type": "string" }
                                      }
                                    },
                                    "then": {
                                      "type": "object",
                                      "properties": {
                                        "itemCount": { "type": "integer" }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_013"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[if]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("bonusCount"));
        });
    }

    /// <summary>
    ///     验证生成器会显式拒绝当前共享子集尚未支持的 <c>oneOf</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Object_Schema_Declares_Unsupported_OneOf()
    {
        const string source = DummySource;
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "oneOf": [
                                      {
                                        "type": "object",
                                        "required": ["itemCount"],
                                        "properties": {
                                          "itemCount": { "type": "integer" }
                                        }
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_015"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("oneOf"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not support combinators that can change generated type shape"));
        });
    }

    /// <summary>
    ///     验证生成器会显式拒绝当前共享子集尚未支持的 <c>anyOf</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Object_Schema_Declares_Unsupported_AnyOf()
    {
        const string source = DummySource;
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    },
                                    "anyOf": [
                                      {
                                        "type": "object",
                                        "required": ["itemCount"],
                                        "properties": {
                                          "itemCount": { "type": "integer" }
                                        }
                                      }
                                    ]
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_015"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("anyOf"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not support combinators that can change generated type shape"));
        });
    }

    /// <summary>
    ///     验证生成器接受显式声明的 <c>additionalProperties: false</c>。
    /// </summary>
    [Test]
    public void Run_Should_Accept_When_Object_Schema_Declares_AdditionalProperties_False()
    {
        const string source = DummySource;
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "additionalProperties": false,
                                    "properties": {
                                      "itemCount": { "type": "integer" }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
    }

    /// <summary>
    ///     验证生成器会拒绝会打开动态字段形状的 <c>additionalProperties</c>。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Object_Schema_Declares_Unsupported_AdditionalProperties()
    {
        const string source = DummySource;
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "additionalProperties": true,
                                    "properties": {
                                      "itemCount": { "type": "integer" }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_016"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("additionalProperties"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("only accepts 'additionalProperties: false'"));
        });
    }

    /// <summary>
    ///     验证 <c>then</c> 子 schema 内的非法 <c>format</c> 也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_Then_Inner_Schema_Is_Invalid()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "itemCount": { "type": "integer" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "properties": {
                                        "itemId": {
                                          "type": "string",
                                          "const": "potion"
                                        }
                                      }
                                    },
                                    "then": {
                                      "type": "object",
                                      "properties": {
                                        "itemCount": {
                                          "type": "integer",
                                          "format": "uuid"
                                        }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[then].itemCount"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Only 'string' properties can declare 'format'."));
        });
    }

    /// <summary>
    ///     验证 <c>else</c> 子 schema 内的非法 <c>format</c> 也会在生成阶段直接给出诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_Else_Inner_Schema_Is_Invalid()
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
                                    "properties": {
                                      "itemId": { "type": "string" },
                                      "bonus": { "type": "integer" }
                                    },
                                    "if": {
                                      "type": "object",
                                      "properties": {
                                        "itemId": {
                                          "type": "string",
                                          "const": "potion"
                                        }
                                      }
                                    },
                                    "else": {
                                      "type": "object",
                                      "properties": {
                                        "bonus": {
                                          "type": "integer",
                                          "format": "uuid"
                                        }
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
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("reward[else].bonus"));
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
    ///     验证同一对象内不同 schema key 若归一化后映射到同一属性名，会在生成前直接给出冲突诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Schema_Keys_Collide_After_Identifier_Normalization()
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
                                  "foo-bar": { "type": "string" },
                                  "foo_bar": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_014"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("foo_bar"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("FooBar"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("foo-bar"));
        });
    }

    /// <summary>
    ///     验证 schema 顶层允许通过元数据覆盖默认配置目录，并会统一路径分隔符。
    /// </summary>
    [Test]
    public void Run_Should_Use_Custom_Config_Path_Metadata_For_Generated_Registration()
    {
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", schema));
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", schema));
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", schema));
        Assert.That(generatedSources["MonsterTable.g.cs"], Does.Contain("FindByDisplayName(string value)"));
        Assert.That(generatedSources["MonsterTable.g.cs"], Does.Contain("_displayNameIndex"));
    }

    /// <summary>
    ///     验证引用元数据成员名在不同合法字段路径规范化后发生碰撞时，生成器仍会分配全局唯一的成员名。
    /// </summary>
    [Test]
    public void Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names()
    {
        const string schema = """
                              {
                                "type": "object",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "drop": {
                                    "type": "object",
                                    "properties": {
                                      "items": {
                                        "type": "array",
                                        "items": { "type": "string" },
                                        "x-gframework-ref-table": "item"
                                      },
                                      "items1": {
                                        "type": "string",
                                        "x-gframework-ref-table": "item"
                                      }
                                    }
                                  },
                                  "dropItems": {
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", schema));
        Assert.That(generatedSources.TryGetValue("MonsterConfigBindings.g.cs", out var bindingsSource), Is.True);
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems1 ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems2 ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems11 ="));
    }

    /// <summary>
    ///     验证生成器只为顶层非主键标量字段生成轻量查询辅助，
    ///     避免把数组、对象和引用字段误生成为查询 API。
    /// </summary>
    [Test]
    public void Run_Should_Generate_Query_Helpers_Only_For_Top_Level_Scalar_Properties()
    {
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", schema));
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

        var generatedSources = RunAndCollectGeneratedSources(
            ConfigRuntimeSource,
            ("monster.schema.json", monsterSchema),
            ("item.schema.json", itemSchema));
        if (!generatedSources.TryGetValue("GeneratedConfigCatalog.g.cs", out var catalogSource))
        {
            Assert.Fail("Expected GeneratedConfigCatalog.g.cs to be generated.");
        }

        AssertGeneratedRegistrationCatalogContract(catalogSource!);
    }

    /// <summary>
    ///     断言聚合注册目录保留筛选选项、比较器透传和按条件注册的生成契约。
    /// </summary>
    /// <param name="catalogSource">`GeneratedConfigCatalog.g.cs` 的生成源码。</param>
    private static void AssertGeneratedRegistrationCatalogContract(string catalogSource)
    {
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
            Assert.That(catalogSource,
                Does.Contain(
                    "using <c>global::System.Collections.Generic.IEqualityComparer&lt;string&gt;?</c> when aggregate registration runs."));
            Assert.That(catalogSource,
                Does.Contain(
                    "using <c>global::System.Collections.Generic.IEqualityComparer&lt;int&gt;?</c> when aggregate registration runs."));
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

    /// <summary>
    ///     运行 schema 生成器并收集生成输出，同时断言本次场景不产生诊断。
    /// </summary>
    /// <param name="source">测试输入源码。</param>
    /// <param name="additionalFiles">参与本次生成的 schema 文件集合。</param>
    /// <returns>按 HintName 索引的生成源码字典。</returns>
    private static IReadOnlyDictionary<string, string> RunAndCollectGeneratedSources(
        string source,
        params (string path, string content)[] additionalFiles)
    {
        var result = SchemaGeneratorTestDriver.Run(source, additionalFiles);
        var runResult = result.Results.Single();
        Assert.That(runResult.Diagnostics, Is.Empty);

        return runResult.GeneratedSources.ToDictionary(
            static sourceResult => sourceResult.HintName,
            static sourceResult => sourceResult.SourceText.ToString(),
            StringComparer.Ordinal);
    }
}

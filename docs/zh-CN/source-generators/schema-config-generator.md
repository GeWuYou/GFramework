---
title: Schema 配置生成器
description: 说明 GFramework.Game.SourceGenerators 如何从 schema 生成配置类型、表包装和聚合注册入口。
---

# Schema 配置生成器

`GFramework.Game.SourceGenerators` 会把消费者项目里的 `schemas/**/*.schema.json` 读入编译期管线，并生成：

- 强类型配置类型
- 对应的表包装类型
- 单表绑定辅助代码
- 聚合注册目录与 `RegisterAllGeneratedConfigTables(...)` 扩展入口

如果你当前目标是“先把配置系统接进项目”，先看 [游戏内容配置系统](../game/config-system.md)。这页更适合在你已经决定使用
`Game.SourceGenerators` 之后，继续确认 schema 输入契约、生成结果和常见诊断边界。

## 它解决什么问题

相比只写 `YAML` 和运行时加载代码，这个生成器把三件事前移到了编译期：

- 把 schema 转成 `Config` 类型，让业务代码直接拿到强类型字段和 XML 文档
- 为运行时表生成包装层，让 `Get`、`TryGet`、按字段查找等入口保持稳定
- 汇总当前项目中所有 schema 的注册信息，避免 schema 数量增长后继续手写逐表注册

这也是 `GFramework.Game` 配置运行时、VS Code 配置工具和 schema 约束能够共享同一份结构定义的基础。

## 最小接入路径

NuGet 安装保持运行时包与生成器包版本一致，并把生成器声明为编译期依赖：

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Game" Version="x.y.z" />
  <PackageReference Include="GeWuYou.GFramework.Game.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

默认情况下，包内 `targets` 会自动把 `schemas/**/*.schema.json` 纳入 `AdditionalFiles`。如果你的 schema 目录不是
`schemas/`，可以在项目文件里覆盖：

```xml
<PropertyGroup>
  <GFrameworkConfigSchemaDirectory>GameSchemas</GFrameworkConfigSchemaDirectory>
</PropertyGroup>
```

## 输入约定

### schema 根对象

当前生成器要求每个 schema 都满足这些基本约束：

- 顶层 `type` 必须是 `object`
- 必须声明必填 `id` 字段
- `id` 目前只支持 `integer` 和 `string`
- schema 文件名与属性名都必须能稳定映射到合法的 C# 标识符

最小示例：

```json
{
  "title": "Monster Config",
  "type": "object",
  "required": ["id", "name"],
  "properties": {
    "id": {
      "type": "integer"
    },
    "name": {
      "type": "string"
    }
  }
}
```

### 路径与索引元数据

除了标准 JSON Schema 字段，当前还支持几个会直接影响生成结果的扩展元数据：

- `x-gframework-config-path`
  - 覆盖默认配置目录；值必须是相对路径，且不能包含 `.`、`..` 或绝对路径段
- `x-gframework-index`
  - 为顶层必填、非主键、非引用的标量字段生成 `FindBy...` / `TryFindFirstBy...` 查找入口
- `x-gframework-ref-table`
  - 为字段补充跨表引用语义，并把这部分信息写入生成的绑定元数据

如果某个字段不满足 lookup index 的安全条件，生成器会直接报诊断，而不是静默生成一个容易失真的查询入口。

### 当前稳定约束子集

从源码和快照测试看，当前共享子集已经覆盖：

- 标量、嵌套对象、对象数组、标量数组
- `default`、`enum`、`const`
- `minimum`、`maximum`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`
- `minLength`、`maxLength`、`pattern`
- `format`
  - 当前稳定字符串子集是 `date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`
- `minItems`、`maxItems`、`uniqueItems`、`contains`、`minContains`、`maxContains`
- `minProperties`、`maxProperties`
- `dependentRequired`、`dependentSchemas`、`allOf`
- 面向对象约束的 `if` / `then` / `else`

这些约束并不一定都会改变生成类型的形状，但会被保留到生成代码文档和绑定元数据里，方便运行时与工具链共享。

## 会生成什么

以 `monster.schema.json` 为例，当前生成器会形成四组稳定输出：

### 1. 配置类型

例如 `MonsterConfig`。它承载 schema 字段到 C# 属性的映射，以及对应的 XML 文档。

### 2. 表包装类型

例如 `MonsterTable`。它包装运行时 `IConfigTable<TKey, TValue>`，并在需要时提供生成的查找入口：

```csharp
public sealed partial class MonsterTable : IConfigTable<int, MonsterConfig>
{
    public MonsterConfig Get(int key) { ... }

    public IReadOnlyList<MonsterConfig> FindByName(string value) { ... }

    public bool TryFindFirstByName(string value, out MonsterConfig? result) { ... }
}
```

如果字段声明了 `x-gframework-index`，生成器会优先使用延迟构建的只读索引；否则按当前表快照做确定性的线性扫描，以保持和热重载后的运行时数据一致。

### 3. 单表绑定辅助

例如 `MonsterConfigBindings`。这里会保留单表注册所需的表名、schema 路径、配置路径和引用元数据，方便项目侧继续组合自己的启动逻辑。

### 4. 聚合注册目录

当一个项目里有多个 schema 时，生成器还会汇总出 `GeneratedConfigCatalog` 与聚合扩展：

```csharp
loader.RegisterAllGeneratedConfigTables();
```

如果你需要按表传入额外比较器或做更细粒度控制，还可以使用带 `GeneratedConfigRegistrationOptions` 的重载。

## 运行时如何消费这些输出

最常见的消费路径是把生成器输出交给 `GameConfigBootstrap` 或 `YamlConfigLoader`：

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var bootstrap = new GameConfigBootstrap(
    new GameConfigBootstrapOptions
    {
        RootPath = configRootPath,
        ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables()
    });
```

当你需要继续手动拼装运行时，也可以直接用单表绑定或聚合目录做自己的注册封装。这时建议把“启动生命周期”和“业务读取入口”分开，继续沿用
[游戏内容配置系统](../game/config-system.md) 里的 `GameConfigHost` / `GameConfigRuntime` 模板。

## 迁移与兼容性

如果你当前项目还在手写逐表注册，最小迁移路径可以按下面的顺序推进：

1. 保持 `GeWuYou.GFramework.Game` 与 `GeWuYou.GFramework.Game.SourceGenerators` 版本一致，并把生成器包声明为 `PrivateAssets="all"` 的编译期依赖。
2. 把现有 schema 收敛到 `schemas/**/*.schema.json`，或在项目文件中显式设置 `GFrameworkConfigSchemaDirectory`。
3. 用 `RegisterAllGeneratedConfigTables(...)` 替换启动阶段里手写的逐表注册代码，再按需通过 `GeneratedConfigRegistrationOptions` 补充单表比较器或个别覆盖。
4. 继续保留原有运行时消费入口，例如 `GameConfigBootstrap`、`YamlConfigLoader`、`GameConfigHost` 或 `GameConfigRuntime`，避免把“生成注册入口切换”与“运行时读取方式重写”混成同一步。

当前兼容边界主要有三类：

- 生成器只覆盖当前公开的 schema 子集；如果现有 schema 使用了尚未支持的结构或元数据，编译期会通过 `ConfigSchemaDiagnostics` 直接暴露，而不会静默退回到手写模型。
- `x-gframework-index`、路径元数据和标识符映射都要求满足安全约束；旧项目里依赖宽松命名或不安全路径的配置，需要先整理 schema，再切换到聚合注册入口。
- 迁移到生成链路后，运行时读取模型保持不变，但“表类型定义”和“注册目录”会改为编译期输出，因此自定义扩展更适合挂在单表绑定或 `ConfigureLoader` 阶段，而不是继续复制生成器会维护的样板代码。

如果你需要分批迁移，当前更稳妥的回退方式是只回退注册入口，而不是同时回退 schema 结构：

- 可以先让一部分表继续沿手写注册保留，另一部分切到生成的单表绑定或聚合注册。
- 如果某张表在迁移后触发诊断，优先根据诊断 ID 收敛 schema，再决定是否临时回到手写注册。
- 当项目已经接受 schema 目录约定后，尽量不要再把生成链路和旧的命名 / 路径规则长期并存，否则后续维护会同时承担两套约束。

## 常见诊断边界

当前 `ConfigSchemaDiagnostics` 暴露的错误主要分成四类：

- schema 根对象或 JSON 读入失败
  - 例如 `GF_ConfigSchema_001`、`GF_ConfigSchema_002`
- 主键与类型映射不合法
  - 例如 `GF_ConfigSchema_003`、`GF_ConfigSchema_005`
- 生成标识符或路径元数据不安全
  - 例如 `GF_ConfigSchema_006`、`GF_ConfigSchema_007`
- 额外约束元数据不合法
  - 例如 `GF_ConfigSchema_008` 到 `GF_ConfigSchema_014`

这些边界由
[Schema 生成器行为测试](https://github.com/GeWuYou/GFramework/blob/main/GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs)
和
[生成结果快照测试](https://github.com/GeWuYou/GFramework/blob/main/GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorSnapshotTests.cs)
共同覆盖。遇到生成失败时，优先先看诊断 ID，再回头核对 schema 本身是否超出当前公开子集。

## 什么时候优先看这页

适合：

- 你想确认一个 schema 字段会生成哪些 C# 入口
- 你要排查 `x-gframework-index`、路径元数据或标识符冲突
- 你在做项目级聚合注册，希望知道 `GeneratedConfigCatalog` 和 `RegisterAllGeneratedConfigTables(...)` 的边界

不适合：

- 你只是第一次接入配置运行时
- 你更关心 `GameConfigBootstrap`、热重载、Godot 资源路径或 VS Code 配置工具

这些采用问题分别回到：

- [游戏内容配置系统](../game/config-system.md)
- [VS Code 配置工具](../game/config-tool.md)
- [源码生成器总览](./index.md)

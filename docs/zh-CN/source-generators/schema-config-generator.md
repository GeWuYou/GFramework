# Schema Config 生成器

> 根据 `*.schema.json` 生成配置实体、表包装和注册辅助代码。

## 概述

`SchemaConfigGenerator` 位于 `GFramework.SourceGenerators.Config.SchemaConfigGenerator`，面向 `GFramework.Game.Config` 的
AI-First 内容配置工作流。

它的输入不是特性，而是编译器 `AdditionalFiles` 中的 `*.schema.json`。

当消费者项目引用仓库里的 `GeWuYou.GFramework.SourceGenerators.targets` 时，默认会把 `schemas/**/*.schema.json` 收进生成器输入。

## 当前前置约束

每个 schema 至少需要满足：

- 根节点 `type` 必须是 `object`
- 必须声明 `id` 字段
- `id` 必须同时出现在 `required` 中
- `id` 类型只能是 `integer` 或 `string`
- schema 字段名必须能安全映射成 C# 标识符

可选元数据：

- `x-gframework-config-path`：覆盖 YAML 相对目录
- `x-gframework-ref-table`：声明跨表引用

## 当前支持的 schema 子集

- 根对象
- 嵌套对象
- 标量类型：`integer`、`number`、`boolean`、`string`
- 数组
- `required`
- `enum`
- 数值约束：`minimum`、`maximum`、`exclusiveMinimum`、`exclusiveMaximum`
- 字符串约束：`minLength`、`maxLength`、`pattern`
- 数组约束：`minItems`、`maxItems`

## 生成输出

对每个有效 schema，当前生成器会产出四类代码：

### 1. 配置实体

例如 `monster.schema.json` 会生成 `MonsterConfig`。

### 2. 表包装

例如 `MonsterTable`，实现对底层 `IConfigTable<TKey, TValue>` 的强类型包装。

### 3. 单表绑定类

例如 `MonsterConfigBindings`，其中包含：

- `Metadata.TableName`
- `Metadata.ConfigRelativePath`
- `Metadata.SchemaRelativePath`
- `RegisterMonsterTable(...)`
- `GetMonsterTable(...)`
- `TryGetMonsterTable(...)`
- `References` 元数据访问入口

### 4. 项目级聚合入口

当当前消费者项目里有多个 schema 时，还会额外生成：

- `GeneratedConfigCatalog`
- `GeneratedConfigRegistrationOptions`
- `GeneratedConfigRegistrationExtensions.RegisterAllGeneratedConfigTables(...)`

这让启动代码可以用一个入口完成所有表注册，而不是每新增一个 schema 就手写一条注册语句。

## 基础接入

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var bootstrap = new GameConfigBootstrap(
    new GameConfigBootstrapOptions
    {
        RootPath = configRootPath,
        ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables()
    });

await bootstrap.InitializeAsync();
```

更完整的运行时接入、热重载和工具链说明见 [游戏内容配置系统](/zh-CN/game/config-system)。

## 当前诊断

| 诊断 ID                 | 含义                             |
|-----------------------|--------------------------------|
| `GF_ConfigSchema_001` | schema JSON 无法解析               |
| `GF_ConfigSchema_002` | 根节点不是 `object`                 |
| `GF_ConfigSchema_003` | 缺少必需的 `id` 字段                  |
| `GF_ConfigSchema_004` | 使用了当前不支持的字段类型                  |
| `GF_ConfigSchema_005` | `id` 类型不是 `integer` / `string` |
| `GF_ConfigSchema_006` | schema 字段名不能安全映射成 C# 标识符       |
| `GF_ConfigSchema_007` | `x-gframework-config-path` 非法  |

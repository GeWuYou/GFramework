# 游戏内容配置系统

> 面向静态游戏内容的 YAML + JSON Schema 工作流。

## 定位

这套配置系统用于管理怪物、物品、技能、任务等静态内容数据。

它与以下能力分工不同：

- `GFramework.Core.Configuration`：运行时键值配置
- `GFramework.Game.Setting`：玩家设置与持久化

## 当前已验证能力

- 使用 YAML 作为配置源文件
- 使用 JSON Schema 描述结构与约束
- 一对象一文件的目录组织
- 运行时按只读配置表查询
- 通过 `SchemaConfigGenerator` 生成配置实体、表包装和注册辅助
- 通过 `YamlConfigLoader` 与 `GameConfigBootstrap` 完成加载与热重载
- 通过 VS Code 扩展提供浏览、校验、轻量表单、批量编辑和引用跳转能力

## 推荐目录结构

```text
GameProject/
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
└─ schemas/
   ├─ monster.schema.json
   └─ item.schema.json
```

## Schema 约定

### 最小示例

```json
{
  "title": "Monster Config",
  "x-gframework-config-path": "config/monster",
  "type": "object",
  "required": ["id", "name"],
  "properties": {
    "id": {
      "type": "integer"
    },
    "name": {
      "type": "string"
    },
    "dropItems": {
      "type": "array",
      "items": {
        "type": "string",
        "enum": ["potion", "slime_gel", "bomb"]
      },
      "x-gframework-ref-table": "item"
    }
  }
}
```

### 当前支持的 schema 子集

- 根对象
- 嵌套对象
- `required`
- 标量类型：`integer`、`number`、`boolean`、`string`
- 数组
- `enum`
- 数值约束：`minimum`、`maximum`、`exclusiveMinimum`、`exclusiveMaximum`
- 字符串约束：`minLength`、`maxLength`、`pattern`
- 数组约束：`minItems`、`maxItems`

### 当前支持的扩展元数据

- `x-gframework-config-path`
    - 覆盖默认 YAML 相对目录
    - 必须是相对路径
    - 不允许包含 `..`
- `x-gframework-ref-table`
    - 声明字段引用的运行时配置表名
    - 运行时会在加载与热重载时校验引用完整性

更完整的生成器输出与诊断说明见 [Schema Config 生成器](/zh-CN/source-generators/schema-config-generator)。

## YAML 示例

```yaml
id: 1
name: Slime
dropItems:
  - potion
  - slime_gel
```

## 运行时接入

推荐使用 `GameConfigBootstrap`，并在 `ConfigureLoader` 中调用生成器产出的聚合注册入口：

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var bootstrap = new GameConfigBootstrap(
    new GameConfigBootstrapOptions
    {
        RootPath = configRootPath,
        ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables(),
        EnableHotReload = true,
        HotReloadOptions = new YamlConfigHotReloadOptions
        {
            OnTableReloaded = tableName => Console.WriteLine($"Reloaded: {tableName}"),
            OnTableReloadFailed = static (tableName, exception) =>
                Console.WriteLine($"Reload failed: {tableName} - {exception.Message}")
        }
    });

await bootstrap.InitializeAsync();
```

初始化成功后，可通过注册表读取生成的强类型表：

```csharp
var registry = bootstrap.Registry;
var monsterTable = registry.GetMonsterTable();
var slime = monsterTable.Get(1);
```

## 热重载语义

当前实现的热重载由 `YamlConfigLoader.EnableHotReload(...)` 提供，具备以下特征：

- 监听 YAML 文件改动
- 使用默认 200ms 防抖
- 会按“变更表 + 依赖它的表”这一受影响闭包整体重载
- 只有整组表重新通过校验后才提交到注册表
- 失败时通过 `OnTableReloadFailed` 回调暴露异常，已提交的旧表保持不变

## VS Code 工具当前范围

仓库中的 `tools/gframework-config-tool` 当前已落地的 MVP 范围包括：

- 浏览工作区 `config/` 目录
- 打开原始 YAML
- 打开匹配 schema
- 递归轻量校验
- 轻量表单编辑
- 从引用字段跳转到引用 schema、配置域或具体配置文件
- 由 schema 元数据生成空白配置样例
- 针对单个配置域做批量编辑

这些能力都以当前仓库实现为准，不再包含独立桌面工具路线或产品评估内容。

## 当前限制

- Schema 仍是受控子集，不等同于完整 JSON Schema
- 生成器与工具链都围绕静态内容配置设计，不覆盖玩家设置和通用持久化
- 工具链当前主形态是仓库内的 VS Code 扩展，而不是独立桌面应用

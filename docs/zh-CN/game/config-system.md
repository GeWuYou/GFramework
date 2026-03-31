# 游戏内容配置系统

> 面向静态游戏内容的 AI-First 配表方案

该配置系统用于管理怪物、物品、技能、任务等静态内容数据。

它与 `GFramework.Core.Configuration` 不同，后者面向运行时键值配置；它也不同于 `GFramework.Game.Setting`，后者面向玩家设置和持久化。

## 当前能力

- YAML 作为配置源文件
- JSON Schema 作为结构描述
- 一对象一文件的目录组织
- 运行时只读查询
- Source Generator 生成配置类型和表包装
- VS Code 插件提供配置浏览、raw 编辑、schema 打开和轻量校验入口

## 推荐目录结构

```text
GameProject/
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
├─ schemas/
│  ├─ monster.schema.json
│  └─ item.schema.json
```

## Schema 示例

```json
{
  "type": "object",
  "required": ["id", "name"],
  "properties": {
    "id": { "type": "integer" },
    "name": { "type": "string" },
    "hp": { "type": "integer" },
    "dropItems": {
      "type": "array",
      "items": { "type": "string" }
    }
  }
}
```

## YAML 示例

```yaml
id: 1
name: Slime
hp: 10
dropItems:
  - potion
  - slime_gel
```

## 运行时接入

当你希望加载后的配置在运行时以只读表形式暴露时，可以使用 `YamlConfigLoader` 和 `ConfigRegistry`：

```csharp
using GFramework.Game.Config;

var registry = new ConfigRegistry();

var loader = new YamlConfigLoader("config-root")
    .RegisterTable<int, MonsterConfig>(
        "monster",
        "monster",
        "schemas/monster.schema.json",
        static config => config.Id);

await loader.LoadAsync(registry);

var monsterTable = registry.GetTable<int, MonsterConfig>("monster");
var slime = monsterTable.Get(1);
```

这个重载会先按 schema 校验，再进行反序列化和注册。

## 运行时校验行为

绑定 schema 的表在加载时会拒绝以下问题：

- 缺失必填字段
- 未在 schema 中声明的未知字段
- 标量类型不匹配
- 数组元素类型不匹配

这样可以避免错误配置被默认值或 `IgnoreUnmatchedProperties` 静默吞掉。

## 开发期热重载

如果你希望在开发期修改配置文件后自动刷新运行时表，可以在初次加载完成后启用热重载：

```csharp
using GFramework.Game.Config;

var registry = new ConfigRegistry();
var loader = new YamlConfigLoader("config-root")
    .RegisterTable<int, MonsterConfig>(
        "monster",
        "monster",
        "schemas/monster.schema.json",
        static config => config.Id);

await loader.LoadAsync(registry);

var hotReload = loader.EnableHotReload(
    registry,
    onTableReloaded: tableName => Console.WriteLine($"Reloaded: {tableName}"),
    onTableReloadFailed: (tableName, exception) =>
        Console.WriteLine($"Reload failed: {tableName}, {exception.Message}"));
```

当前热重载行为如下：

- 监听已注册表对应的配置目录
- 监听该表绑定的 schema 文件
- 检测到变更后按表粒度重载
- 重载成功后替换该表在 `IConfigRegistry` 中的注册
- 重载失败时保留旧表，并通过失败回调提供诊断

这项能力默认定位为开发期工具，不承诺生产环境热更新平台语义。

## 生成器接入约定

配置生成器会从 `*.schema.json` 生成配置类型和表包装类。

通过已打包的 Source Generator 使用时，默认会自动收集 `schemas/**/*.schema.json` 作为 `AdditionalFiles`。

如果你在仓库内直接使用项目引用而不是打包后的 NuGet，请确认 schema 文件同样被加入 `AdditionalFiles`。

## VS Code 工具

仓库中的 `tools/vscode-config-extension` 当前提供以下能力：

- 浏览 `config/` 目录
- 打开 raw YAML 文件
- 打开匹配的 schema 文件
- 对必填字段、未知顶层字段、基础标量类型和标量数组元素做轻量校验
- 对顶层标量字段和顶层标量数组提供轻量表单入口

当前仍建议把复杂数组、嵌套对象和批量修改放在 raw YAML 中完成。

## 当前限制

以下能力尚未完全完成：

- 跨表引用校验
- 更完整的 JSON Schema 支持
- 更强的 VS Code 嵌套对象与复杂数组编辑器

因此，现阶段更适合作为你游戏项目的“受控试点配表系统”，而不是完全无约束的大规模内容生产平台。

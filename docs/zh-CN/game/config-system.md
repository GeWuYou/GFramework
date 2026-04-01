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
  "title": "Monster Config",
  "description": "定义怪物静态配置。",
  "type": "object",
  "required": ["id", "name"],
  "properties": {
    "id": {
      "type": "integer",
      "description": "怪物主键。"
    },
    "name": {
      "type": "string",
      "title": "Monster Name",
      "description": "怪物显示名。",
      "default": "Slime"
    },
    "hp": {
      "type": "integer",
      "default": 10
    },
    "rarity": {
      "type": "string",
      "enum": ["common", "rare", "boss"]
    },
    "dropItems": {
      "type": "array",
      "description": "掉落物品表主键。",
      "items": {
        "type": "string",
        "enum": ["potion", "slime_gel", "bomb"]
      },
      "x-gframework-ref-table": "item"
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
- 标量 `enum` 不匹配
- 标量数组元素 `enum` 不匹配
- 通过 `x-gframework-ref-table` 声明的跨表引用缺失目标行

跨表引用当前使用最小扩展关键字：

```json
{
  "type": "object",
  "required": ["id", "dropItemId"],
  "properties": {
    "id": { "type": "integer" },
    "dropItemId": {
      "type": "string",
      "x-gframework-ref-table": "item"
    }
  }
}
```

约束如下：

- 仅支持 `string`、`integer` 及其标量数组声明跨表引用
- 引用目标表需要由同一个 `YamlConfigLoader` 注册，或已存在于当前 `IConfigRegistry`
- 热重载中若目标表变更导致依赖表引用失效，会整体回滚受影响表，避免注册表进入不一致状态

当前还支持以下“轻量元数据”：

- `title`：供 VS Code 插件表单和批量编辑入口显示更友好的字段标题
- `description`：供表单提示、生成代码 XML 文档和接入说明复用
- `default`：供生成类型属性初始值和工具提示复用
- `enum`：供运行时校验、VS Code 校验和表单枚举选择复用

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
- 若变更表被其他表通过跨表引用依赖，会联动重验受影响表
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
- 对同一配置域内的多份 YAML 文件执行批量字段更新
- 在表单和批量编辑入口中显示 `title / description / default / enum / ref-table` 元数据

当前批量编辑入口适合对同域文件统一改动顶层标量字段和顶层标量数组；复杂数组、嵌套对象仍建议放在 raw YAML 中完成。

## 当前限制

以下能力尚未完全完成：

- 更完整的 JSON Schema 支持
- 更强的 VS Code 嵌套对象与复杂数组编辑器

因此，现阶段更适合作为你游戏项目的“受控试点配表系统”，而不是完全无约束的大规模内容生产平台。

## 独立 Config Studio 评估

当前阶段的结论是：`不建议立即启动独立 Config Studio`，继续以 `VS Code Extension` 作为主工具形态更合适。

当前不单独启动桌面版的原因：

- 当前已落地的能力主要仍围绕 schema 校验、轻量表单、批量编辑和 raw YAML 回退，这些都能在 VS Code 宿主里低成本迭代
- runtime、generator、tooling 之间仍在持续收敛 schema 子集和元数据语义，过早拆出桌面工具会放大版本协同成本
- 当前待补强点仍是更完整 schema 支持和复杂编辑体验，先在插件里验证真实工作流更稳妥
- 仓库当前的主要使用者仍偏开发者和技术策划，独立桌面版带来的“免开发环境”收益还不足以抵消额外维护面

只有在以下条件明显成立时，再建议启动独立 `Config Studio`：

- 主要使用者变成非开发人员，且 VS Code 安装与使用成本成为持续阻力
- 需要更重的表格视图、跨表可视化关系编辑、复杂审批流或离线发布流程
- 插件形态已经频繁受限于 VS Code Webview/Extension API，而不是 schema 与工作流本身
- 已经沉淀出稳定的 schema 元数据约定，能够支撑单独桌面产品的长期维护

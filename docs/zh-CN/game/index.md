---
title: Game 模块
description: GFramework.Game 运行时模块的入口、采用顺序与源码阅读导航。
---

# Game 模块

`Game` 栏目对应 `GFramework.Game` 与 `GFramework.Game.Abstractions` 这层游戏运行时能力。

它建立在 `Core` 之上，负责把“能运行的基础架构”继续扩展成“可落地的游戏项目基础设施”，重点覆盖静态内容配置、数据与存档、设置、场景与 UI 路由、序列化和文件存储。

## 模块与包关系

- `GeWuYou.GFramework.Game`
  - 游戏层默认运行时实现。
- `GeWuYou.GFramework.Game.Abstractions`
  - 对应的契约层。
- `GeWuYou.GFramework.Game.SourceGenerators`
  - 面向 `schemas/**/*.schema.json` 的配置类型与表包装生成器。
- `GeWuYou.GFramework.Core`
  - 本层直接依赖的底座，提供架构、生命周期、事件、日志等通用能力。

如果你的项目只想依赖契约，不想带入默认实现，请选择 `Game.Abstractions`。如果你需要可运行的默认基类与实现，请使用
`Game`。

## 栏目覆盖范围

`Game` 栏目聚焦“游戏项目该怎么把这些运行时能力接起来”，适合先按目标判断你要接入的是配置、设置与存档、Scene / UI 路由，还是文件存储与序列化。

当前栏目主要入口：

- 配置与内容系统
  - [配置系统](./config-system.md)
  - [VS Code 配置工具](./config-tool.md)
- 数据、设置、序列化与存储
  - [数据系统](./data.md)
  - [设置系统](./setting.md)
  - [序列化系统](./serialization.md)
  - [存储系统](./storage.md)
- 导航与界面
  - [场景系统](./scene.md)
  - [UI 系统](./ui.md)

## 最小接入路径

接入 `Game` 层前，先确保你已经完成 `Core` 的基础架构初始化。

最常见的四种接入目标如下：

### 1. 只想先拿到文件存储

```csharp
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

var serializer = new JsonSerializer();
IStorage storage = new FileStorage("GameData", serializer);
```

### 2. 接入设置与存档

典型顺序是：

1. 注册 `JsonSerializer`
2. 注册 `IStorage`
3. 注册 `ISettingsDataRepository`
4. 注册 `SettingsModel<TRepository>`
5. 注册 `SettingsSystem`
6. 注册 `ISaveRepository<TSaveData>`

具体组合方式见：

- [设置系统](./setting.md)
- [数据系统](./data.md)
- [存储系统](./storage.md)

### 3. 接入静态 YAML 配置

当你要维护怪物、物品、技能、任务等只读内容数据时，优先使用：

- `GFramework.Game` 运行时
- `GFramework.Game.SourceGenerators`
- `schemas/**/*.schema.json` + `config/**/*.yaml`

这条工作流的正式契约，以 `GFramework.Game` Runtime 和 `GFramework.Game.SourceGenerators` 当前共享支持的 schema
子集为准。`VS Code` 配置工具主要负责编辑期提示和表单辅助，不单独扩展运行时可接受的 schema 形状。

开始接入时，建议先把 schema 约束控制在共享子集内，并尽早确认像 `additionalProperties: false`（需显式设置为 `false`；省略或 `true` 视为非 `false`）这类已收口的对象边界，以及
`oneOf` / `anyOf` 当前会被直接拒绝，而不是在工具里看起来“可以先写”。如果你的配置模型需要更深层的嵌套数组、联合分支或其他超出共享子集的复杂
shape，优先回到 raw YAML 和 schema 设计本体处理，再决定是否拆分结构或调整约束方式。

完整约定见：

- [配置系统](./config-system.md)

### 4. 接入 Scene / UI 路由

`SceneRouterBase` 与 `UiRouterBase` 提供的是可复用基类，不直接绑定具体引擎。工厂、root、注册表通常由引擎适配层或项目自身提供。

入口见：

- [场景系统](./scene.md)
- [UI 系统](./ui.md)

## 阅读顺序

1. [Core 入口](../core/index.md)
2. [配置系统](./config-system.md)
3. [VS Code 配置工具](./config-tool.md)
4. [数据系统](./data.md)
5. [设置系统](./setting.md)
6. [场景系统](./scene.md)或[UI 系统](./ui.md)

## 源码与 API 阅读入口

如果你已经完成栏目入口页阅读，下一步通常不是看统计表，而是按模块角色回到源码和 XML 文档：

| 模块 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `GFramework.Game` | `YamlConfigLoader`、`SettingsModel<TRepository>`、`SceneRouterBase`、`UiRouterBase` | 默认运行时实现、配置加载、设置编排和路由基类 |
| `GFramework.Game.Abstractions` | `IConfigRegistry`、`ISaveRepository<TSaveData>`、`ISettingsSystem`、`ISceneRouter`、`IUiRouter` | 契约层边界，以及项目中哪些程序集只应依赖接口 |
| `GFramework.Game.SourceGenerators` | `SchemaConfigGenerator`、`ConfigSchemaDiagnostics` | schema 生成入口与诊断模型，确认配置系统的编译期链路 |

## 运行时与生成器如何配合

- 运行时入口主要来自 `GFramework.Game`
- 只依赖接口或拆分业务层时，补充 `GFramework.Game.Abstractions`
- 需要静态内容配置类型和表包装生成时，再追加 `GFramework.Game.SourceGenerators`
- 需要编辑器侧内容维护工作流时，再看 [VS Code 配置工具](./config-tool.md)，并把它视为共享契约之上的辅助层

## 对应模块入口

- [入门指南](../getting-started/index.md)
- [Game 抽象层说明](../abstractions/game-abstractions.md)
- [源码生成器总览](../source-generators/index.md)
- [API 参考入口](../api-reference/index.md)

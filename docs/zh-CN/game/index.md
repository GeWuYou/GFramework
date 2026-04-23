---
title: Game
description: GFramework.Game family 的运行时入口、采用顺序与 XML 阅读基线。
---

# Game

`Game` 栏目对应 `GFramework.Game` 与 `GFramework.Game.Abstractions` 这层游戏运行时能力。

它建立在 `Core` 之上，负责把“能运行的基础架构”继续扩展成“可落地的游戏项目基础设施”，重点覆盖静态内容配置、数据与存档、设置、场景与 UI 路由、序列化和文件存储。

## 先理解包关系

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

## 这个栏目应该回答什么

`Game` 栏目聚焦“游戏项目该怎么接这些运行时能力”，不再保留与当前实现脱节的泛化模块示例。

当前栏目主要入口：

- 配置与内容系统
  - [config-system](./config-system.md)
- 数据、设置、序列化与存储
  - [data](./data.md)
  - [setting](./setting.md)
  - [serialization](./serialization.md)
  - [storage](./storage.md)
- 导航与界面
  - [scene](./scene.md)
  - [ui](./ui.md)

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

- [setting](./setting.md)
- [data](./data.md)
- [storage](./storage.md)

### 3. 接入静态 YAML 配置

当你要维护怪物、物品、技能、任务等只读内容数据时，优先使用：

- `GFramework.Game` 运行时
- `GFramework.Game.SourceGenerators`
- `schemas/**/*.schema.json` + `config/**/*.yaml`

完整约定见：

- [config-system](./config-system.md)

### 4. 接入 Scene / UI 路由

`SceneRouterBase` 与 `UiRouterBase` 提供的是可复用基类，不直接绑定具体引擎。工厂、root、注册表通常由引擎适配层或项目自身提供。

入口见：

- [scene](./scene.md)
- [ui](./ui.md)

## 推荐阅读顺序

1. [Core 入口](../core/index.md)
2. [config-system](./config-system.md)
3. [data](./data.md)
4. [setting](./setting.md)
5. [scene](./scene.md) 或 [ui](./ui.md)

## Game Family XML 覆盖基线

下面这份 inventory 记录的是 `2026-04-23` 对 `Game` family 做的一轮轻量 XML 盘点结果：只统计公开 / 内部类型声明是否带 XML 注释，用来建立 README / landing / API 阅读链路；成员级 `param`、`returns`、`exception` 与生命周期说明仍需要后续波次继续细化。

| 模块 | 基线状态 | 代表类型 | 阅读重点 |
| --- | --- | --- | --- |
| `GFramework.Game` | `56/56` 个类型声明已带 XML 注释 | `YamlConfigLoader`、`SettingsModel<TRepository>`、`SceneRouterBase`、`UiRouterBase` | 先看运行时默认实现、配置加载、设置编排和路由基类 |
| `GFramework.Game.Abstractions` | `80/80` 个类型声明已带 XML 注释 | `IConfigRegistry`、`ISaveRepository<TSaveData>`、`ISettingsSystem`、`ISceneRouter`、`IUiRouter` | 再看契约层边界，决定项目哪些程序集只依赖接口 |
| `GFramework.Game.SourceGenerators` | `2/2` 个类型声明已带 XML 注释 | `SchemaConfigGenerator`、`ConfigSchemaDiagnostics` | 最后看 schema 生成入口与诊断模型，确认配置系统的编译期链路 |

## 与真实接法的关系

这个栏目以源码、`*.csproj`、模块 `README.md` 与 `ai-libs/` 下已验证的参考接法为准。

例如当前文档应优先和以下已验证事实保持一致：

- 配置系统采用 `YAML + JSON Schema + Source Generator`
- 设置持久化通常通过 `UnifiedSettingsDataRepository`
- 场景与 UI 路由依赖项目自己的 factory / root，而不是框架替你绑定引擎对象

如果某个旧页面与这些事实冲突，应以源码和模块 README 为准，并在同一轮里修正文档。

## 对应模块入口

- [`GFramework.Game/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game/README.md)
- [`GFramework.Game.Abstractions/README.md`](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Game.Abstractions/README.md)
- 仓库根 [`README.md`](https://github.com/GeWuYou/GFramework/blob/main/README.md)

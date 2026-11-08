---
title: Game 模块
description: 通过独立服务采用 UI、场景、设置、存档、输入和配置能力。
---

# Game 模块

`GFramework.Game` 提供游戏通用运行时，`GFramework.Game.Abstractions` 提供业务和宿主契约。普通服务通过构造注入工作，Architecture 可用于模块启动和停止编排。

## 按任务选择入口

| 任务 | 运行时入口 | 文档 |
| --- | --- | --- |
| 页面、窗口、对话框 | `UiService`、`UiRegistry` | [UI 系统](./ui.md) |
| 游戏场景导航 | `SceneRouter` | [场景系统](./scene.md) |
| 设置加载和应用 | `SettingsModel<TRepository>`、`SettingsSystem` | [设置系统](./setting.md) |
| 玩家数据和进度 | `DataRepository`、`SaveRepository<T>` | [数据与存档](./data.md) |
| 绑定、重绑定与 UI 动作 | `InputBindingStore`、`UiInputDispatcher` | [输入系统](./input.md) |
| 文件与序列化 | `FileStorage`、`ScopedStorage`、`JsonSerializer` | [存储](./storage.md)、[序列化](./serialization.md) |
| 静态 YAML 内容 | `GameConfigBootstrap`、`YamlConfigLoader` | [配置系统](./config-system.md) |
| DLC 与资料型模组 | `ContentPackDiscovery`、`ContentPackResolver`、`ContentCatalogLoader` | [内容包](./content-packs.md) |
| DLL 模组与方法补丁 | `GameModHost`、可选 `HarmonyModExtension` | [代码模组与 Harmony](./code-mods.md) |

## 最小采用

只使用某项能力时，直接构造对应服务并提供其依赖。例如，文件存储和存档无需先启动 Architecture：

```csharp
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

using var storage = new FileStorage("GameData", new JsonSerializer());
var saves = new SaveRepository<PlayerSave>(storage,
    new SaveConfiguration { SaveRoot = "saves" },
    new SaveRepositoryOptions<PlayerSave>
    {
        GameId = "my-game",
        CurrentVersion = 1
    });
```

`PlayerSave` 是实现 `IData` 的项目 DTO，完整采用见 [存档教程](../tutorials/save-system.md)。

Scene 与 UI 额外接收宿主挂载和线程调度依赖。标准 DI 的 `AddGamePause`、`AddGameUi`、`AddGameScenes` 位于 `GFramework.Game.Runtime`；需要可选架构编排时安装 `GameRuntimeModule(dispatcher, configure)`。

关闭时，业务和 UI／Scene 服务先停止，原生 root、dispatcher 和底层存储随后释放。类型／工厂创建的服务由 provider 拥有，外部实例由调用方拥有。

## 配置和生成器

运行时配置采用 `Game`；schema 生成按需安装 `Game.SourceGenerators`，使用 `schemas/**/*.schema.json` 和 `config/**/*.yaml`。对象边界必须显式设置 `additionalProperties: false`，组合分支等超出共享子集的结构会被拒绝。配置工具遵守同一共享契约，见 [配置系统](./config-system.md) 与 [配置编辑工具](./config-tool.md)。

普通 UI 采用强类型路由和显式工厂，场景使用默认导航服务。节点与信号生成器按项目需要选择。

## 可运行工程

- [官方素材小游戏](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)：玩法、UI、暂停、设置、存档和继续。
- [Godot 运行时示例](../godot/runtime-sample.md)：配置、输入和原生宿主接线。
- [Game 抽象接口](../abstractions/game-abstractions.md)
- [源码生成器](../source-generators/index.md)

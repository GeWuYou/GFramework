---
title: Game 抽象层
description: GFramework.Game.Abstractions 的契约边界、包关系与源码阅读重点。
---

# Game 抽象层

`GFramework.Game.Abstractions` 是 `Game` 运行时的契约包。

它建立在 `GFramework.Core.Abstractions` 之上，负责定义配置、数据、设置、场景、UI、路由、存储和资源注册表相关的接口、
枚举与事件契约；默认实现、路由基类、YAML 加载器、文件存储和设置 / 存档仓库则在 `GFramework.Game` 中。

如果你要开箱即用地接入游戏运行时能力，应依赖 `GFramework.Game`；如果你在做共享业务层、feature 包、测试替身或引擎适配层，
才单独消费本包。

## 什么时候单独依赖它

- 你希望公共业务层只依赖 `ISceneRouter`、`IUiRouter`、`ISettingsSystem`、`ISaveRepository<TSaveData>` 这类契约
- 你要让多个程序集共享 `ISettingsData`、`IData`、`ISceneEnterParam`、`IUiPageEnterParam` 等数据和路由上下文
- 你需要自己实现 factory、root、存储或配置加载器，但不想把默认运行时一起带进来

## 包关系

- 契约层：`GFramework.Game.Abstractions`
- 运行时实现：`GFramework.Game`
- 底层基础契约：`GFramework.Core.Abstractions`

## 契约地图

| 契约族 | 作用 |
| --- | --- |
| `Config/` | `IConfigLoader`、`IConfigRegistry`、`IConfigTable<TKey, TValue>` 和配置失败诊断模型 |
| `Data/` | `IData`、`IVersionedData`、`IDataRepository`、`ISettingsDataRepository`、`ISaveRepository<TSaveData>` 及数据事件 |
| `Setting/` | `ISettingsData`、`ISettingsModel`、`ISettingsSystem`、设置迁移契约与内置设置数据类型 |
| `Scene/` | `IScene`、`ISceneRouter`、`ISceneFactory`、`ISceneRoot`、转场处理器与事件 |
| `UI/` | `IUiPage`、`IUiRouter`、`IUiFactory`、`IUiRoot`、交互配置与 UI 转场选项 |
| `Routing/` | `IRoute`、`IRouteContext`、`IRouteGuard<TRoute>`，作为 Scene / UI 共享的路由基础约定 |
| `Storage/` `Asset/` `Enums/` | 文件存储角色、资源注册表，以及转场 / UI 层级 / 输入动作等跨层枚举 |

## 契约族阅读入口

如果你要回到源码 XML 文档确认契约，请优先看下面这些族群：

| 契约族 | 代表类型 | 建议先确认什么 |
| --- | --- | --- |
| `Config/` | `IConfigLoader`、`IConfigRegistry`、`IConfigTable<TKey, TValue>`、`ConfigLoadException` | 配置表注册、只读访问和失败诊断边界 |
| `Data/` | `IDataRepository`、`ISettingsDataRepository`、`ISaveRepository<TSaveData>`、`DataRepositoryOptions` | 业务数据、统一设置文件、槽位存档与迁移契约 |
| `Setting/` | `ISettingsData`、`ISettingsModel`、`ISettingsSystem`、`LocalizationSettings` | 设置生命周期、应用语义、迁移接口和内置设置对象 |
| `Scene/` | `IScene`、`ISceneRouter`、`ISceneFactory`、`SceneTransitionEvent` | 场景行为、工厂 / root 边界和转场模型 |
| `UI/` | `IUiPage`、`IUiRouter`、`IUiFactory`、`UiInteractionProfile`、`UiTransitionHandlerOptions` | 页面栈、层级 UI、输入动作和 UI 转场契约 |
| `Routing/` `Storage/` `Asset/` `Enums/` | `IRoute`、`IRouteContext`、`IFileStorage`、`IAssetRegistry<T>`、`UiLayer`、`SceneTransitionType` | 公共路由上下文、存储角色、资源注册表与共享枚举 |

## 最小接入路径

### 1. 只在公共业务层声明游戏对象

```csharp
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;

public sealed class GameSaveData : IVersionedData
{
    public int Version { get; set; } = 1;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public sealed class GameplayEnterParam : ISceneEnterParam
{
    public required string Seed { get; init; }
}

public sealed class PauseMenuParam : IUiPageEnterParam
{
    public bool AllowRestart { get; init; }
}
```

### 2. 让 feature 包只依赖抽象

```csharp
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Scene;

public sealed class ContinueGameCommandHandler
{
    private readonly ISaveRepository<GameSaveData> _saveRepository;
    private readonly ISceneRouter _sceneRouter;

    public ContinueGameCommandHandler(
        ISaveRepository<GameSaveData> saveRepository,
        ISceneRouter sceneRouter)
    {
        _saveRepository = saveRepository;
        _sceneRouter = sceneRouter;
    }
}
```

### 3. 什么时候切到运行时包

下面这些需求都属于 `GFramework.Game` 的职责，而不是本包：

- 使用默认的 `JsonSerializer`、`FileStorage` 或 `ScopedStorage`
- 使用 `SettingsModel<TRepository>`、`SettingsSystem`、`SaveRepository<TSaveData>` 等默认实现
- 使用 `YamlConfigLoader`、`GameConfigBootstrap`、`GameConfigModule`
- 继承 `SceneRouterBase`、`UiRouterBase` 或默认转场处理器基类

## 阅读顺序

1. 先读本页，确认你是否真的只需要契约层
2. 再看 [Game 模块总览](../game/index.md) 了解默认运行时怎么组织这些契约
3. 继续读具体专题页：
   - [配置系统](../game/config-system.md)
   - [数据系统](../game/data.md)
   - [设置系统](../game/setting.md)
   - [场景系统](../game/scene.md)
   - [UI 系统](../game/ui.md)
4. 需要统一入口时，回到：
   - [Game 模块总览](../game/index.md)
   - [入门指南](../getting-started/index.md)

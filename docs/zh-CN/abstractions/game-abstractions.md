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

## 配置契约的采用边界

如果你只依赖 `GFramework.Game.Abstractions`，需要额外记住一件事：这里的 `Config/` 只定义“如何注册与访问配置表”的读取契约，不定义
AI-First 配置工作流的完整实现边界。

与配置相关的实际采用路径仍然要回到 `GFramework.Game`：

- `YamlConfigLoader`、`GameConfigBootstrap`、`GameConfigModule` 等实现都在 `GFramework.Game`
- `GFramework.Game.SourceGenerators` 生成的配置类型，服务的是与 Runtime 对齐的共享 schema 子集
- 共享子集之外的复杂 schema 设计，不会因为你只依赖 abstractions 就自动获得额外支持

这意味着，如果你的 schema 依赖下面这些能力，就不能只停留在 abstractions 视角理解配置契约：

- `oneOf`、`anyOf` 这类复杂组合关键字
- 非 `false` 的 `additionalProperties`
- 其他会引入开放对象形状、联合分支或属性合并漂移的 schema 设计

这些边界由 `GFramework.Game` 与 [配置系统](../game/config-system.md) 负责说明和落地；`GFramework.Game.Abstractions` 本身不重新定义它们。

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
- 需要确认 AI-First 配置工作流当前支持的共享 schema 子集，以及 `oneOf` / `anyOf`、非 `false` `additionalProperties` 等不在采用路径内的边界

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

如果你的关注点是配置契约，请把 [配置系统](../game/config-system.md) 当作下一跳，而不是停留在 abstractions 页面对支持边界做推断。

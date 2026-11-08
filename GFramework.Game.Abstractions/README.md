# GFramework.Game.Abstractions

Game 的引擎无关契约，包括强类型 UI、场景、输入、设置、文档存档、代码模组和配置。接口与数据定义放在此包；运行实现位于 `GFramework.Game`，Godot 原生实现位于 `GFramework.Godot`。

## 子系统

| 目录 | 主要入口 | 责任 |
| --- | --- | --- |
| `UI/` | `IUiService`、`UiRoute<TArgs>`、`UiDialogRoute<TArgs, TResult>`、`IUiView`、`IUiHost` | 导航、参数、业务结果和宿主视图租约 |
| `Runtime/` | `IGameDispatcher` | 显式宿主调度 |
| `Scene/` | `ISceneRouter`、`ISceneFactory`、`ISceneRoot`、`ISceneBehavior` | 场景导航和生命周期 |
| `Input/` | `IInputBindingStore`、`IUiInputDispatcher` | 动作绑定与异步语义分发 |
| `Data/` | `IDataRepository`、`ISaveRepository<T>`、`ISettingsDataRepository` | 持久化与槽位存档 |
| `Setting/` | `ISettingsModel`、`ISettingsSystem`、`ISettingsData` | 设置加载、迁移、应用和重置 |
| `Config/` | `IConfigRegistry`、配置表契约 | 静态内容配置 |
| `Modding/` | `IGameMod`、`IGameModContext` | 游戏定义 API 的代码扩展、宿主调度和资源责任 |
| `Content/` | `ContentId`、`ContentPackManifest`、`ContentSnapshot` | 稳定内容身份、包依赖、玩家顺序和存档内容快照 |
| `Storage/`、`Asset/`、`Routing/` | 存储、资源登记、场景守卫契约 | 相邻能力的最小公共边界 |

## 最小采用

业务程序集用 `UiRoute<TArgs>` 声明路由，调用方依赖 `IUiService`。视图实现放在宿主或项目展示层；业务参数无需携带原生节点。UI 句柄标识具体实例，路由选项独立描述显示、输入、暂停和多实例。

Scene、数据和设置服务不继承 System／Model／Utility 角色；宿主通过构造注入提供实现并决定启动与停止顺序。此包仍引用 Core 契约，使用暂停等公共值类型。

## 文档

- [Game 抽象接口](../docs/zh-CN/abstractions/game-abstractions.md)
- [Game 概览](../docs/zh-CN/game/index.md)
- [UI 系统](../docs/zh-CN/game/ui.md)
- [场景系统](../docs/zh-CN/game/scene.md)
- [DLC 与资料型模组](../docs/zh-CN/game/content-packs.md)
- [代码模组与 Harmony](../docs/zh-CN/game/code-mods.md)
- [Game 运行时](../GFramework.Game/README.md)

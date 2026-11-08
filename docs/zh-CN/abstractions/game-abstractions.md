---
title: Game 抽象接口
description: 游戏通用服务、强类型路由、宿主视图与持久化契约的采用入口。
---

# Game 抽象接口

`GFramework.Game.Abstractions` 定义游戏通用契约和数据类型。实现由 `GFramework.Game`、引擎适配层或项目提供。业务模块可依赖契约而保持原生视图在宿主层。

## 契约地图

| 目录 | 代表入口 | 责任 |
| --- | --- | --- |
| `UI/` | `IUiService`、`UiRoute<TArgs>`、`UiDialogRoute<TArgs, TResult>` | 页面、窗口与业务结果 |
| `UI/` 宿主部分 | `IUiView`、`IUiHost`、`IUiActionTarget`、`IUiGuard` | 租约、挂载、动作和守卫 |
| `Runtime/` | `IGameDispatcher` | 宿主回调调度 |
| `Scene/` | `ISceneRouter`、`ISceneFactory`、`ISceneRoot`、`ISceneBehavior` | 场景生命周期和导航 |
| `Data/` | `IData`、`IDataLocation`、`IDataRepository`、`ISaveRepository<T>` | 数据位置和持久化 |
| `Modding/` | `IGameMod`、`IGameModContext` | 显式授权的代码扩展与宿主生命周期 |
| `Setting/` | `ISettingsModel`、`ISettingsSystem`、`ISettingsData` | 设置、迁移和应用 |
| `Input/` | `IInputBindingStore`、`IUiInputDispatcher` | 动作绑定与语义分发 |
| `Config/` | `IConfigRegistry`、配置表契约 | 静态内容加载 |
| `Routing/`、`Storage/`、`Asset/` | 场景守卫、存储和资源表 | 相邻能力的共享边界 |

## UI 使用方式

业务定义 `UiRoute<TArgs>`，使用强类型参数调用 `IUiService`；宿主把相同定义登记到 `UiRegistry`。具体实例用 `UiInstanceHandle` 标识，关闭通过句柄或 `UiViewContext` 完成。对话框用 `UiDialogContext<TResult>` 提交业务值。

`UiRouteOptions` 将显示层、模态、世界输入阻断、暂停和多实例分别声明。`UiSnapshot` 是可安全检查的状态副本，业务无需访问原生节点来判断输入阻断。

## 独立服务和所有权

Scene、UI、数据、设置和暂停采用普通服务契约。异步接口使用 `Async` 后缀和最后一个 `CancellationToken`；导航结果区分应用、拒绝和无变化。服务停止完成后才可释放它使用的宿主对象。

本包依赖 Core 契约以共享暂停等基础类型。运行时、原生引擎和源码生成器各自维护实现边界。

## 相关主题

- [Game 概览](../game/index.md)
- [UI 系统](../game/ui.md)
- [场景系统](../game/scene.md)
- [设置系统](../game/setting.md)
- [数据与存档](../game/data.md)

- [代码模组与 Harmony](../game/code-mods.md)

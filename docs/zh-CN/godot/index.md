---
title: Godot 运行时集成
description: 将通用服务接入原生节点、场景树、InputMap、暂停和存储。
---

# Godot 运行时集成

`GFramework.Godot` 将通用能力接到 Godot 宿主。Godot 继续拥有主循环，UI 和 Scene 服务通过显式 dispatcher 访问原生对象。完整玩法工程见 [官方素材小游戏](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)。

## 能力入口

| 任务 | 原生入口 | 文档 |
| --- | --- | --- |
| 页面、菜单和对话框 | `GodotUiRoot`、`GodotUiView`、注册扩展 | [UI 系统](./ui.md) |
| 场景创建与导航 | `GodotSceneFactory`、`SceneBehaviorFactory` | [场景系统](./scene.md) |
| InputMap 和重绑定 | `GodotInputBindingStore` | [输入](./input.md) |
| 场景树暂停 | `GodotPauseHandler` | [暂停](./pause.md) |
| 用户数据 | `GodotFileStorage` | [存储](./storage.md) |
| 音频、图形和语言 | Godot 设置应用器 | [设置](./setting.md) |
| YAML 配置 | `GodotYamlConfigLoader` | [配置系统](../game/config-system.md) |
| 可选架构编排 | `AbstractArchitecture` | [架构集成](./architecture.md) |

## 采用路径

从[Godot 接入教程](../tutorials/godot-integration.md)运行精简场景，再按功能查阅本页的能力入口。教程引用示例工程中的实际源码。

安装 `GeWuYou.GFramework.Godot`，按项目需要选择 `Core`、`Game` 和契约包。普通 UI 登记 `Control` 场景，使用默认 root 和强类型 UI 服务；场景导航直接构造 `SceneRouter` 并传入工厂、root 和宿主调度器。

节点注入、信号接线和 `project.godot` 常量按需安装 `GeWuYou.GFramework.Godot.SourceGenerators`。运行时包不生成这些辅助代码，普通 UI 接入无需生成器。

使用 Architecture 时，在配置阶段声明模块，通过 `InitializeAsync` 启动，`GameRuntimeModule(dispatcher, configure)` 将普通服务的停止接入原生宿主分离之前。项目也可直接组合服务，采用路径见 [Godot UI](./ui.md)。

## 原生生命周期

按钮和输入产生的异步任务由宿主统一观察。退出时关闭新的业务入口，等待启动和在途操作，再停止 UI／Scene、释放暂停与存储，最后退出场景树。

`GodotUiRoot` 管理挂载和焦点，视图租约拥有创建的节点；场景适配器按场景生命周期释放资源。`QueueFree` 排队完成原生释放，后续流程应等待必要的引擎帧。

## 示例与相邻能力

- [运行时示例](./runtime-sample.md)
- [Godot 完整项目教程](../tutorials/godot-complete-project.md)
- [节点扩展](./extensions.md)
- [信号](./signal.md)
- [协程](./coroutine.md)
- [资源](./resource.md)
- [日志](./logging.md)
- [源码生成器](../source-generators/index.md)

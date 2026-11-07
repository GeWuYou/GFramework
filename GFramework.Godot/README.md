# GFramework.Godot

将 GFramework 通用运行时接入 Godot 的节点、场景树、InputMap、资源、暂停和文件路径。引擎继续拥有原生主循环，框架服务使用显式宿主调度。

## 相邻模块

- `Game` 提供 `UiService`、`SceneRouter`、设置、存档和配置运行时。
- `Core` 提供标准 DI、日志、暂停和可选 Architecture 编排。
- `Godot.SourceGenerators` 按需生成节点注入、信号、场景包装和项目元数据；普通 UI 直接登记 `Control` 场景。

## 子系统

| 目录 | 主要入口 | 作用 |
| --- | --- | --- |
| `UI/` | `GodotUiRoot`、`GodotUiView`、注册扩展、`GodotUiInputBridge` | 原生挂载、焦点、模态与异步动作 |
| `Scene/` | `GodotSceneFactory`、`GodotSceneRegistry`、`SceneBehaviorFactory` | 原生场景创建和生命周期 |
| `Architectures/` | `AbstractArchitecture` | 可选引擎宿主编排 |
| `Input/` | `GodotInputBindingStore` | InputMap 绑定和快照 |
| `Pause/` | `GodotPauseHandler` | 暂停组与场景树连接 |
| `Storage/` | `GodotFileStorage` | `user://` 持久化与原子写入 |
| `Setting/` | 音频、图形、语言应用器 | 将设置数据应用到引擎 |
| `Config/`、`Resource/` | Godot 配置与资源适配 | 项目资源和内容加载 |
| `Coroutine/`、`Extensions/`、`Signal/`、`Logging/`、`Text/` | 协程、节点与日志等扩展 | 保留的引擎能力 |

## 最小采用

在 Godot 主线程挂载 `GodotUiRoot`，捕获宿主同步上下文创建 `SynchronizationContextGameDispatcher`，通过 `UiRegistry.RegisterGodot` 或 `RegisterGodotScene` 登记普通 `Control`。创建 `UiService` 后，即可通过共享强类型路由打开页面和窗口。

场景使用显式注册表构造 `GodotSceneFactory`，再创建 `SceneRouter(factory, root, dispatcher)`。需要暂停的 UI 共享一个 `PauseStackManager`，通过 `GodotPauseHandler` 接到场景树。

### 关闭顺序

观察按钮、输入和启动任务的错误。退出时关闭新的业务入口、等待在途操作，停止 UI 和场景服务，再释放暂停、存储与原生 root。使用 Architecture 时，`GameRuntimeModule(dispatcher, configure)` 将普通服务停止接入宿主分离之前的阶段。

## 可运行工程与文档

- [官方素材小游戏](../Samples/DodgeTheCreeps/README.md)：实际玩法、强类型菜单、暂停、设置和保存继续。
- [Godot 运行时示例](../Samples/GodotIntegration/README.md)：配置、输入、场景、协程与持久化接入。
- [Godot 概览](../docs/zh-CN/godot/index.md)
- [UI 系统](../docs/zh-CN/godot/ui.md)
- [场景系统](../docs/zh-CN/godot/scene.md)
- [输入](../docs/zh-CN/godot/input.md)
- [暂停](../docs/zh-CN/godot/pause.md)
- [设置](../docs/zh-CN/godot/setting.md)
- [存储](../docs/zh-CN/godot/storage.md)

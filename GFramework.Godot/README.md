# GFramework.Godot

将 GFramework 通用运行时接入 Godot 的节点、场景树、InputMap、资源、暂停和文件路径。引擎继续拥有原生主循环，框架服务使用显式宿主调度。

Godot 不传递引用 CQRS。需要消息分发时，在消费工程显式安装 `GFramework.Cqrs`；协程与普通宿主接入无需该依赖。

## 相邻模块

- `Game` 提供 `UiService`、`SceneRouter`、设置、存档和配置运行时。
- `Core` 提供标准 DI、日志、暂停和可选 Architecture 编排。
- `Game` 的 DLL 模组宿主通过显式 dispatcher 使用 Godot 主线程；需要方法补丁时另引 `Game.Harmony`。
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
| `Content/` | 内容资源会话、官方 PCK 信任策略、资料素材加载 | 启动时挂载可信 DLC，隔离第三方数据与素材 |
| `Coroutine/` | `Timing`、节点作用域、`SwitchTo` | 统一阶段推进、场景暂停与父子任务清理 |
| `Extensions/`、`Signal/`、`Logging/`、`Text/` | 节点与日志等扩展 | 引擎能力 |

## 最小采用

在 Godot 主线程挂载 `GodotUiRoot`，捕获宿主同步上下文创建 `SynchronizationContextGameDispatcher`，通过 `UiRegistry.RegisterGodot` 或 `RegisterGodotScene` 登记普通 `Control`。创建 `UiService` 后，即可通过共享强类型路由打开页面和窗口。

场景使用显式注册表构造 `GodotSceneFactory`，再创建 `SceneRouter(factory, root, dispatcher)`。需要暂停的 UI 共享一个 `PauseStackManager`，通过 `GodotPauseHandler` 接到场景树。

协程通过 `node.RunCoroutine(...)` 进入节点默认 Supervisor 容器；任务内部子流程默认 FailFast。需要整体失败的多任务操作时，用 `node.CreateCoroutineScope(CoroutineFailurePolicy.FailFast)`。阶段切换保持同一句柄及暂停策略，节点退树取消其全部任务与后代。

### 关闭顺序

观察按钮、输入和启动任务的错误。退出时关闭新的业务入口、等待在途操作，停止玩法、UI 和场景服务，再停止代码模组及其补丁和订阅，最后释放暂停、存储与原生 root。使用 Architecture 时，`GameRuntimeModule(dispatcher, configure)` 将普通服务停止接入宿主分离之前的阶段。

`AbstractArchitecture` 已实现 Core 的显式异步宿主契约，使用 `InitializeAsync`；派生类继续覆盖适配层的 `OnStartedAsync`。直接继承 Core `Architecture` 的异步组合根改为实现 `IAsyncArchitectureHost.StartHostAsync`，并自行通过 dispatcher 接线原生对象。

## 可运行工程与文档

- [官方素材小游戏](../Samples/DodgeTheCreeps/README.md)：实际玩法、强类型菜单、暂停、设置和保存继续。
- [Godot 运行时示例](../Samples/GodotIntegration/README.md)：配置、输入、场景、协程与持久化接入。
- [Godot 概览](../docs/zh-CN/godot/index.md)
- [UI 系统](../docs/zh-CN/godot/ui.md)
- [场景系统](../docs/zh-CN/godot/scene.md)
- [输入](../docs/zh-CN/godot/input.md)
- [暂停](../docs/zh-CN/godot/pause.md)
- [协程与作用域](../docs/zh-CN/godot/coroutine.md)
- [设置](../docs/zh-CN/godot/setting.md)
- [存储](../docs/zh-CN/godot/storage.md)
- [DLC 与资料型模组](../docs/zh-CN/game/content-packs.md)
- [代码模组与 Harmony](../docs/zh-CN/game/code-mods.md)

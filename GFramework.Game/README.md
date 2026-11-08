# GFramework.Game

游戏通用运行时，提供强类型 UI、场景导航、输入绑定、配置、设置、文档存档、代码模组、序列化和存储。采用构造注入，可直接创建或使用标准 DI；Architecture 为可选模块编排。

## 包与相邻模块

- 依赖 `Core` 与 `Game.Abstractions`，使用标准 DI、日志和 Core 基础能力。
- `Game.Abstractions` 定义业务和宿主契约，业务程序集可只引用契约。
- `Godot` 提供原生 UI root、视图租约、场景适配、输入、暂停和存储。
- `Game.Harmony` 提供可选的方法补丁集成，Game 本身不引用 Harmony。
- `Game.SourceGenerators` 按 schema 生成配置类型；普通 UI 和场景采用无需生成器。

## 子系统

| 目录 | 主要入口 | 作用 |
| --- | --- | --- |
| `UI/` | `UiService`、`UiRegistry` | 页面历史、窗口、强类型参数和对话框结果 |
| `Scene/` | `SceneRouter` | 显式工厂与宿主驱动的场景导航 |
| `Runtime/` | `GameServiceCollectionExtensions`、`GameRuntimeModule`、dispatcher | 标准服务登记、可选编排和宿主线程 |
| `Input/` | `InputBindingStore`、`UiInputDispatcher` | 绑定快照和异步 UI 动作分发 |
| `Config/` | `GameConfigBootstrap`、`YamlConfigLoader`、`ConfigRegistry` | 只读内容配置与热重载 |
| `Content/` | `ContentPackDiscovery`、`ContentPackResolver`、`ContentCatalogLoader` | DLC／资料模组发现、依赖排序、字段补丁与来源追踪 |
| `Data/` | `SaveRepository<T>`、`SaveDocument`、`SaveMigrationGraph`、`ModSaveSession` | 文档优先的双向迁移、内容修复、模组状态与槽位持久化 |
| `Modding/` | `GameModHost`、`GameModHostOptions` | 显式授权的 DLL 加载、共享契约和生命周期 |
| `Setting/` | `SettingsModel<TRepository>`、`SettingsSystem` | 数据迁移、应用和保存 |
| `Serializer/`、`Storage/` | `JsonSerializer`、`FileStorage`、`ScopedStorage` | 序列化、原子文件写入和键空间 |

## 最小采用

UI 通过共享 `UiRoute<TArgs>` 定义和 `UiRegistry` 工厂登记，再创建 `UiService(routes, host, dispatcher, pause)`。页面使用 `PushAsync`／`ReplaceAsync`，独立窗口使用 `ShowAsync`，实例通过句柄或视图上下文关闭。对话框通过 `UiDialogRoute<TArgs, TResult>` 返回业务结果。

场景可直接创建 `SceneRouter(factory, root, dispatcher)`。设置和存档同样通过构造函数接收存储等依赖，不要求组件角色或上下文绑定。

标准 DI 提供三个接线入口：

```csharp
services.AddGamePause();
services.AddGameUi(routes, uiHost, dispatcher);
services.AddGameScenes(sceneFactory, sceneRoot, dispatcher);
```

这些扩展位于 `GFramework.Game.Runtime`。服务由 provider 拥有，传入的宿主对象仍由调用方拥有。使用 Architecture 时，将相同登记放入 `new GameRuntimeModule(dispatcher, services => { ... })`，保证业务停止先于原生宿主分离。

## 生命周期

所有异步调用遵循 `Async` 后缀和末尾 `CancellationToken`。UI 与 Scene 的操作结果区分应用、拒绝和无变化，异常向调用方传播。停止先关闭接纳、等待在途工作，再完成清理；宿主必须保持原生 root 和调度器存活。

设置加载先准备完整迁移结果，再更新现有数据；失败保留当前数据并报告错误。保存失败向调用方传播，已接纳的底层写入等待到完成。数据仓库与设置沿用各自格式；槽位存档统一使用 `SaveDocument`，版本和内容元数据不再依赖业务 DTO。`PrepareLoadAsync` 准备独立候选结果，`CommitPreparedAsync` 备份并提交，普通读取不自动回写。

## 文档和可运行工程

- [Game 概览](../docs/zh-CN/game/index.md)
- [UI 系统](../docs/zh-CN/game/ui.md)
- [场景系统](../docs/zh-CN/game/scene.md)
- [设置系统](../docs/zh-CN/game/setting.md)
- [数据与存档](../docs/zh-CN/game/data.md)
- [内容配置](../docs/zh-CN/game/config-system.md)
- [DLC 与资料型模组](../docs/zh-CN/game/content-packs.md)
- [代码模组与 Harmony](../docs/zh-CN/game/code-mods.md)
- [官方素材小游戏](../Samples/DodgeTheCreeps/README.md)

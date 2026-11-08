# Godot runtime sample

这是一个可由 Godot 打开的 2D 工程：移动蓝色圆点收集金色圆点，通过菜单操作设置和一个存档槽位。画面由代码绘制，无外部素材依赖。

## 环境与启动

- Godot **4.7.2 .NET** 版本，与项目的 `Godot.NET.Sdk` 和框架的 `GodotSharp` 版本一致。
- .NET SDK，能够构建 `net8.0` 消费项目及仓库内的生成器项目。
- 首次还原需要访问项目使用的 NuGet 源。

在仓库根目录执行：

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --editor --path Samples/GodotIntegration
```

在编辑器中运行主场景，或使用同一 Godot .NET 可执行文件运行：

```bash
godot --path Samples/GodotIntegration
```

`godot` 表示已安装的 Godot .NET 可执行文件；如果它未加入 PATH，替换为该文件的实际路径。首次打开可能需要导入和构建。Release 编译入口为：

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Release
```

工程保留独立的 `GodotIntegration.sln`，供 Godot 原生构建与导出器按程序集名称找到解决方案。它只列出样例项目；框架依赖通过项目引用构建。`Debug`、`Release`、`ExportDebug` 和 `ExportRelease` 配置均显式映射到样例，后两项使用 Godot SDK 提供的导出设置。不要用仓库根解决方案替代或删除这份文件。

## 操作

- WASD 移动，接近金色圆点后计数加一。
- 默认 Escape 打开暂停菜单，再按一次关闭顶层菜单。
- 暂停菜单可恢复、打开设置、保存槽位 1，或返回主菜单。
- 设置支持显示网格、重绑定暂停键、恢复默认暂停键，以及保存设置。选择重绑定后按一个键，再点击“Save settings”持久化。
- “New game”创建未保存的新进度；只有明确保存时才覆盖槽位 1。“Continue slot 1”读取上次保存的坐标和收集数。
- 返回主菜单后再继续，或退出后重新打开工程再继续，均读取同一个槽位。保存失败会显示错误并保留当前游戏，读取失败不会自动覆盖损坏数据。

暂停期间移动与世界协程都停止；设置菜单叠加在暂停菜单上时，两者分别持有暂停请求，关闭设置后仍保持暂停。窗口关闭和 Quit 按钮保留首次退出请求：取消并等待启动、等待当前保存或导航完成，再等待架构销毁并退出场景树。结束编辑器进程或强制终止不会自动保存游戏。

## 接线与数据

| 入口 | 接入能力 |
| --- | --- |
| `DemoRoot` | 项目场景根、默认 UI 宿主接入、异步按钮错误处理和关闭流程 |
| `DemoArchitecture` | 通过 GameRuntimeModule 安装标准 DI 服务，协调原生宿主停止 |
| `DemoSceneFactory` | 构造带项目依赖的原生世界，由默认 SceneRouter 导航 |
| `DemoUiRoutes` / `DemoPanel` | 强类型路由集中声明显示、输入和暂停策略；普通 Control 只负责视图 |
| `DemoWorld` | 原生帧更新、节点归属协程与暂停 |
| `schemas/` / `config/` | 配置类型生成和 Godot 编辑器/导出态 YAML 加载 |

[界面定义](DemoUiRoutes.cs) 使用带标题参数的强类型路由。菜单是页面，暂停和设置是独立窗口；两个窗口各自持有暂停请求。所有视图在一处使用 RegisterGodot 注册，并由默认 GodotUiRoot 挂载。实例按钮通过 UiViewContext.CloseAsync 关闭自己，默认 GodotUiInputBridge 把未处理的返回键交给 UI 服务。窗口顺序和返回目标由服务管理。

[架构接线](DemoArchitecture.cs) 在配置阶段使用 AddGamePause、AddGameUi 和 AddGameScenes 注册普通服务；启动后通过 GetService 解析 IUiService、ISceneRouter 和 IPauseStackManager。GameRuntimeModule 等待 UI 和场景停止，再交由 Godot 宿主分离节点。

项目文件显式引用 `Game.Abstractions`，并通过 analyzer 引用和生成器 targets 加载 `gameplay.schema.json`。生成的 `GameplayConfig` 提供移动速度；修改 `config/gameplay/default.yaml` 后重新启动即可读取。

存档使用 `SaveRepository<DemoSave>`；设置使用 `ISettingsData` 和 `UnifiedSettingsDataRepository`，输入绑定通过 `GodotInputBindingStore` 快照存取。设置仓库采用显式类型登记，不启用任意多态类型还原。

- 游戏进度：`user://runtime_sample/saves/slot_1/save.json`
- 设置与按键快照：`user://runtime_sample/settings.json`
- 导出态配置缓存：`user://runtime_sample/config_cache/`

没有旧项目存档格式迁移：示例版本为 1。配置路径使用 `res://`，可变数据使用 `user://`，不把开发机路径写入数据。

## 桌面导出

安装与 Godot 4.7.2 匹配的导出模板后，在编辑器 Export 面板选择 Windows Desktop 或 Linux Desktop。预设会包含 YAML 和 schema 文本；不要删除 `include_filter`，否则导出态无法加载配置。

命令行可从仓库根目录执行：

```bash
mkdir -p Samples/GodotIntegration/exports
godot --headless --path Samples/GodotIntegration --export-release "Windows Desktop"
```

运行导出的程序，重复新游戏、暂停、设置、保存、退出及继续流程。引擎启动、输入、暂停、原生节点释放与桌面导出需要 Godot 原生环境；`dotnet build` 只验证托管代码可以编译。

## 原生集成自检

完成 Debug 构建后，可通过原生引擎运行一次自动结束的集成检查：

```bash
godot --headless --path Samples/GodotIntegration -- --smoke
```

检查使用真实按钮信号、原生键盘事件与引擎帧，覆盖配置加载、新游戏、移动、嵌套暂停、设置与按键快照落盘重载、存档与继续，以及节点释放后的协程停止。每次运行使用独立的 `user://runtime_smoke/<随机标识>/` 目录，不读取或覆盖日常样例数据。

协程检查还覆盖 Update → FixedUpdate → Deferred → Update 的实际阶段顺序、暂停期间普通 Physics 冻结与忽略暂停任务继续、暂停时令牌取消、`CancelWith` 长等待注销，以及节点退出后父子任务的 `finally`。这些检查使用真实节点和引擎回调，托管阶段测试不能替代它们。

正常完成输出 `GFRAMEWORK_SMOKE_RESULT PASS` 并返回 0；断言或清理失败输出 FAIL 并返回非零。CI 或本地调用方仍应为引擎进程设置超时，以发现启动或调度挂起。该检查不评价 GUI 布局、可读性或视觉效果，桌面导出仍需单独运行验证。

首次通过还会输出 `GFRAMEWORK_SMOKE_DATA <标识>`。退出后用新进程确认设置和存档能跨进程恢复：

```bash
godot --headless --path Samples/GodotIntegration -- --smoke-continue=<标识>
```

续测标识必须是 32 位 N 格式 GUID，使用上一轮输出的标识即可找到其数据；路径固定在 `user://runtime_smoke/` 下，格式错误或组合多个模式会直接失败。新进程通过正常启动加载按键快照，再点击 Continue，检查非默认进度与暂停状态；不会访问日常样例的 `runtime_sample` 目录。导出的程序同样接受这两个自检参数。


Linux 桌面导出可以使用对应预设：

```bash
godot --headless --path Samples/GodotIntegration --export-release "Linux Desktop"
Samples/GodotIntegration/exports/GFrameworkSample.x86_64 --headless -- --smoke
```

导出程序的 --smoke 也使用独立的测试数据目录，验证打包配置资源和运行时持久化；它不验证可视布局。

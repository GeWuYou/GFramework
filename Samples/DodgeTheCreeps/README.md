# Dodge the Creeps

使用 Godot 官方入门教程素材制作的 2D 生存小游戏。WASD 或方向键移动，躲开不断出现的敌人，生存时间即得分。

## 运行

需要 Godot 4.7.2 .NET 和 .NET SDK。可直接在 Godot 编辑器中打开本目录的项目并运行主场景，也可在仓库根目录执行：

```bash
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --editor --path Samples/DodgeTheCreeps
godot --path Samples/DodgeTheCreeps
```

`godot` 使用本机的 Godot .NET 可执行文件。首次打开会导入图片、音频和字体。Release 构建使用同一项目加 `-c Release`。

## 操作

- **New run**：开始新一局；**Continue saved run**：恢复槽位 1 的分数和位置。
- WASD 或方向键移动；Escape 或右上角按钮打开暂停菜单。
- 暂停菜单支持恢复、保存当前局、设置和返回主菜单。
- 设置可调整音量和全屏。选择 **Save settings** 后保存并应用；关闭设置会回到原菜单。
- 未保存的新局返回主菜单时，确认框提供继续游戏与放弃选择。
- 碰撞结束后显示本局与最高分，可以重试或返回菜单。

新局不会覆盖已有槽位，只有明确保存才更新槽位 1。继续游戏会在保存的位置生成一个新的敌人布局，并提供短暂保护。游戏不在退出时自动保存当前局。

## 框架接入

| 入口 | 职责 |
| --- | --- |
| `DodgeRoutes` | 强类型页面、窗口、对话框定义；独立声明排序、模态和暂停策略 |
| `DodgeMenus` | 普通 Control 视图及一次性注册；按钮通过实例上下文关闭或提交结果 |
| `DodgeApp` | 组合 UiService、SceneRouter、暂停、存档、设置与原生输入，观察异步动作并等待停止 |
| `Gameplay/` | 原生移动、敌人、计分、碰撞和动画 |

此示例直接组合普通服务；[运行时集成示例](../GodotIntegration/README.md) 展示同一套服务接入可选 Architecture。

普通页面或窗口只需添加视图和一处强类型注册。参数在工厂中接收，对话框通过 `UiDialogContext<TResult>.CompleteAsync` 返回结果。

## 数据

- 当前局：`user://dodge_the_creeps/saves/slot_1/save.json`
- 音量、全屏与最高分：`user://dodge_the_creeps/preferences.json`

存档只包含版本、分数和数值坐标。资源使用 `res://`，可变数据使用 `user://`。读取或写入失败会报告错误并保留原有文件。

## 桌面导出

安装同版本 .NET 导出模板后，在 Export 面板选择 Windows Desktop 或 Linux Desktop，也可在仓库根目录执行：

```bash
mkdir -p Samples/DodgeTheCreeps/exports
godot --headless --path Samples/DodgeTheCreeps --export-release "Windows Desktop"
godot --headless --path Samples/DodgeTheCreeps --export-release "Linux Desktop"
```

分发整个导出目录，保留可执行文件、资源包和对应的 `data_DodgeTheCreeps_*` 目录，以及[素材署名](ASSETS.md)和许可证。项目自带独立解决方案，供 Godot 发布程序集时使用。

## 原生自检

完成 Debug 构建和资源导入后：

```bash
godot --headless --path Samples/DodgeTheCreeps -- --smoke
godot --headless --path Samples/DodgeTheCreeps -- --smoke-continue=<上一轮输出的标识>
```

自检使用真实按钮信号、输入和引擎帧，检查移动、嵌套暂停、设置、保存、确认框、继续和碰撞结束。每次运行使用独立的 `user://dodge_smoke/<GUID>/`，并输出 `DODGE_SMOKE_RESULT PASS` 或 `FAIL`。续测在新进程中确认持久化数据恢复，导出的程序接受相同参数。调用方应设置进程超时。

需要查看界面时可运行 `-- --capture-menu=<PNG绝对路径>`，程序保存菜单画面后正常退出。

## 素材

图片、音乐、音效和字体来自 Godot 官方 [Dodge the Creeps 教程](https://docs.godotengine.org/en/stable/getting_started/first_2d_game/index.html)。作者、来源和许可证见[素材署名](ASSETS.md)。

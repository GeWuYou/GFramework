# Dodge the Creeps 进阶示例

这是与基础版玩法和能力对应的独立 Godot 游戏，展示标准 DI、显式依赖、按功能组织、领域与宿主分离、可选 Architecture 生命周期。操作、素材和内容包沿用基础版；应用标识、程序集及存储目录独立。

配套的[进阶项目教程](../../docs/zh-CN/tutorials/godot-advanced-project.md)说明如何从基础接入过渡到此结构，并直接引用编译源码。

## 运行

需要 Godot 4.7.2 .NET 与仓库支持的 .NET SDK。从仓库根执行：

```bash
bash scripts/prepare-dodge-advanced-mods.sh
dotnet build Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj -c Debug
godot --editor --path Samples/DodgeTheCreeps.Advanced
godot --path Samples/DodgeTheCreeps.Advanced
```

脚本单独编译外部 Bonus 与 Harmony 示例，将模组自己的 DLL 和依赖描述暂存进此项目，不打包宿主共享的 ModApi 或 Harmony 库。首次导入图片、字体和音频后运行主场景。也可在 Godot 编辑器中直接打开本目录的项目。

Windows 本轮源项目运行使用官方 4.7.2 mono Windows 编辑器和 .NET 10，并在独立验收进程设置 DOTNET_ROLL_FORWARD=LatestMajor；没有修改游戏或编辑器的 runtimeconfig。只有 .NET 10 的本机可在 PowerShell 中设置这项进程环境后运行，或安装 .NET 8 运行时满足编辑器工具的默认要求：

```powershell
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
godot --path Samples/DodgeTheCreeps.Advanced
```

## 项目与职责

| 项目或边界 | 职责 |
| --- | --- |
| [领域与应用](../DodgeTheCreeps.Advanced.Core/README.md) | 纯 .NET GameSession、RunRules、RunUseCases、设置规则及 IRunBoard/IRunStore 端口 |
| AdvancedGameRoot | Godot 场景根、启动及退出，等待完整异步清理 |
| AdvancedGameArchitecture | 唯一容器装配入口、显式内容启动组件、宿主线程调度 |
| GameApplication | 菜单与场景流程，调用明确的应用用例和功能服务 |
| SettingsService | 设置草稿验证、原生应用、持久化与失败回滚 |
| SaveService | SaveDocument、版本转换、内容恢复、确认提交、精确备份与 Mod 负载 |
| DodgeContent / ContentSelectionService | 内容发现和加载、下次启动选择、可执行代码批准 |
| ModService | 每模组 API、计分扩展、当前局状态及独立撤销 |
| GameBoardAdapter / GameSceneFactory | 应用端口到真实 Godot 棋盘的适配 |
| [用例测试](../DodgeTheCreeps.Advanced.Tests/README.md) | 不启动引擎即可验证应用规则、失败顺序与依赖方向 |

Core 不引用引擎或框架。游戏的原生移动、碰撞、音频、动画、随机数及场景生命周期保留在 Godot 边界。容器只在装配根使用；服务通过构造函数取得实际依赖。

## 游戏功能

- WASD 或方向键八向等速移动；四向素材表现，停止时保留最后朝向。
- 开始、暂停、设置、返回、死亡、重开，完整棋盘跨进程继续。
- 明确保存才更新槽位 1；新局不覆盖旧存档，死亡清除当前局与恢复入口。
- 总音量、音乐、音效、全屏与三档竖屏尺寸；草稿可应用、丢弃或继续编辑。
- 英文与简体中文实时切换，保留当前场景、暂停、对话框与焦点。
- Moon 原生 DLC、Coral 资料包、两种速度补丁、Bonus DLL 与 Double Score Harmony DLL。
- 内容启停与排序在重启后生效，代码批准绑定版本和指纹。
- 文档升级/显式降级、缺失内容修复、原始字节备份及精确恢复、Mod 状态保存。

玩法、内容格式和信任边界与[基础示例功能说明](../DodgeTheCreeps/README.md)一致。进阶版的可变数据根为 `user://dodge_the_creeps_advanced/`，GameId 为 `dodge-the-creeps-advanced`，不会读写基础版存档。ModApi 契约和外部示例程序集复用；Harmony 的内部游戏补丁目标作为明确扩展契约保留在进阶宿主中。

## 原生验收

先构建 Debug 并完成导入，再执行：

```bash
godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke
godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke-continue=<上一轮GUID>
```

支持与基础版相同的 movement、persistence、localization、settings、content、modding、review 入口及其续测参数。每轮使用 `user://dodge_advanced_smoke/<GUID>/`，检查退出码与 `DODGE_*_RESULT PASS`。内容和代码模组验收必须跨真实进程按输出的 NEXT 标识继续。

`--capture-menu=<PNG绝对路径>`、`--capture-content=...`、`--capture-settings=...`、`--capture-recovery=...`、`--capture-game=...` 保存实际渲染画面；可结合 `--capture-language=zh-CN`。截图需正常渲染环境。窗口模式和大小也需可视环境验收，headless 不能证明显示正确。

## 导出与素材

使用同版本 .NET 导出模板，运行模组准备脚本后从此项目导出：

```bash
godot --headless --path Samples/DodgeTheCreeps.Advanced --export-release "Windows Desktop"
godot --headless --path Samples/DodgeTheCreeps.Advanced --export-release "Linux Desktop"
```

导出默认写入本项目的 exports 目录。分发完整导出产物、数据目录和[素材署名](ASSETS.md)。本项目独立保存图片、音频、字体、内容文件和素材许可证，可单独导入及导出；不引用基础版程序集或运行资源。

官方 PCK 源场景及信任摘要沿用同一份授权素材。重新生成时在此目录执行 `build-official-pack.gd`，随后构建宿主；修改原生内容需要同步更新开发者固定的信任摘要。

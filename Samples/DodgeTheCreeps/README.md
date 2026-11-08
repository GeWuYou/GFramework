# Dodge the Creeps

使用 Godot 官方入门教程素材制作的 2D 生存小游戏。WASD 或方向键移动，躲开不断出现的敌人，生存时间即得分。

从空工程复现制作过程，见[官方素材小游戏制作教程](../../docs/zh-CN/tutorials/godot-complete-project.md)。教程按素材准备、角色与敌人、UI、暂停、存档和导出组织，并直接引用本工程源码。

## 运行

需要 Godot 4.7.2 .NET 和 .NET SDK。可直接在 Godot 编辑器中打开本目录的项目并运行主场景，也可在仓库根目录执行：

```bash
python3 scripts/build-dodge-mods.py
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --editor --path Samples/DodgeTheCreeps
godot --path Samples/DodgeTheCreeps
```

`godot` 使用本机的 Godot .NET 可执行文件。首次打开会导入图片、音频和字体。Release 构建使用同一项目加 `-c Release`。`build-dodge-mods.py` 独立编译并暂存两个代码示例；主游戏不引用其实现项目，运行时从安装目录加载 DLL。运行该脚本后，下次启动会补装尚未安装的代码示例；导出前同样保留暂存结果。

## 操作

- **New run**：开始新一局；**Continue saved run**：恢复槽位 1 的完整棋盘。
- WASD 或方向键移动；Escape 或右上角按钮打开暂停菜单。
- 暂停菜单支持恢复、保存当前局、设置和返回主菜单。
- 设置提供总音量、音乐、音效，窗口／全屏，360×540、480×720、640×960 三档竖屏尺寸，以及英文／简体中文。
- 语言切换立即保存并刷新现有界面，其余设置通过 **Apply settings** 应用。返回或 Escape 遇到草稿时可应用、放弃或继续编辑；重置不会清除最高分、游戏存档或内容包选择。
- **Content** 管理可选包的启用和加载优先级；保存后重启游戏生效。当前已加载的关卡可通过 **Arena** 切换。
- 未保存的新局返回主菜单时，确认框提供继续游戏与放弃选择。
- 碰撞结束后显示本局与最高分，并清除当前存档和备份恢复入口，可以重试或返回菜单。

新局不会覆盖已有槽位，只有明确保存才更新槽位 1。继续游戏恢复敌人数量、位置、速度、动画进度、玩家朝向、计时和随机状态。玩家死亡后清除槽位 1，设置和最高分保留。游戏不在退出时自动保存当前局。文档版本转换与内容修复先在独立数据上准备，确认后备份原字节并提交；旧扁平 DTO 存档不自动识别。

## 框架接入

| 入口 | 职责 |
| --- | --- |
| `DodgeRoutes` | 强类型页面、窗口、对话框定义；独立声明排序、模态和暂停策略 |
| `DodgeMenus` | 普通 Control 视图及一次性注册；按钮通过实例上下文关闭或提交结果 |
| `DodgeApp` | 组合 UiService、SceneRouter、暂停、存档、设置与原生输入，观察异步动作并等待停止 |
| `Gameplay/` | 原生移动、敌人、计分、碰撞和动画 |
| `DodgeApp.Mods`、`Modding/` | 每模组游戏 API、状态作用域和内部计分补丁目标 |
| `DodgeProgressMigration` | DTO 创建前的文档升级和显式降级 |

此示例直接组合普通服务；[运行时集成示例](../GodotIntegration/README.md) 展示同一套服务接入可选 Architecture。

普通页面或窗口只需添加视图和一处强类型注册。参数在工厂中接收，对话框通过 `UiDialogContext<TResult>.CompleteAsync` 返回结果。

## 数据

- 当前局：`user://dodge_the_creeps/saves/slot_1/save.json`
- 音量、全屏与最高分：`user://dodge_the_creeps/preferences.json`

存档采用 `SaveDocument`：外层包含游戏身份、schema、内容包版本／指纹／实际顺序及各模组版本负载；`Game.Data` 保存完整棋盘的可序列化数值和稳定敌人／关卡 ID。设置和游戏进度独立保存。资源使用 `res://`，可变数据使用 `user://`。读取或写入失败会报告错误并保留原有文件。

玩家支持八方向等速移动，动画使用官方的横向和向上两组帧，通过翻转表现四向；斜向不旋转角色。移动朝向随存档保存，停下后保留最后姿态。

界面复用框架本地化管理器，翻译表在 `Localization/`，中文字体及 OFL 许可随工程分发。首次采用系统语言，不支持的语言回退英文；语言切换保留当前世界、暂停层、对话框和输入焦点。

## DLC 与资料模组

示例将尚未安装过的自带演示包安装到 `user://dodge_the_creeps/content/installed/`，全部默认停用。本体数据和游戏定义的 schema 使用单独缓存。之后启动不会重新安装被删除的演示包，也不会覆盖玩家安装的模组。

| 包 | 内容 |
| --- | --- |
| Moon Arena | 官方 DLC，增加 `moon:crater` 关卡和独立 PCK 中的原生装饰场景 |
| Coral Garden | 资料模组，增加 `coral:drifter` 敌人和 `coral:reef` 关卡，加载原始 PNG 与 OGG |
| Gentle Creeps | 将本体 walker 的速度字段改为 70–100 |
| Fast Creeps | 将相同速度字段改为 220–260，用于检查玩家优先级和字段来源 |
| Bonus Score | 独立 DLL，通过游戏公开 API 每次额外加 1 分并记录累计次数 |
| Double Score (Harmony) | 独立 DLL，使用 owner 作用域给内部计分结果乘 2 |

在 **Content** 中勾选包，用上下按钮设置优先级，界面显示受依赖约束的最终顺序。后加载的字段补丁覆盖先前值，修改不同字段可以叠加。保存选择不改变当前局；重启后显示最终 walker 速度及其来源。错误内容会禁止开始游戏，但管理界面仍可停用错误包、禁用已缺失包或重置为本体。

安装其他资料模组时，将完整包目录复制到 `content/installed/`，使 `content-pack.json` 位于其根目录，然后重新启动。清单和内容格式可参考 `content_sources/`。游戏只注册 `enemies`、`arenas` 两张表，类型及 schema 由游戏定义；资料型包不提供 CLR 类型或玩法代码。敌人的 `texture` 和关卡的 `music` 使用 `包ID:相对路径`，例如 `coral:sprites/creep.png`；只能读取启用包内的 PNG、OGG、WAV，不接受场景或脚本。

Moon 的原生场景通过独立信任入口加载：游戏代码中固定其 PCK 的 SHA256，挂载时禁止替换已有资源，场景只能从 `res://content/moon/` 读取。修改包清单里的名称或“官方”标记不能获得信任。原生挂载失败后需要修正选择并重启，不尝试在当前进程卸载或重新拼装资源。

官方 PCK 的源场景在 `official-pack/`。使用相同 Godot 版本重新生成资源包及编译进游戏的信任摘要：

```bash
godot --headless --path Samples/DodgeTheCreeps --script build-official-pack.gd
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
```

`content_sources/coral` 中的原始媒体以 `.source` 后缀随游戏打包，安装时去掉该后缀。这样导出程序保留原始可解码文件，不依赖编辑器导入缓存。用户安装的正常包直接使用 `.png`、`.ogg` 文件即可。重新生成摘要是游戏开发者操作，不属于玩家安装模组流程；真实分发应由游戏提供自己的可信包验证策略。

## 代码模组

`DodgeTheCreeps.ModApi` 是宿主与模组共享的独立契约项目；`DodgeTheCreeps.BonusMod` 和 `DodgeTheCreeps.HarmonyMod` 分别编译成外部 DLL。`python3 scripts/build-dodge-mods.py` 只将模组自己的 DLL 和依赖描述暂存为 `content_sources/code-*/` 中的 `.source` 文件，运行时安装时去掉后缀；共享 API 和 Harmony 由宿主提供。

在 Content 中启用代码包会先显示执行代码的确认；确认绑定包版本及文件指纹。更新后的包需要重新批准，清单中的名称不能授予执行权限。代码拥有游戏进程权限，不是沙箱。启停、排序和更新都在重启后生效。

原计分每次加 1；只启用 Bonus Score 时加 2，只启用 Double Score 时加 2，同时启用时先计算内部补丁结果，再执行公开 API 修饰器，合计加 3。Bonus 的次数保存在该模组独立数据区，与完整棋盘一起保存；新局重置，继续游戏恢复。状态 API 只在活动局面中可用；模组初始化和无当前局的主菜单阶段访问会得到明确错误，因此初始化只登记计分回调，回调执行时才读写本局状态。停止时注销回调并只撤销对应模组的 Harmony 补丁。

安装自己的代码包时，使用清单格式 v2、明确 DLL／入口类型和代码 API 范围；需要补丁服务时声明 `harmony` 能力。将可运行包目录复制到安装位置后重启，按相同流程批准。完整 API 和信任边界见[代码模组与 Harmony](../../docs/zh-CN/game/code-mods.md)。

## 内容变更、结构转换与存档恢复

继续游戏先读取文档信封，再在独立负载上准备结构转换和内容修复，最后创建当前 DTO。停用 Moon 后，旧文档会报告缺失包和关卡；可以取消并恢复原内容，或确认候选修复。游戏的纯恢复策略接收完整 `SaveDocument`，将缺失关卡替换为 Original Yard，只移除失效定义的敌人，其余有效棋盘状态与缺失模组负载保留。其他游戏可在同一回调中明确修复或删除指定模组块。

`DodgeProgressMigration` 声明稀疏 v1 文档到完整 v2 棋盘的升级，以及带嵌套坐标的 v3 演示文档到当前 v2 的有损降级。两个方向由游戏明确实现；框架不把高版本号改小后直接读取，也不先用当前 DTO 解释旧负载。降级说明列出丢弃的未来模式。

准备不改变当前世界和原文件。玩家确认后，框架核对源文件和内容会话，将原始字节备份到当前槽位 `backups/`，再原子提交候选文档。准备后文件变化、取消、备份失败或验证／写入失败均不覆盖原档。

备份不自动清理。**Restore last save backup** 再次确认，先备份当前文件，再恢复上次原档；恢复后仍检查结构版本和内容依赖。最近备份入口在重启后保留，死亡清档使本局恢复入口失效。

## 桌面导出

安装同版本 .NET 导出模板后，在 Export 面板选择 Windows Desktop 或 Linux Desktop，也可在仓库根目录执行：

```bash
python3 scripts/build-dodge-mods.py
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

自检使用真实按钮信号、输入和引擎帧，检查移动、嵌套暂停、设置、保存、确认框、继续、碰撞结束和死亡清档。每次运行使用独立的 `user://dodge_smoke/<GUID>/`，并输出 `DODGE_SMOKE_RESULT PASS` 或 `FAIL`。续测确认死亡后的存档和恢复入口失效，设置和最高分保留。完整棋盘跨进程恢复由 `--persistence-smoke` 覆盖。导出的程序接受相同参数，调用方应设置进程超时。

需要查看界面时可运行 `-- --capture-menu=<PNG绝对路径>`，程序保存菜单画面后正常退出。

`-- --capture-content=<PNG绝对路径>` 和 `-- --capture-recovery=<PNG绝对路径>` 分别保存内容管理界面及长诊断恢复对话框，使用独立临时数据目录。这些截图参数需要正常渲染窗口，不能用 headless 渲染替代可视检查。

内容验收使用独立 GUID 数据目录和多个真实进程。先运行 `-- --content-smoke`，然后使用每轮输出的 `DODGE_CONTENT_NEXT` 值继续：

```bash
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke-continue=<GUID>:active
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke-continue=<GUID>:recover
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke-continue=<GUID>:broken
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke-continue=<GUID>:profile
godot --headless --path Samples/DodgeTheCreeps -- --content-smoke-continue=<GUID>:done
```

这些阶段检查启停需重启、两个补丁的顺序与来源、资料包媒体和原生 PCK 场景生效、取消降级不写档、自动备份及精确恢复、修改 PCK 被拒绝、损坏设置仍可管理修复，以及下一进程恢复本体游戏。`recover` 阶段只篡改本轮 GUID 安装目录中的测试 PCK。成功输出 `DODGE_CONTENT_RESULT PASS <阶段>`；导出程序接受相同参数。

新增功能有独立原生入口：`--movement-smoke` 检查八向移动、动画和等速；`--persistence-smoke` 与 `--persistence-smoke-continue=<GUID>` 检查完整棋盘及后续随机生成；`--localization-smoke` 与同名 `-continue=<GUID>` 检查实时翻译和新进程语言恢复；`--settings-smoke` 与同名 `-continue=<GUID>` 检查全部设置、草稿关闭、失败回滚和持久化。每轮使用独立 GUID 数据目录，检查退出码和 `DODGE_SMOKE_RESULT PASS`。

代码模组跨进程验收先构建外部 DLL，再运行一次初始入口和三次续测，每次使用上一轮的 `DODGE_MODDING_NEXT` 标识：

```bash
python3 scripts/build-dodge-mods.py
godot --headless --path Samples/DodgeTheCreeps -- --modding-smoke
godot --headless --path Samples/DodgeTheCreeps -- --modding-smoke-continue=<GUID>
godot --headless --path Samples/DodgeTheCreeps -- --modding-smoke-continue=<GUID>
godot --headless --path Samples/DodgeTheCreeps -- --modding-smoke-continue=<GUID>
```

阶段 0–3 分别检查原生拒绝／允许代码执行且当前进程不生效、两个 DLL 改变计分并保存模组状态、停用后恢复本体计分且保留缺失模组数据、重新启用后恢复状态并独立撤销补丁和 API 注册。各阶段输出 `DODGE_MODDING_RESULT PASS <阶段>`，导出程序支持相同参数。

`--persistence-smoke` 还使用不能直接反序列化为当前 DTO 的 v3 文档，检查先准备嵌套坐标转换、有损降级确认、取消不改原始字节以及提交前的精确备份。

`--capture-settings=<PNG绝对路径>` 可捕获设置页；与 `--capture-language=zh-CN` 或 `en` 组合可以检查不同语言的实际渲染。物理窗口模式和大小需要正常渲染环境，headless 验收只验证请求状态与音频／持久化行为。

`--capture-game=<PNG绝对路径>` 捕获开局保护期的斜向输入姿态，用于检查角色没有旋转或额外圆环。

## 素材

图片、音乐、音效和字体来自 Godot 官方 [Dodge the Creeps 教程](https://docs.godotengine.org/en/stable/getting_started/first_2d_game/index.html)。作者、来源和许可证见[素材署名](ASSETS.md)。

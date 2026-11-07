---
title: 制作官方素材小游戏
description: 从空白 Godot C# 工程制作 Dodge the Creeps，逐步接入玩法、强类型 UI、暂停、设置、存档与桌面导出。
---

# 制作官方素材小游戏：Dodge the Creeps

本教程按实际制作顺序，使用 [Godot 官方首个 2D 游戏教程](https://docs.godotengine.org/en/stable/getting_started/first_2d_game/index.html) 的素材，组装一款移动躲避、生存计分的小游戏。角色、敌人和动画使用 Godot 原生节点；GFramework 承接场景、UI、暂停、存档和异步关闭。

完成后可以操作主菜单、新游戏、暂停、设置、保存继续、放弃确认和结束画面。完整工程位于 [小游戏源码](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)。

<img src="./assets/dodge-the-creeps/menu.png" alt="Dodge the Creeps 主菜单，包含新局、继续、设置与退出操作" width="320" />

## 准备环境与工作目录 {#prepare}

需要 C# 的类、事件、泛型和 `async/await` 基础，以及 Godot 的节点、信号和输入映射概念。准备 Godot **4.7.2 .NET** 与能够构建仓库目标框架的 **.NET SDK 10**；示例游戏本身使用 `net8.0`。引擎版本与项目的 `Godot.NET.Sdk`、框架的 GodotSharp 引用保持一致。

在工作区克隆框架，并在旁边创建练习工程：

```bash
git clone https://github.com/GeWuYou/GFramework.git GFramework
```

```text
workspace/
├─ GFramework/          # 框架源码及完整样例
└─ DodgeTheCreeps/      # 在 Godot 中新建的练习工程
```

下文的终端命令均从这个工作区根目录执行。`godot` 表示本机 Godot .NET 可执行文件；`res://` 表示练习工程根目录。

想先观察完成后的效果，可以运行仓库样例：

```bash
dotnet build GFramework/Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --editor --path GFramework/Samples/DodgeTheCreeps
```

正文的关键片段直接引用可编译样例中的命名区段。片段位于相应类内部；组装时保留完整文件中的 `using`、字段和辅助方法。每个制作阶段都提供完整源码入口，便于逐步对照。

## 一、建立主场景与项目引用 {#project}

在 Godot 项目管理器中新建 `DodgeTheCreeps`，选择 Compatibility 渲染器。创建 `Node` 根节点，保存为 `Main.tscn`，挂载 C# 脚本 `DodgeApp.cs`，将 `Main.tscn` 设为主场景。

在项目设置中确认：

| 设置 | 值 |
| --- | --- |
| Viewport Width / Height | `480` / `720` |
| Window Width / Height Override | `480` / `720` |
| Stretch Mode | `canvas_items` |
| .NET Assembly Name | `DodgeTheCreeps` |
| Main Scene | `res://Main.tscn` |

项目文件沿用样例的配置：

<<< ../../../Samples/DodgeTheCreeps/DodgeTheCreeps.csproj{xml}

上面的引用路径用于仓库内的样例。按本教程的并列目录布局，将三项引用改为：

```xml
<ItemGroup>
  <ProjectReference Include="../GFramework/GFramework.Godot/GFramework.Godot.csproj"/>
  <ProjectReference Include="../GFramework/GFramework.Game.Abstractions/GFramework.Game.Abstractions.csproj"/>
  <ProjectReference Include="../GFramework/GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj"/>
</ItemGroup>
```

主场景只需要挂载启动脚本：

<<< ../../../Samples/DodgeTheCreeps/Main.tscn{ini}

先保留 Godot 生成的空 `DodgeApp : Node` 脚本，执行：

```bash
dotnet build DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --path DodgeTheCreeps
```

**检查点：** 窗口正常打开，C# 脚本可编译，主场景可找到。此时画面为空。

## 二、准备官方美术、声音与字体 {#assets}

从官方教程下载 [Dodge the Creeps 素材包](https://github.com/godotengine/godot-docs-project-starters/releases/download/latest-4.x/dodge_the_creeps_2d_assets.zip)，解压后把 `art/`、`fonts/` 放到练习工程根目录。

| 文件 | 用途 |
| --- | --- |
| `playerGrey_walk1.png`、`playerGrey_walk2.png` | 玩家横向移动 |
| `playerGrey_up1.png`、`playerGrey_up2.png` | 玩家纵向移动 |
| `enemyWalking_*.png`、`enemySwimming_*.png`、`enemyFlyingAlt_*.png` | 三种敌人外观 |
| `House In a Forest Loop.ogg`、`gameover.wav` | 背景音乐与结束音效 |
| `Xolonium-Regular.ttf` | 菜单字体 |

文件名与大小写保持原样，游戏通过 `res://art/...` 和 `res://fonts/...` 加载。让编辑器完成资源导入。

保留字体原有的许可证和说明，并从完整样例保留 `ASSETS.md` 与 `art/LICENSE-MIT.txt`。图片使用 Kenney 的 CC0 素材，音乐需保留 HorrorPen 的 CC-BY 3.0 署名，字体遵循 OFL。分发要求集中在 [素材署名与许可](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/ASSETS.md)。

**检查点：** 在 FileSystem 中能预览 PNG，播放音频，并找到字体资源。

## 三、实现玩家移动与动画 {#player}

先建立 `Gameplay/`。这个阶段完成以下五个文件，再运行玩法预览：

| 文件 | 责任与完整源码 |
| --- | --- |
| `DodgeSpriteFrames.cs` | [把原始图片组成动画](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/Gameplay/DodgeSpriteFrames.cs) |
| `DodgePlayer.cs` | [玩家节点、移动与保护指示](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/Gameplay/DodgePlayer.cs) |
| `DodgeMob.cs` | [敌人节点、外观与速度](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/Gameplay/DodgeMob.cs) |
| `DodgeWorld.cs` | [生成、更新、计分与碰撞](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs) |
| `ScoreChangedEventArgs.cs` | [分数变化事件数据](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/Gameplay/ScoreChangedEventArgs.cs) |

保留源码中的 `DodgeTheCreeps.Gameplay` 命名空间。可以先复制完整实现作为起点，再按本节和下一节逐项调整。

### 输入动作

在 Project Settings → Input Map 中添加四个动作及按键：

| 动作 | 按键 |
| --- | --- |
| `dodge_left` | A、左方向键 |
| `dodge_right` | D、右方向键 |
| `dodge_up` | W、上方向键 |
| `dodge_down` | S、下方向键 |

这些动作供世界移动使用。菜单导航继续使用 Godot 的 `ui_*` 动作。完成工程中的 `ConfigureInput` 会补齐尚未存在的游戏动作；已经在编辑器建立的动作保留原有绑定。

### 玩家节点

`DodgePlayer._Ready` 创建 `AnimatedSprite2D`，将四张玩家图片组成 `walk` 和 `up` 动画。`Advance` 读取输入、按 `delta` 更新坐标并限制边界：

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgePlayer.cs#tutorial-movement{csharp}

移动方向决定动画与翻转；停止移动时停在当前帧。重开后的短暂保护由世界通知玩家，玩家绘制相应指示。

## 四、实现敌人、计分与碰撞 {#world}

`DodgeWorld._Ready` 创建玩家、背景音乐播放器和结束音效播放器。世界进入场景树后，`StartRun` 才能访问这些节点。

### 统一更新

世界用一个暂停感知的物理更新驱动移动、生成和碰撞，玩家与敌人的 `Advance` 由它调用：

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-loop{csharp}

`AdvanceScore` 累加未暂停的 `delta`，每满一秒增加一分，通过 `ScoreChanged` 通知界面。`IsRunning` 控制一局是否继续更新。

### 从边缘生成敌人

先在屏幕外的四条边选择出生点，再朝场内随机目标移动。生成时确定速度、外观和方向：

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-spawn{csharp}

首次生成留出准备时间。分数提高后缩短生成间隔、增加速度，并用下限和上限控制难度增长。`DodgeMob` 负责原生动画，世界负责其运动与移除。

### 碰撞与结束

每帧移除离开有效区域的敌人，并比较玩家与敌人的圆形距离：

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-collision{csharp}

保护时间结束后，距离小于两个碰撞半径之和时调用 `EndRun`。它先停止本局，再播放结束音效并发出一次 `RunEnded`。

### 先跑玩法预览

暂时将 `DodgeApp.cs` 写为下面的预览脚本：

```csharp
using DodgeTheCreeps.Gameplay;
using Godot;

namespace DodgeTheCreeps;

public partial class DodgeApp : Node
{
    public override void _Ready()
    {
        var world = new DodgeWorld { Name = "World" };
        AddChild(world);
        world.StartRun();
    }
}
```

重新构建并运行。

**检查点：** 玩家能移动且留在场内，敌人从边缘出现，碰撞后这一局停止。这个阶段可以通过编辑器 Remote 场景树查看原生节点；第六步会替换这份预览脚本。

## 五、定义页面、窗口和数据 {#routes}

完整应用按下列文件分工：

```text
DodgeTheCreeps/
├─ Main.tscn
├─ DodgeApp.cs                 # 服务装配与游戏业务流程
├─ DodgeApp.Smoke.cs           # 同一个 partial 类型中的原生自检
├─ DodgeRoutes.cs              # 路由定义
├─ DodgeMenus.cs               # 原生视图和按钮
├─ DodgeProgress.cs            # 当前局存档
├─ DodgePreferences.cs         # 音量、全屏、最高分
├─ Gameplay/
├─ art/
└─ fonts/
```

### 路由定义

在 `DodgeRoutes` 中一次声明参数类型和交互策略，注册与调用复用这些定义：

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

| 界面 | 组织方式 | 参数与行为 |
| --- | --- | --- |
| 主菜单 | 页面 | `bool` 表示是否允许继续存档 |
| HUD | 独立窗口 | `int` 初始分数；不捕获返回动作 |
| 暂停 | 模态窗口 | `int` 当前分数；持有暂停请求 |
| 设置 | 模态窗口 | 独立暂停请求，显示在暂停菜单上方 |
| 放弃确认 | 结果对话框 | `string` 提问，返回 `bool` |
| 结束画面 | 页面 | `int` 本局分数 |

`Layer` 确定展示顺序。`Modal`、世界输入阻断和 `PauseWhileVisible` 分别决定交互与暂停。HUD 的 `AllowBack = false`、`CapturedActions = None` 使它能持续展示而不截获返回。

### 存档与偏好

当前局只保存可序列化数值：

<<< ../../../Samples/DodgeTheCreeps/DodgeProgress.cs{csharp}

偏好数据保存音量、全屏和最高分：

<<< ../../../Samples/DodgeTheCreeps/DodgePreferences.cs{csharp}

继续游戏恢复分数和坐标，并生成新的敌人布局与保护时间。新局保持为内存状态，玩家明确保存后才更新槽位。

## 六、装配框架服务并注册原生视图 {#services}

用 [完整启动脚本](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/DodgeApp.cs) 替换第四步的预览脚本，并加入 [原生自检伴随文件](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/DodgeApp.Smoke.cs)、路由、两个数据类型和 [菜单实现](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/DodgeMenus.cs)。保留同一 `partial DodgeApp` 的两个文件，以便编译完整工程；普通启动流程也调用其中的参数解析方法。

### 在原生线程创建服务

以下接线位于 `DodgeApp._Ready`。根节点进入树后捕获宿主同步上下文，随后创建场景、UI 和输入桥：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-services{csharp}

`DodgeApp` 实现 `ISceneRoot`，把场景行为的原生对象挂到自身，并在清理时分离。它的 `WorldFactory.Create` 创建 `DodgeWorld`、连接计分与结束事件，再用 `SceneBehaviorFactory.Create` 包装。

`GodotUiRoot` 提供 UI 容器和焦点处理。`GodotPauseHandler` 将共享暂停管理器连接到场景树。`GodotUiInputBridge` 把未处理的 Escape 交给 UI；没有界面捕获时，执行打开暂停菜单的业务动作。

### 一处登记全部视图

`DodgeMenus` 用普通 `Control` 工厂登记视图。参数和关闭上下文随工厂进入：

<<< ../../../Samples/DodgeTheCreeps/DodgeMenus.cs#tutorial-register{csharp}

布局辅助方法 `Page`、`Title`、`Caption` 和 `Button` 负责一致的容器、字体和样式。完整菜单源码包含这些方法，创建新界面时可复用它们。

### 加载数据后显示主菜单

启动流程先创建存储与槽位仓库，读取偏好，最后决定 Continue 是否可用：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-load-data{csharp}

这时重新构建练习工程。

**检查点：** 主菜单出现，字体和美术可见；首次运行 Continue 处于禁用状态；设置能打开并返回。

## 七、连接按钮、HUD 与新游戏 {#start-game}

### 观察异步按钮操作

按钮把工作交给 `RunAction`，执行期间禁用自己，结束后恢复可用。Quit 使用独立的关闭边界：

<<< ../../../Samples/DodgeTheCreeps/DodgeMenus.cs#tutorial-buttons{csharp}

启动脚本保存并观察动作任务，统一展示错误：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-actions{csharp}

保留任务供关闭流程等待，可以避免保存尚未结束就释放底层资源。原生信号边界以外的业务方法保持返回 `Task`。

### 新游戏与继续游戏

`StartGameAsync` 先取得需要恢复的数据，再切换场景，最后显示 HUD：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-start-game{csharp}

`await _scenes.ReplaceAsync` 完成后，世界已经挂载并执行 `_Ready`，此时才能调用 `StartRun` 或 `RestoreRun`。

世界的 `ScoreChanged` 通过启动脚本更新 HUD；`RunEnded` 触发结束流程，保存最高分并显示带本局分数的结束页。最高分写入失败时，结束画面的重试和返回操作仍可使用。

**检查点：** New run 进入游戏，HUD 分数按生存时间变化；碰撞后出现结束画面，Try again 能开始新一局。

## 八、接入嵌套暂停与设置 {#pause-settings}

`DodgeWorld` 使用 `Pausable`，启动脚本和菜单能在暂停期间继续处理操作。可见的暂停窗口持有一份暂停请求，设置窗口再持有一份。

设置界面从当前偏好建立局部草稿：

<<< ../../../Samples/DodgeTheCreeps/DodgeMenus.cs#tutorial-settings{csharp}

保存时先写入新偏好，再替换内存数据并应用音量、全屏：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-settings{csharp}

关闭设置时通过实例上下文关闭准确的窗口，服务释放它自己的暂停请求；下面的暂停菜单仍然持有请求。

**检查点：** 游戏中按 Escape，角色、敌人和计分停止；打开设置、保存并返回后仍保持暂停；再恢复游戏才继续更新。关闭后重开设置，草稿从已保存偏好重新建立。

## 九、保存继续与确认结果 {#save-confirm}

### 显式保存当前局

玩家点击保存时，将世界状态转换为 DTO，再交给槽位仓库：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-game{csharp}

游戏进度保存在 `user://dodge_the_creeps/saves/slot_1/save.json`，偏好保存在 `user://dodge_the_creeps/preferences.json`。资源加载使用 `res://`，可变数据使用 `user://`。

### 用返回值处理放弃确认

确认视图直接通过 `UiDialogContext<bool>` 提交选择：

<<< ../../../Samples/DodgeTheCreeps/DodgeMenus.cs#tutorial-confirm{csharp}

业务流程等待结果，接受放弃后才清理世界并回到主菜单：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-confirm-exit{csharp}

这里的 `Accepted` 表示对话框提交了业务结果，`Value` 表示玩家的选择。Keep playing 提交 `false`；关闭对话框得到未接受结果；只有明确提交 `true` 才执行放弃。

**检查点：** 新局返回菜单时，Keep playing 保留当前局；选择放弃后返回菜单。保存一局、退出程序，再打开并 Continue，应恢复保存的分数和位置。

## 十、等待服务与原生资源完成关闭 {#shutdown}

窗口关闭和 Quit 按钮连接到 `RequestQuit`，先关闭新操作入口、取消等待，再等待动作与服务停止。Quit 按钮单独连接关闭边界，避免把关闭操作加入它自己等待的动作集合。

::: details 完整关闭实现

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-shutdown{csharp}

:::

服务停止期间保留原生根节点；页面回调和存档写入有机会完成。UI 和场景清理后，等待排队的节点释放及音频混音线程归还播放引用，再释放主题、存储和取消源。

`DodgeWorld._ExitTree` 解除音乐事件订阅、停止播放并释放它拥有的随机数对象。普通 UI 场景有业务订阅时，可用 `IGodotUiLifecycle` 在打开时登记、关闭时解绑，具体接法见 [Godot UI 生命周期](../godot/ui.md#可选生命周期)。

**检查点：** 游戏中关闭窗口、暂停时 Quit、保存后立即退出都能正常结束；重新运行仍可读取已明确保存的内容。

## 十一、构建、原生自检与桌面导出 {#export}

在练习工程中保留完整样例的 `DodgeTheCreeps.sln` 和 `export_presets.cfg`。独立解决方案中的项目名、项目文件和程序集名称保持一致，并包含 `Debug`、`Release`、`ExportDebug`、`ExportRelease` 映射。

先构建并导入资源：

```bash
dotnet build DodgeTheCreeps/DodgeTheCreeps.csproj -c Release
dotnet build DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --headless --editor --path DodgeTheCreeps --import
godot --headless --path DodgeTheCreeps -- --smoke
```

原生自检输出 `DODGE_SMOKE_RESULT PASS` 和一份 `DODGE_SMOKE_DATA <GUID>`。用该标识启动新进程：

```bash
godot --headless --path DodgeTheCreeps -- --smoke-continue=<上一轮的GUID>
```

自检在独立数据目录中通过真实按钮、输入和引擎帧检查移动、嵌套暂停、设置、存档、确认结果和结束流程。运行检查时给进程设置超时，并检查退出码与原生日志。

安装与引擎同版本的 .NET 导出模板，在练习工程中建立 `exports/`，然后执行：

```bash
godot --headless --path DodgeTheCreeps --export-release "Windows Desktop"
godot --headless --path DodgeTheCreeps --export-release "Linux Desktop"
```

分发时保留可执行文件、`.pck`、对应的 `data_DodgeTheCreeps_*` 目录，以及素材署名和许可证。导出程序支持相同的 `--smoke` 与 `--smoke-continue` 参数。

最后实际操作移动、暂停、焦点切换、设置、保存继续、重试和退出，检查窗口缩放、文字可读性、音量与游戏手感。

需要记录主菜单画面时，可运行 `godot --path DodgeTheCreeps -- --capture-menu=<PNG绝对路径>`，程序会保存画面并正常退出。

## 常见问题

| 现象 | 检查位置 |
| --- | --- |
| 玩家不能移动 | 四个 `dodge_*` 动作及按键绑定；世界是否已调用 `StartRun` |
| 重开时提示世界尚未挂载 | 先 `AddChild` 或等待场景替换，再开始本局 |
| 菜单关闭后仍暂停 | 检查下面是否还有可见暂停窗口或设置窗口 |
| 界面出现但点击没有作用 | 视图注册、原生层级和按钮是否交给 `RunAction`；查看错误提示 |
| 继续游戏不可用 | 是否明确保存槽位 1；新局只修改内存 |
| 导出找不到程序集 | 独立解决方案、程序集名、SDK 与 Export 配置映射 |
| 导出程序能启动但素材缺失 | 资源导入结果、资源路径和完整的导出目录 |

## 后续练习

新增一个带参数页面，或为实时数据展示加入打开订阅、关闭解绑；调整暂停菜单中的设置，并验证多层窗口返回规则。框架入口分别见 [通用 UI](../game/ui.md)、[场景服务](../game/scene.md)、[暂停实践](./pause-system.md) 和 [存档实践](./save-system.md)。

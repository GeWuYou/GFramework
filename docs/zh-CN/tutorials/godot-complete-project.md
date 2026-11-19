---
title: "制作 Godot 小游戏"
description: "运行并扩展 Dodge the Creeps 的玩法、界面、存档和内容。"
---

# 制作 Godot 小游戏 {#overview}

这个示例把移动、刷怪、碰撞和计分接入 GFramework 的类型化 UI、暂停与存档。先运行完整工程，再按下面的顺序查看接线。

## 准备工具 {#prepare}

安装[仓库 SDK 和匹配的 Godot .NET 编辑器](../getting-started/installation.md#requirements)。下列命令均从仓库根目录执行。

## 构建与启动 {#project}

```bash
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps --import
godot --path Samples/DodgeTheCreeps
```

## 素材 {#assets}

查看[素材与授权](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/ASSETS.md)。复用素材时保留对应许可与署名。

## 玩家移动 {#player}

用原生角色接收方向输入并驱动动画。

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgePlayer.cs#tutorial-movement{csharp}

## 敌人、碰撞与计分 {#world}

从玩法循环更新敌人与分数，游戏结束时结束当前局的存档。

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-loop{csharp}

<<< ../../../Samples/DodgeTheCreeps/Gameplay/DodgeWorld.cs#tutorial-collision{csharp}

## 页面和窗口 {#routes}

用强类型路由定义 HUD、设置与退出确认的参数和策略。

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

## 装配服务 {#services}

在宿主线程上创建共享调度器、视图根和存储，注册页与窗口的工厂。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-services{csharp}

## 开始游戏 {#start-game}

从菜单调用用例启动一局，并切换到对应 UI。让菜单把用户动作交给应用入口。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-start-game{csharp}

## 暂停与设置 {#pause-settings}

运行游戏后打开暂停页，再叠加设置窗口。关闭设置后，暂停页继续持有暂停请求；返回游戏后才恢复玩法。将设置的预览和确认写入拆开。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.Settings.cs#tutorial-save-settings{csharp}

## 保存与继续 {#save-confirm}

捕获独立的游戏快照并保存。重新运行后选择继续，检查位置和分数。实现细节见[实现存档](../tutorials/save-system.md)。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-game{csharp}

## 宿主关闭 {#shutdown}

等待正在进行的工作和原生服务完成退出，再释放场景树资源。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-shutdown{csharp}

## 自检与导出 {#export}

```bash
godot --headless --path Samples/DodgeTheCreeps -- --smoke
godot --headless --path Samples/DodgeTheCreeps -- --persistence-smoke
```

检查 `DODGE_SMOKE_RESULT PASS`。然后使用对应流程输出的标识运行 `--smoke-continue=<id>` 或 `--persistence-smoke-continue=<id>`，验证新进程读取行为。安装匹配的 .NET 导出模板后，从 Godot Export 面板选择 Windows Desktop 或 Linux Desktop。

## 加入 DLC 与模组 {#content-extension}

运行[内容包](../game/content-packs.md)流程为游戏添加定义和资源，或实现[代码模组契约](../game/code-mods.md)。保留内容包的稳定 ID，验证已保存的游戏数据能找到对应记录。

下一步：[把纯 C# 用例移出 Godot 宿主](../tutorials/godot-advanced-project.md)。

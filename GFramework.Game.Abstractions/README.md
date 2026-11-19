# Game.Abstractions

[简体中文](#简体中文) · [English](#english)

## 简体中文

为共享游戏项目提供内容、输入、存档、场景、UI 和宿主接口。

`GFramework.Game.Abstractions` 会安装 `GFramework.Core.Abstractions`。需要在共享项目中声明 `IUiHost`、`IStorage` 或 `IGameMod` 等契约时直接引用它。用 `GFramework.Game` 装配默认服务，Godot 原生适配由 `GFramework.Godot` 提供。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/abstractions/game-abstractions.html)

## English

Share content, input, save, scene, UI, and host interfaces across game projects.

`GFramework.Game.Abstractions` brings in `GFramework.Core.Abstractions`. Reference it directly in shared projects that declare contracts such as `IUiHost`, `IStorage`, or `IGameMod`. Use `GFramework.Game` for default services and `GFramework.Godot` for native Godot adapters.

[Read the guide](https://gewuyou.github.io/GFramework/abstractions/game-abstractions.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Game.Abstractions
```

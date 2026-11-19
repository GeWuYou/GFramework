# Game

[简体中文](#简体中文) · [English](#english)

## 简体中文

为游戏项目添加类型化配置、内容包、模组、存档、设置、场景与 UI。安装 Game 后从项目需要的服务和宿主适配开始。

`GFramework.Game` 会安装 `GFramework.Core` 和 `GFramework.Game.Abstractions`。共享接口的项目可直接引用契约包。需要从 JSON Schema 生成配置类型时加入 `GFramework.Game.SourceGenerators`，需要将场景、UI 或存储接到 Godot 时选择 `GFramework.Godot`，需要为代码模组提供 Harmony 补丁时加入 `GFramework.Game.Harmony`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/game/)

## English

Add typed configuration, content packs, mods, saves, settings, scenes, and UI to a game. Install Game and compose the services and host adapters your project requires.

`GFramework.Game` brings in `GFramework.Core` and `GFramework.Game.Abstractions`. Reference the contract package directly from a project that shares interfaces. Add `GFramework.Game.SourceGenerators` for typed configuration from JSON Schema, use `GFramework.Godot` for Godot scene, UI, or storage adapters, and add `GFramework.Game.Harmony` for code mod patches.

[Read the guide](https://gewuyou.github.io/GFramework/game/)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Game
```

# Godot

[简体中文](#简体中文) · [English](#english)

## 简体中文

将 GFramework 的服务与 Godot .NET 场景树接线。安装后选择场景、UI、输入、存储或协程等原生适配，用引擎线程的调度器装配。

`GFramework.Godot` 依赖 `GFramework.Game`，并通过它取得 `GFramework.Core` 和游戏契约。需要从 `project.godot` 生成项目常量，或需要节点查找、信号绑定等编译期辅助时，在 Godot 工程中另装 `GFramework.Godot.SourceGenerators`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/godot/)

## English

Connect GFramework services to a Godot .NET scene tree. Choose native adapters for scenes, UI, input, storage, or coroutines and compose them with the engine-thread dispatcher.

`GFramework.Godot` depends on `GFramework.Game`, which brings in `GFramework.Core` and the game contracts. For generated constants from `project.godot` or build-time helpers such as node lookups and signal bindings, add `GFramework.Godot.SourceGenerators` to the Godot project.

[Read the guide](https://gewuyou.github.io/GFramework/godot/)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Godot
```

# Godot Runtime Sample

[简体中文](#简体中文) · [English](#english)

## 简体中文

主场景展示移动、收集、暂停、设置和存档。GettingStarted 场景展示资源索引、页面、确认结果与关闭。运行时流程见[完整示例](https://gewuyou.github.io/GFramework/zh-CN/godot/runtime-sample.html)。

[查看教程](https://gewuyou.github.io/GFramework/zh-CN/tutorials/godot-integration.html)

## English

The main scene shows movement, collection, pause, settings, and saving. The GettingStarted scene demonstrates resource indexing, page opening, dialog results, and shutdown. See the [runtime walkthrough](https://gewuyou.github.io/GFramework/godot/runtime-sample.html).

[Read the walkthrough](https://gewuyou.github.io/GFramework/tutorials/godot-integration.html)

## 运行 · Run

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --headless --editor --path Samples/GodotIntegration --import
godot --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn
```

## 自检 · Smoke tests

```bash
godot --headless --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn -- --quickstart-smoke
godot --headless --path Samples/GodotIntegration -- --smoke
```

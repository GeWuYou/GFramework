---
title: "运行 Godot 示例"
description: "体验从启动、暂停设置到重启继续的完整流程。"
---

# 运行 Godot 示例 {#overview}

在仓库根目录构建并打开工程：

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --editor --path Samples/GodotIntegration
```

用匹配版本的 Godot .NET 启动主场景。开始新游戏，使用 WASD 移动，按 Escape 打开暂停菜单。在设置中更改绑定，保存游戏并返回菜单，重启后选择继续。

## 命令行自检 {#smoke}

```bash
godot --headless --path Samples/GodotIntegration -- --smoke
```

要验证重启后读取，使用首轮输出的标识执行 `--smoke-continue=<id>`。只接入一个页面和对话框时，使用[最小 Godot 场景](../tutorials/godot-integration.md)。

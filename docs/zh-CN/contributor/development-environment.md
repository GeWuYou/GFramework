---
title: "开发环境"
description: "安装仓库指定的 SDK 并构建源码和文档。"
---

# 开发环境 {#overview}

使用[仓库 SDK](../getting-started/installation.md#requirements)、Git、Bash 和 Python 3。预览文档使用 Bun，运行 Godot 工程使用匹配的 Godot .NET。

```bash
dotnet build GFramework.sln -c Release
dotnet test GFramework.sln -c Release
```

文档验证在仓库的 `docs/` 目录进行：

```bash
bun install --frozen-lockfile
bun run build
bun run dev
```

预览中英对应页面、检查两种语言的链接，并依据源码核对示例。

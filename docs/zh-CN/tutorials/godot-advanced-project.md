---
title: "组织进阶 Godot 项目"
description: "分离纯 C# 游戏规则、应用用例与 Godot 适配。"
---

# 组织进阶 Godot 项目 {#overview}

## 运行示例 {#run}

```bash
bash scripts/prepare-dodge-advanced-mods.sh
dotnet build Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps.Advanced --import
godot --path Samples/DodgeTheCreeps.Advanced
```

## 组织功能 {#features}

| 工程 | 职责 |
| --- | --- |
| `DodgeTheCreeps.Advanced.Core` | 游戏规则、进度快照和应用端口 |
| `DodgeTheCreeps.Advanced` | UI、原生玩法适配、设置和内容管理 |
| `DodgeTheCreeps.Advanced.Tests` | 对规则和用例的测试 |

从规则和快照开始，为真实的宿主边界定义端口。用例处理保存、恢复与计分顺序，再把结果交给原生视图。

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/RunUseCases.cs{csharp}

## 组合依赖 {#composition}

在组合根统一注册依赖。用启动组件预载资源，退出时等待内容、UI 与存储结束后再释放原生对象。

<<< ../../../Samples/DodgeTheCreeps.Advanced/Composition/AdvancedGameArchitecture.cs#advanced-composition{csharp}

## 复核行为 {#verify}

用 `dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release` 检查业务用例。通过 `godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke` 检查原生流程，并用输出标识继续跨进程检查。

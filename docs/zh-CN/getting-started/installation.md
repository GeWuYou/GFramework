---
title: 安装
---

# 安装

## 按需要选择包

| 包 | 用途 |
| --- | --- |
| GeWuYou.GFramework.Core | 引擎无关能力与可选生命周期编排 |
| GeWuYou.GFramework.Core.Abstractions | 独立契约层，由 Core 传递引用 |
| GeWuYou.GFramework.Cqrs | 可选异步请求、通知与流处理 |
| GeWuYou.GFramework.Game | 配置、内容、Mod、存档、场景与 UI |
| GeWuYou.GFramework.Godot | Godot 宿主与原生适配 |
| GeWuYou.GFramework.Ecs.Arch | 可选 Arch ECS 接入 |
| GeWuYou.GFramework.Game.Harmony | 可选 Harmony Mod 支持 |

```bash
dotnet add package GeWuYou.GFramework.Core
# 按需加入游戏及宿主
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Godot
```

目标框架和依赖版本以对应项目与 NuGet 元数据为准。Godot 使用支持 C# 的 .NET 编辑器版本。普通 C# 应用无需安装 Godot、CQRS 或生成器。

## 编译期能力

枚举扩展与 Priority 使用 Core.SourceGenerators 及其随附特性源码。Game schema、CQRS handler registry、Godot 节点与场景生成器按实际功能分别选择，见 [生成器指南](../source-generators/index.md)。

框架 runtime 和 abstractions 不引用生成器或使用生成器特性。普通业务依赖使用构造函数，标准 DI 注册集中在组合根。

## 仓库示例

```bash
dotnet run --project Samples/MinimalApp -c Release
dotnet build Samples/GodotIntegration -c Release
dotnet build Samples/DodgeTheCreeps.Advanced -c Release
```

示例以项目引用使用同一仓库源码；独立消费使用包引用。创建完整游戏的操作步骤见 [基础游戏](../tutorials/godot-complete-project.md) 与 [进阶游戏](../tutorials/godot-advanced-project.md)。

包提供可选择的传递 global using 支持；需要显式控制导入时按包中的 buildTransitive 配置排除或关闭。C# 源码中的 using 必须与实际采用的命名空间一致。

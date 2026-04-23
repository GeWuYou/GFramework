# GFramework

> 面向游戏开发场景的模块化 C# 框架，按运行时、抽象层、引擎集成和源码生成器拆分能力。

[![NuGet Core](https://img.shields.io/badge/NuGet-GeWuYou.GFramework.Core-2C7BE5)](https://www.nuget.org/packages/GeWuYou.GFramework.Core)
[![NuGet Meta](https://img.shields.io/badge/NuGet-GeWuYou.GFramework-1F9D55)](https://www.nuget.org/packages/GeWuYou.GFramework)
[![Godot](https://img.shields.io/badge/Godot-4.6-green)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)
[![zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat-square&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS5zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/GeWuYou/GFramework)

## 从哪里开始

- 第一次接触框架：[`docs/zh-CN/getting-started/index.md`](docs/zh-CN/getting-started/index.md)
- 想先跑一个最小例子：[`docs/zh-CN/getting-started/quick-start.md`](docs/zh-CN/getting-started/quick-start.md)
- 已经知道要用哪个模块：直接进入对应模块目录下的 `README.md`

## 模块地图

| 模块 | 作用 | 入口 |
| --- | --- | --- |
| `GFramework.Core` | 架构、命令、查询、事件、状态、日志、资源、协程等基础运行时 | [README](GFramework.Core/README.md) |
| `GFramework.Core.Abstractions` | `Core` 对应的契约层，适合面向接口开发或做模块拆分 | [README](GFramework.Core.Abstractions/README.md) |
| `GFramework.Cqrs` | 新版 CQRS runtime，提供 request dispatcher、notification publish 与 handler 注册 | [README](GFramework.Cqrs/README.md) |
| `GFramework.Cqrs.Abstractions` | CQRS 消息、处理器、pipeline 行为等契约 | [README](GFramework.Cqrs.Abstractions/README.md) |
| `GFramework.Game` | 面向游戏项目的配置、数据、路由、场景、UI、设置和存储运行时 | [README](GFramework.Game/README.md) |
| `GFramework.Game.Abstractions` | `Game` 对应的契约层 | [README](GFramework.Game.Abstractions/README.md) |
| `GFramework.Godot` | Godot 集成层，负责把框架能力接入节点、场景、UI、设置与存储 | [README](GFramework.Godot/README.md) |
| `GFramework.Ecs.Arch` | Arch ECS 集成 | [README](GFramework.Ecs.Arch/README.md) |
| `GFramework.Ecs.Arch.Abstractions` | Arch ECS 集成对应的契约层，适合共享宿主循环与 ECS 模块边界 | [README](GFramework.Ecs.Arch.Abstractions/README.md) |
| `GFramework.Core.SourceGenerators` | Core 侧通用源码生成器与分析器 | [README](GFramework.Core.SourceGenerators/README.md) |
| `GFramework.Game.SourceGenerators` | 游戏内容配置 schema 生成器 | [README](GFramework.Game.SourceGenerators/README.md) |
| `GFramework.Cqrs.SourceGenerators` | CQRS handler registry 生成器 | [README](GFramework.Cqrs.SourceGenerators/README.md) |
| `GFramework.Godot.SourceGenerators` | Godot 项目元数据、节点注入、信号绑定与 Scene/UI 辅助生成器 | [README](GFramework.Godot.SourceGenerators/README.md) |

## 内部支撑模块

以下目录目前不是独立采用入口，而是跟随所属模块维护的内部支撑组件：

| 目录 | 定位 | 跟随入口 |
| --- | --- | --- |
| `GFramework.Core.SourceGenerators.Abstractions` | `Core.SourceGenerators` 的内部契约层 | [GFramework.Core.SourceGenerators/README.md](GFramework.Core.SourceGenerators/README.md) |
| `GFramework.Godot.SourceGenerators.Abstractions` | `Godot.SourceGenerators` 的内部契约层 | [GFramework.Godot.SourceGenerators/README.md](GFramework.Godot.SourceGenerators/README.md) |
| `GFramework.SourceGenerators.Common` | 生成器家族共享的公共支撑代码 | [docs/zh-CN/source-generators/index.md](docs/zh-CN/source-generators/index.md) |

## 文档导航

仓库根 README 与文档站点保持同一套栏目命名：

- 入门指南：[`docs/zh-CN/getting-started/index.md`](docs/zh-CN/getting-started/index.md)
- Core：[`docs/zh-CN/core/index.md`](docs/zh-CN/core/index.md)
- Game：[`docs/zh-CN/game/index.md`](docs/zh-CN/game/index.md)
- Godot：[`docs/zh-CN/godot/index.md`](docs/zh-CN/godot/index.md)
- 教程：[`docs/zh-CN/tutorials/index.md`](docs/zh-CN/tutorials/index.md)
- 源码生成器：[`docs/zh-CN/source-generators/index.md`](docs/zh-CN/source-generators/index.md)
- ECS：[`docs/zh-CN/ecs/index.md`](docs/zh-CN/ecs/index.md)
- 抽象接口：[`docs/zh-CN/abstractions/index.md`](docs/zh-CN/abstractions/index.md)
- 最佳实践：[`docs/zh-CN/best-practices/index.md`](docs/zh-CN/best-practices/index.md)
- API 参考：[`docs/zh-CN/api-reference/index.md`](docs/zh-CN/api-reference/index.md)
- FAQ：[`docs/zh-CN/faq.md`](docs/zh-CN/faq.md)
- 故障排查：[`docs/zh-CN/troubleshooting.md`](docs/zh-CN/troubleshooting.md)
- 贡献：[`docs/zh-CN/contributing.md`](docs/zh-CN/contributing.md)

## 包选择

- `GeWuYou.GFramework`
  当前是聚合元包，只聚合 `GFramework.Core` 与 `GFramework.Game` 这两层运行时，适合快速试用。
- `GeWuYou.GFramework.Core`
  推荐的最小起步包。先从核心运行时开始，再按需叠加 `Cqrs`、`Game`、`Godot` 和 Source Generators。
- `GeWuYou.GFramework.*.Abstractions`
  适合需要单独依赖契约层、插件化、测试替身或多模块解耦的场景。
- `GeWuYou.GFramework.*.SourceGenerators`
  只在需要编译期生成代码时安装，版本应与运行时包保持一致。

## 最小安装组合

```bash
# 最小起步
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 新版 CQRS
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions

# 游戏层运行时
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# Godot 集成
dotnet add package GeWuYou.GFramework.Godot

# 按需安装源码生成器
dotnet add package GeWuYou.GFramework.Core.SourceGenerators
dotnet add package GeWuYou.GFramework.Game.SourceGenerators
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators
```

## 可选全局 using

NuGet 消费项目可显式开启模块级自动导入：

```xml
<PropertyGroup>
  <EnableGFrameworkGlobalUsings>true</EnableGFrameworkGlobalUsings>
</PropertyGroup>
```

如果只想排除部分命名空间：

```xml
<ItemGroup>
  <GFrameworkExcludedUsing Include="GFramework.Core.Environment" />
  <GFrameworkExcludedUsing Include="GFramework.Godot.Extensions" />
</ItemGroup>
```

> 该能力主要面向 NuGet 消费场景。仓库内 `ProjectReference` 方式仍建议显式维护自己的 `GlobalUsings.cs`。

## 仓库结构

```text
GFramework.sln
├─ GFramework.Core/
├─ GFramework.Core.Abstractions/
├─ GFramework.Cqrs/
├─ GFramework.Cqrs.Abstractions/
├─ GFramework.Game/
├─ GFramework.Game.Abstractions/
├─ GFramework.Godot/
├─ GFramework.Ecs.Arch/
├─ GFramework.Ecs.Arch.Abstractions/
├─ GFramework.Core.SourceGenerators/
├─ GFramework.Core.SourceGenerators.Abstractions/
├─ GFramework.Game.SourceGenerators/
├─ GFramework.Cqrs.SourceGenerators/
├─ GFramework.Godot.SourceGenerators/
├─ GFramework.Godot.SourceGenerators.Abstractions/
├─ GFramework.SourceGenerators.Common/
└─ docs/
```

## 贡献

提交功能或行为变更时，请把代码、测试和文档一起更新：

1. 先阅读对应模块目录下的 `README.md`
2. 如果改动影响采用路径、安装方式、公共 API 或目录结构，同时更新 `docs/zh-CN/`
3. 对跨模块或多阶段任务，维护 `ai-plan/public/README.md` 与对应主题目录下的 tracking / trace

## 许可证

本仓库当前采用 [Apache License 2.0](LICENSE)。

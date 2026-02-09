# GFramework

> 面向游戏开发场景的模块化 C# 框架，核心能力与具体引擎解耦，可按需组合 Core / Game / Godot / Source Generators。

[![NuGet Core](https://img.shields.io/badge/NuGet-GeWuYou.GFramework.Core-2C7BE5)](https://www.nuget.org/packages/GeWuYou.GFramework.Core)
[![NuGet Meta](https://img.shields.io/badge/NuGet-GeWuYou.GFramework-1F9D55)](https://www.nuget.org/packages/GeWuYou.GFramework)
[![Godot](https://img.shields.io/badge/Godot-4.6-green)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-6.0+-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)
[![zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat-square&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/GeWuYou/GFramework)
---

## 项目简介

GFramework 采用清晰分层与模块化设计，强调：

- **架构分层（Architecture / Model / System / Utility）**
- **命令与查询分离（CQRS）**
- **类型安全事件机制**
- **可绑定属性与响应式数据流**
- **可扩展的 IOC/生命周期管理**
- **基于 Roslyn 的源码生成能力**

项目灵感参考自 [QFramework](https://github.com/liangxiegame/QFramework)，并在模块边界、工程组织和可扩展性方面进行了持续重构。

## 功能模块

| 模块 | 说明 | 文档 |
| --- | --- | --- |
| `GFramework.Core` | 平台无关的核心架构能力（架构、命令、查询、事件、属性、IOC、日志等） | [查看](GFramework.Core/README.md) |
| `GFramework.Core.Abstractions` | Core 对应的抽象接口定义 | [查看](GFramework.Core.Abstractions/README.md) |
| `GFramework.Game` | 游戏业务侧扩展（状态、配置、存储、UI 等） | [查看](GFramework.Game/README.md) |
| `GFramework.Game.Abstractions` | Game 模块抽象接口定义 | [查看](GFramework.Game.Abstractions/README.md) |
| `GFramework.Godot` | Godot 集成层（节点扩展、场景/设置/存储适配等） | [查看](GFramework.Godot/README.md) |
| `GFramework.SourceGenerators` | 通用源码生成器（日志、枚举扩展、规则等） | [查看](GFramework.SourceGenerators/README.md) |
| `GFramework.Godot.SourceGenerators` | Godot 场景下的源码生成器扩展 | [查看](GFramework.Godot.SourceGenerators/README.md) |

## 文档导航

- 入门教程：[`docs/tutorials/getting-started.md`](docs/tutorials/getting-started.md)
- Godot 集成：[`docs/tutorials/godot-integration.md`](docs/tutorials/godot-integration.md)
- 进阶模式：[`docs/tutorials/advanced-patterns.md`](docs/tutorials/advanced-patterns.md)
- 最佳实践：[`docs/best-practices/architecture-patterns.md`](docs/best-practices/architecture-patterns.md)
- API 参考：[`docs/api-reference/`](docs/api-reference/)

> 如果你更偏好按模块阅读，建议从各子项目 `README.md` 开始，再回到 `docs/` 查阅专题文档。

## 包选择说明（避免混淆）

- **`GeWuYou.GFramework`**：聚合元包（Meta Package），用于一键引入常用能力集合，适合快速试用或原型阶段。
- **`GeWuYou.GFramework.Core`**：核心起步包，适合希望按模块精细控制依赖的项目（推荐生产项目从此起步）。

如果你已明确技术栈，建议优先按模块安装（Core / Game / Godot / SourceGenerators），避免不必要依赖。

## 快速安装

按实际需求选择依赖：

```bash
# 核心能力（推荐最小起步）
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 游戏扩展
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# Godot 集成（仅 Godot 项目需要）
dotnet add package GeWuYou.GFramework.Godot

# 源码生成器（可选，但推荐）
dotnet add package GeWuYou.GFramework.SourceGenerators
```

## 仓库结构

```text
GFramework.sln
├─ GFramework.Core/
├─ GFramework.Core.Abstractions/
├─ GFramework.Game/
├─ GFramework.Game.Abstractions/
├─ GFramework.Godot/
├─ GFramework.SourceGenerators/
├─ GFramework.Godot.SourceGenerators/
├─ docs/
└─ docfx/
```

## 兼容性

- **运行时/工具链**：基于 .NET 生态，具体以各项目 `*.csproj` 的 `TargetFramework` 为准。
- **引擎集成**：当前提供 Godot 集成模块，Core 层可迁移至其他 .NET 场景。

## 贡献

欢迎提交 Issue 与 Pull Request：

1. Fork 本仓库并创建特性分支
2. 补充必要的测试或文档更新
3. 提交 PR，描述变更背景、方案与验证结果

## 许可证

本项目采用 [Apache License 2.0](LICENSE)。

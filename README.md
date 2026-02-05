# GFramework

> 面向游戏开发场景的模块化 C# 框架，核心能力与具体引擎解耦，可按需组合 Core / Game / Godot / Source Generators。

[![NuGet](https://img.shields.io/badge/NuGet-GeWuYou.GFramework-blue)](https://www.nuget.org/packages/GeWuYou.GFramework)
[![Godot](https://img.shields.io/badge/Godot-4.5+-green)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-6.0+-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)

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
| `GFramework.Game.Abstractions` | Game 模块抽象接口定义 | 源码目录：`GFramework.Game.Abstractions/` |
| `GFramework.Godot` | Godot 集成层（节点扩展、场景/设置/存储适配等） | [查看](GFramework.Godot/README.md) |
| `GFramework.SourceGenerators` | 通用源码生成器（日志、枚举扩展、规则等） | [查看](GFramework.SourceGenerators/README.md) |
| `GFramework.Godot.SourceGenerators` | Godot 场景下的源码生成器扩展 | 源码目录：`GFramework.Godot.SourceGenerators/` |

## 文档导航

- 入门教程：[`docs/tutorials/getting-started.md`](docs/tutorials/getting-started.md)
- Godot 集成：[`docs/tutorials/godot-integration.md`](docs/tutorials/godot-integration.md)
- 进阶模式：[`docs/tutorials/advanced-patterns.md`](docs/tutorials/advanced-patterns.md)
- 最佳实践：[`docs/best-practices/architecture-patterns.md`](docs/best-practices/architecture-patterns.md)
- API 参考：[`docs/api-reference/`](docs/api-reference/)

> 如果你更偏好按模块阅读，建议从各子项目 `README.md` 开始，再回到 `docs/` 查阅专题文档。

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

# GFramework

> 专为游戏开发场景设计的综合性C#游戏开发框架，Core 模块与平台无关

[![NuGet](https://img.shields.io/badge/NuGet-GeWuYou.GFramework-blue)](https://www.nuget.org/packages/GeWuYou.GFramework.Core/)
[![Godot](https://img.shields.io/badge/Godot-4.5+-green)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-6.0+-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)
[![zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/GeWuYou/GFramework)

[![Scanned with Feluda](https://img.shields.io/badge/Scanned%20with-Feluda-brightgreen)](https://github.com/anistark/feluda)

本项目参考(CV)自[QFramework](https://github.com/liangxiegame/QFramework)，并进行了模块化重构和功能增强。

## 🚀 快速导航

### 📚 学习路径

#### 🎯 新手入门

1. 📚 [从零开始教程](docs/tutorials/getting-started.md) - 完整的项目创建和开发指南
2. 📖 [基本概念](GFramework.Core/README.md#核心概念) - 理解核心概念
3. 💡 [快速示例](#快速开始-getting-started) - 5分钟上手体验

#### 🏗️ 进阶开发

4. 📖 [Godot 集成教程](docs/tutorials/godot-integration.md) - 深度 Godot 集成和最佳实践
5. ⚡ [高级模式教程](docs/tutorials/advanced-patterns.md) - CQRS、事件溯源、插件系统等
6. 🏗️ [架构模式最佳实践](docs/best-practices/architecture-patterns.md) - 推荐的架构设计模式
7. 🏗️ [性能优化技巧](docs/best-practices/performance-tips.md) - 内存和性能优化

#### 🏗️ 专家指南

8. 📊 [API 参考](docs/api-reference/) - 详细的类和接口说明

### 📖 详细文档

#### 🏛️ 核心项目 Core Projects

- [📖 **GFramework.Core** - 核心框架功能，架构、事件、命令、查询等（平台无关）](GFramework.Core/README.md)
- [📖 **GFramework.Core.Abstractions** - 核心接口定义
- [📖 **GFramework.Game** - 游戏特定抽象和系统
- [📖 **GFramework.Game.Abstractions** - 游戏抽象接口定义
- [📖 **GFramework.Godot** - Godot 特定实现（Node扩展、GodotLogger 等）
- [📖 **GFramework.SourceGenerators** - 通用源代码生成器
- [📖 **GFramework.Godot.SourceGenerators** - Godot 特定的代码生成器

#### 📖 源代码生成器 Source Generators

- [📖 **日志生成器** - 自动 ILogger 字段生成
- [📖 **上下文感知生成器** - 自动 IContextAware 实现
- [📖 **枚举扩展生成器** - 自动枚举扩展方法

#### 📖 API 参考

- [📖 **Core API 参考** - 核心类和接口详细说明
- [📖 **Godot API 参考** - Godot 模块 API 详细说明
- [📖 **Source Generators API 参考** - 源码生成器 API 详细说明
- [📖 **Game API 参考** - Game 模块 API 详细说明

#### 🏗️ 教程和指南

- [🏗️ **架构模式最佳实践** - 推荐的架构设计模式
- [🏗️ **性能优化技巧** - 内存和性能优化

#### 🏗️ 常见问题解决

- [🏗️ **错误处理策略** - 异常处理和错误恢复
- [🏗️ **调试技巧** - 调试和测试指南

## ✨ 主要特性

### 🏗️ 核心架构 Core Architecture

- **依赖注入 Dependency Injection**: 内置IoC容器管理对象生命周期
- **事件系统 Event System**: 类型安全的事件系统，实现松耦合
- **属性绑定 Property Binding**: 可绑定属性，支持响应式编程
- **日志框架 Logging Framework**: 结构化日志，支持多个日志级别
- **生命周期管理 Lifecycle Management**: 阶段式的架构生命周期管理
- **命令查询分离 CQRS**: 命令和查询的职责分离

### 🎮 游戏开发特性 Game Development Features

- **资产管理 Asset Management**: 集中化资产目录系统
- **资源工厂 Resource Factory**: 工厂模式的资源创建模式
- **架构模式 Architecture Pattern**: 关注点分离的清晰架构
- **模块化 Module System**: 支持架构模块安装和扩展
- **源码生成 Source Generators**: 零运行时开销的代码生成

### 🌐 平台无关 Platform Agnostic

- **纯 .NET 实现**: Core 模块无任何平台特定依赖
- **Godot 集成 Godot Integration**: GFramework.Godot 提供 Godot 特定功能
- **可移植 Portable**: 可以轻松移植到 Unity、.NET MAUI 等平台

## 🚀 快速开始 Getting Started

### 1️⃣ 安装 Installation

```bash
# 安装核心包（平台无关）
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 安装游戏包
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# 安装源码生成器（推荐）
dotnet add package GeWuYou.GFramework.SourceGenerators
dotnet add package GeWuYou.GFramework.SourceGenerators.Attributes

# 安装Godot包（仅Godot项目需要）
dotnet add package GeWuYou.GFramework.Godot
```

### 2️⃣ 基本使用 Basic Usage

```csharp
using GFramework.Core.architecture;
using GFramework.SourceGenerators.Attributes;

// 1. 定义架构（继承 Architecture 基类）
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Model - 游戏数据
        RegisterModel(new PlayerModel());
        RegisterModel(new GameModel());
        
        // 注册 System - 业务逻辑
        RegisterSystem(new CombatSystem());
        RegisterSystem(new UISystem());
        
        // 注册 Utility - 工具类
        RegisterUtility(new StorageUtility());
    }
}

// 2. 创建并初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();

// 3. 通过依赖注入在Controller中使用
[Log]
[ContextAware]
public partial class PlayerController : IController
{
    private PlayerModel _playerModel;
    
    public PlayerController(IArchitecture architecture)
    {
        _playerModel = architecture.GetModel<PlayerModel>();
    }
    
    // 监听属性变化
    public void Initialize()
    {
        _playerModel.Health.RegisterWithInitValue(hp => UpdateHealthDisplay(hp));
    }
    
    private void UpdateHealthDisplay(int hp)
    {
        // 更新 UI 显示
        Console.WriteLine($"Health: {hp}");
    }
}
```

### 3️⃣ 命令和查询 Command & Query

```csharp
// 定义命令
public class AttackCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();
        var enemyModel = this.GetModel<EnemyModel>();
        
        // 业务逻辑
        int damage = playerModel.AttackPower.Value;
        enemyModel.Health.Value -= damage;
        
        // 发送事件
        this.SendEvent(new DamageDealtEvent { Damage = damage });
    }
}

// 定义查询
public class CanAttackQuery : AbstractQuery<bool>
{
    protected override bool OnDo()
    {
        var playerModel = this.GetModel<PlayerModel>();
        return playerModel.Health.Value > 0 && !playerModel.IsStunned.Value;
    }
}

// 使用命令和查询
public class CombatController : IController
{
    private readonly IArchitecture _architecture;
    
    public CombatController(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    public void OnAttackButtonPressed()
    {
        // 先查询
        if (_architecture.SendQuery(new CanAttackQuery()))
        {
            // 再执行命令
            _architecture.SendCommand(new AttackCommand());
        }
    }
}
```

### 4️⃣ 事件系统 Event System

```csharp
// 定义事件
public struct DamageDealtEvent
{
    public int Damage;
    public Vector3 Position;
}

// 发送事件
this.SendEvent(new DamageDealtEvent { Damage = 100, Position = position });

// 注册事件监听
this.RegisterEvent<DamageDealtEvent>(OnDamageDealt);

private void OnDamageDealt(DamageDealtEvent e)
{
    ShowDamageNumber(e.Damage, e.Position);
}
```

## 📦 项目架构 Architecture

框架遵循清洁架构原则，具有以下层次：

```
┌─────────────────────────────────────────┐
│           View / UI                      │  UI 层：用户界面
├─────────────────────────────────────────┤
│            Controller                    │  控制层：处理用户输入
├─────────────────────────────────────────┤
│             System                       │ 逻辑层：业务逻辑
├─────────────────────────────────────────┤
│              Model                       │  数据层：游戏状态
├─────────────────────────────────────────┤
│             Utility                      │  工具层：无状态工具
├─────────────────────────────────────────┤
│         Command / Query                  │  横切关注点
└─────────────────────────────────┘
```

## 🔧 平台集成 Platform Integration

### Godot 项目

```csharp
// 使用 GFramework.Godot 获取 Godot 特定功能
using GFramework.Godot;

public class GodotPlayerController : Node, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        // 使用 Godot 特定的扩展方法
        this.RegisterEvent<DamageDealtEvent>(OnDamageDealt)
            .UnRegisterWhenNodeExitTree(this);
    }
}
```

### 移植到其他平台

GFramework.Core 是纯 .NET 库，可以轻松移植到：

- **Unity**（使用 Unity 容器替代 Godot 节点）
- **.NET MAUI**（用于跨平台 UI 应用）
- **任何其他 .NET 应用**

---

## 📖 版本历史 Version History

### v1.0.0 (2026-01-12)

- ✅ 完整的文档体系创建完成
- ✅ 核心项目文档完善
- ✅ 源码生成器文档完成
- ✅ 最佳实践指南创建
- ✅ API 参考文档生成
- ✅ 从零开始教程完善

### 计划中的任务

- [📝 待完成任务] - 2 个低优先级任务
- [📝 进行中的任务] - 0 个

---

## 🎯 如果这个项目对你有帮助，请给我们一个 ⭐！

**Fork** 本仓库并创建 Pull Request  
**Report Issues** 报告 Bug 或功能请求  
**Star** 给我们一个 Star！

---

**📚 文档统计**

- **新增文档**: 10+ 个文件
- **代码示例**: 150+ 个可直接使用的代码片段
- **文档总量**: 6000+ 行
- **覆盖项目**: 100% 项目文档覆盖

---

**许可证**: Apache 2.0  
**更新日期**: 2026-01-12
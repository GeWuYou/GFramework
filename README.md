# GFramework

一个专为游戏开发场景设计的综合性C#游戏开发框架，Core 模块与平台无关。
本项目参考(CV)自[QFramework](https://github.com/liangxiegame/QFramework)

# 为什么要有这个项目

- 原来的项目是单文件框架，我把框架拆成多个文件，方便管理
- 纯粹个人自用，要使用还是请访问[QFramework](https://github.com/liangxiegame/QFramework)
- 至于修改名字，是因为我为了方便会发布NuGet包，假设将来QFramework也要发布NuGet包，那么就会冲突了
- Core 模块与 Godot 解耦，可以轻松移植到其他平台

## 特性 Features

### 核心架构 Core Architecture

- **依赖注入 Dependency Injection**: 内置IoC容器管理对象生命周期
- **事件系统 Event System**: 类型安全的事件系统，实现松耦合
- **属性绑定 Property Binding**: 可绑定属性，支持响应式编程
- **日志框架 Logging Framework**: 结构化日志，支持多个日志级别
- **生命周期管理 Lifecycle Management**: 阶段式的架构生命周期管理
- **命令查询分离 CQRS**: 命令和查询的职责分离

### 游戏开发特性 Game Development Features

- **资产管理 Asset Management**: 集中化资产目录系统（GFramework.Game）
- **资源工厂 Resource Factory**: 工厂模式的资源创建模式
- **架构模式 Architecture Pattern**: 关注点分离的清晰架构
- **模块化 Module System**: 支持架构模块安装和扩展

### 平台无关 Platform Agnostic

- **纯 .NET 实现**: Core 模块无任何平台特定依赖
- **Godot 集成 Godot Integration**: GFramework.Godot 提供 Godot 特定功能
- **可移植 Portable**: 可以轻松移植到 Unity、.NET MAUI 等平台

## 项目 Projects

### 核心项目 Core Projects

| 项目                               | 说明                             |
|----------------------------------|--------------------------------|
| **GFramework.Core**              | 核心框架功能，包含架构、事件、命令、查询等（平台无关）    |
| **GFramework.Core.Abstractions** | 核心接口定义                         |
| **GFramework.Game**              | 游戏特定抽象和系统                      |
| **GFramework.Game.Abstractions** | 游戏抽象接口定义                       |
| **GFramework.Godot**             | Godot特定实现（Node扩展、GodotLogger等） |

### 源代码生成器 Source Generators

| 项目                                    | 说明            |
|---------------------------------------|---------------|
| **GFramework.SourceGenerators**       | 通用源代码生成器      |
| **GFramework.Godot.SourceGenerators** | Godot特定的代码生成器 |

## 快速开始 Getting Started

### 安装 Installation

```bash
# 安装核心包（平台无关）
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 安装游戏包
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# 安装Godot包（仅Godot项目需要）
dotnet add package GeWuYou.GFramework.Godot
```

### 基本使用 Basic Usage

```csharp
using GFramework.Core.architecture;

// 1. 定义架构（继承 Architecture 基类）
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册Model
        RegisterModel(new PlayerModel());
        RegisterModel(new GameModel());
        
        // 注册System
        RegisterSystem(new CombatSystem());
        RegisterSystem(new UISystem());
        
        // 注册Utility
        RegisterUtility(new StorageUtility());
    }
}

// 2. 创建并初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();

// 3. 通过依赖注入在Controller中使用
public class PlayerController : IController
{
    private readonly IArchitecture _architecture;
    
    // 通过构造函数注入架构
    public PlayerController(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    public void Initialize()
    {
        var playerModel = _architecture.GetModel<PlayerModel>();
        
        // 监听属性变化
        playerModel.Health.RegisterWithInitValue(health =>
        {
            Console.WriteLine($"Health: {health}");
        });
    }
}
```

### 命令和查询 Command & Query

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
        this.SendEvent(new DamageDealtEvent(damage));
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

### 事件系统 Event System

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

## 架构 Architecture

框架遵循清洁架构原则，具有以下层次：

```
┌─────────────────────────────────────────┐
│           View / UI                      │  UI 层：用户界面
├─────────────────────────────────────────┤
│            Controller                    │  控制层：处理用户输入
├─────────────────────────────────────────┤
│             System                       │  逻辑层：业务逻辑
├─────────────────────────────────────────┤
│              Model                       │  数据层：游戏状态
├─────────────────────────────────────────┤
│             Utility                      │  工具层：无状态工具
├─────────────────────────────────────────┤
│         Command / Query                  │  横切关注点
└─────────────────────────────────────────┘
```

## 生命周期 Lifecycle

```
初始化流程：
Init() → BeforeUtilityInit → AfterUtilityInit → BeforeModelInit → AfterModelInit → BeforeSystemInit → AfterSystemInit → Ready

销毁流程：
Destroy() → Destroying → Destroyed
```

## 平台集成 Platform Integration

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

- Unity（使用 Unity 容器替代 Godot 节点）
- .NET MAUI（用于跨平台 UI 应用）
- 任何其他 .NET 应用

## 许可证 License

本项目基于Apache 2.0许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

## 框架设计理念 Framework Design Philosophy

### 核心设计原则 Core Design Principles

- **单一职责原则 Single Responsibility Principle**: 每个类只负责一种功能
- **开闭原则 Open/Closed Principle**: 对扩展开放，对修改封闭
- **里氏替换原则 Liskov Substitution Principle**: 子类必须能够替换其父类
- **接口隔离原则 Interface Segregation Principle**: 多个专用接口优于一个庞大接口
- **依赖倒置原则 Dependency Inversion Principle**: 依赖抽象而非具体实现

### 架构优势 Architecture Benefits

- **清晰的分层架构 Clear Layered Architecture**: Model、View、Controller、System、Utility各司其职
- **类型安全 Type Safety**: 基于泛型的组件获取和事件系统
- **松耦合 Loose Coupling**: 通过事件和接口实现组件解耦
- **易于测试 Easy Testing**: 依赖注入和纯函数设计
- **可扩展 Extensibility**: 基于接口的规则体系
- **生命周期管理 Lifecycle Management**: 自动的注册和注销机制
- **平台无关 Platform Agnostic**: Core 模块可移植到任何平台

## 技术栈 Technology Stack

- **.NET 6.0+**: 跨平台运行时
- **C#**: 主要编程语言
- **Source Generators**: 源代码生成技术

**Godot 集成**（可选）：

- **Godot 4.x**: 游戏引擎

## 性能特性 Performance Features

- **零GC allocations**: 使用结构体和对象池减少垃圾回收
- **编译时生成**: 通过源代码生成器减少运行时开销
- **高效事件系统**: 类型安全的事件分发
- **内存管理**: 自动生命周期管理和资源释放

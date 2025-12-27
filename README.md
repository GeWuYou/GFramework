# GFramework

一个专为Godot和通用游戏开发场景设计的综合性C#游戏开发框架。
本项目参考(CV)自[QFramework](https://github.com/liangxiegame/QFramework)

# 为什么要有这个项目

- 原来的项目是单文件框架，我把框架拆成多个文件，方便管理
- 纯粹个人自用，要使用还是请访问[QFramework](https://github.com/liangxiegame/QFramework)
- 至于修改名字，是因为我为了方便会发布GuGet包，假设将来QFramework也要发布GuGet包，那么就会冲突了

## 特性 Features

### 核心架构 Core Architecture

- **依赖注入 Dependency Injection**: 内置IoC容器管理对象生命周期
- **事件系统 Event System**: 类型安全的事件系统，实现松耦合
- **属性绑定 Property Binding**: 可绑定属性，支持响应式编程
- **日志框架 Logging Framework**: 结构化日志，支持多个日志级别

### 游戏开发特性 Game Development Features

- **资产管理 Asset Management**: 集中化资产目录系统
- **资源工厂 Resource Factory**: 工厂模式的资源创建模式
- **架构模式 Architecture Pattern**: 关注点分离的清晰架构

### Godot集成 Godot Integration

- **Godot特定扩展 Godot-Specific Extensions**: Godot开发的扩展和工具
- **节点扩展 Node Extensions**: Godot节点类的有用扩展
- **Godot日志器 Godot Logger**: Godot应用程序的专用日志系统

## 项目 Projects

### 核心项目 Core Projects

- **GFramework.Core**: 核心框架功能
- **GFramework.Game**: 游戏特定抽象和系统
- **GFramework.Godot**: Godot特定实现

### 源代码生成器 Source Generators

- **GFramework.SourceGenerators**: 自动代码生成的代码生成器
- **GFramework.Godot.SourceGenerators**: Godot特定的代码生成器
- **GFramework.SourceGenerators.Abstractions**: 源代码生成器的抽象
- **GFramework.Godot.SourceGenerators.Abstractions**: Godot特定的抽象

## 快速开始 Getting Started

### 安装 Installation

1. 安装NuGet包：

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.SourceGenerators
dotnet add package GeWuYou.GFramework.Godot
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators
```

### 基本使用 Basic Usage

```csharp
// 创建架构实例 Create an architecture instance
var architecture = new MyArchitecture();

// 初始化架构 Initialize the architecture
await architecture.InitializeAsync();

// 访问服务 Access services
var service = architecture.Container.Resolve<IMyService>();
```

### Godot集成 Godot Integration

```csharp
// 使用Godot特定功能 Use Godot-specific features
[GodotLog]
public partial class MyGodotNode : Node
{
    // 自动生成的日志器将可用 Auto-generated logger will be available
    private readonly ILogger _log = Log.GetLogger("MyGodotNode");
    
    public override void _Ready()
    {
        _log.Info("Node is ready!");
    }
}
```

## 架构 Architecture

框架遵循清洁架构原则，具有以下层次：

1. **核心层 Core Layer**: 基础抽象和接口 Fundamental abstractions and interfaces
2. **应用层 Application Layer**: 用例和应用服务 Use cases and application services
3. **基础设施层 Infrastructure Layer**: 外部依赖和实现 External dependencies and implementations
4. **表示层 Presentation Layer**: UI和用户交互组件 UI and user interaction components

## 许可证 License

本项目基于Apache 2.0许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

## 支持 Support

如需支持和问题，请在仓库中提交问题。

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

## 技术栈 Technology Stack

- **.NET 6.0+**: 跨平台运行时
- **Godot 4.x**: 游戏引擎
- **C#**: 主要编程语言
- **Source Generators**: 源代码生成技术

## 性能特性 Performance Features

- **零GC allocations**: 使用结构体和对象池减少垃圾回收
- **编译时生成**: 通过源代码生成器减少运行时开销
- **高效事件系统**: 类型安全的事件分发
- **内存管理**: 自动生命周期管理和资源释放
# GFramework.Core 核心框架

> 一个基于 CQRS、MVC 和事件驱动的轻量级游戏开发架构框架

## 目录

- [框架概述](#框架概述)
- [核心概念](#核心概念)
- [架构图](#架构图)
- [快速开始](#快速开始)
- [包说明](#包说明)
- [组件联动](#组件联动)
- [最佳实践](#最佳实践)
- [设计理念](#设计理念)

## 框架概述

本框架是一个与平台无关的轻量级架构，它结合了多种经典设计模式：

- **MVC 架构模式** - 清晰的层次划分
- **CQRS 模式** - 命令查询职责分离
- **IoC/DI** - 依赖注入和控制反转
- **事件驱动** - 松耦合的组件通信
- **响应式编程** - 可绑定属性和数据流
- **阶段式生命周期管理** - 精细化的架构状态控制

**重要说明**：GFramework.Core 是与平台无关的核心模块，不包含任何 Godot 特定代码。Godot 集成功能在 GFramework.Godot 包中实现。

### 核心特性

- **清晰的分层架构** - Model、View、Controller、System、Utility 各司其职
- **类型安全** - 基于泛型的组件获取和事件系统
- **松耦合** - 通过事件和接口实现组件解耦
- **易于测试** - 依赖注入和纯函数设计
- **可扩展** - 基于接口的规则体系
- **生命周期管理** - 自动的注册和注销机制
- **模块化** - 支持架构模块安装
- **平台无关** - Core 模块可以在任何 .NET 环境中使用

## 核心概念

### 五层架构

```
┌─────────────────────────────────────────┐
│             View / UI                    │  UI 层：用户界面
├─────────────────────────────────────────┤
│            Controller                    │  控制层：处理用户输入
├─────────────────────────────────────────┤
│             System                       │  逻辑层：业务逻辑
├─────────────────────────────────────────┤
│              Model                       │  数据层：游戏状态
├─────────────────────────────────────────┤
│             Utility                      │  工具层：无状态工具
└─────────────────────────────────────────┘
```

### 横切关注点

```
Command ──┐
Query   ──┼──→  跨层操作（修改/查询数据）
Event   ──┘
```

### 架构阶段

```
初始化：Init → BeforeUtilityInit → AfterUtilityInit → BeforeModelInit → AfterModelInit → BeforeSystemInit → AfterSystemInit → Ready
销毁：Destroy → Destroying → Destroyed
```

## 架构图

### 整体架构

```
                     ┌──────────────────┐
                     │   Architecture   │ ← 管理所有组件
                     └────────┬─────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
     ┌───▼────┐          ┌───▼────┐          ┌───▼─────┐
     │ Model  │          │ System │          │ Utility │
     │  层    │          │  层    │          │  层     │
     └───┬────┘          └───┬────┘          └────────┘
         │                   │
         │    ┌─────────────┤
         │    │             │
     ┌───▼────▼───┐    ┌───▼──────┐
     │ Controller │    │ Command/ │
     │    层      │    │  Query   │
     └─────┬──────┘    └──────────┘
           │
     ┌─────▼─────┐
     │   View    │
     │    UI     │
     └───────────┘
```

### 数据流向

```
用户输入 → Controller → Command → System → Model → Event → Controller → View 更新

查询流程：Controller → Query → Model → 返回数据
```

## 快速开始

本框架采用"约定优于配置"的设计理念，只需 4 步即可搭建完整的架构。

### 为什么需要这个框架？

在传统开发中，我们经常遇到这些问题：

- 代码耦合严重：UI 直接访问游戏逻辑，逻辑直接操作 UI
- 难以维护：修改一个功能需要改动多个文件
- 难以测试：业务逻辑和 UI 混在一起无法独立测试
- 难以复用：代码紧密耦合，无法在其他项目中复用

本框架通过清晰的分层解决这些问题。

### 1. 定义架构（Architecture）

**作用**：Architecture 是整个应用的"中央调度器"，负责管理所有组件的生命周期。

```csharp
using GFramework.Core.architecture;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Model - 游戏数据
        RegisterModel(new PlayerModel());
        
        // 注册 System - 业务逻辑
        RegisterSystem(new CombatSystem());
        
        // 注册 Utility - 工具类
        RegisterUtility(new StorageUtility());
    }
}
```

**优势**：

- **依赖注入**：组件通过上下文获取架构引用
- **集中管理**：所有组件注册在一处，一目了然
- **生命周期管理**：自动初始化和销毁
- **平台无关**：可以在任何 .NET 环境中使用

### 2. 定义 Model（数据层）

**作用**：Model 是应用的"数据库"，只负责存储和管理状态。

```csharp
public class PlayerModel : AbstractModel
{
    // 使用 BindableProperty 实现响应式数据
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> Gold { get; } = new(0);
    
    protected override void OnInit()
    {
        // Model 中可以监听自己的数据变化
        Health.Register(hp =>
        {
            if (hp <= 0) this.SendEvent(new PlayerDiedEvent());
        });
    }
}

// 也可以不使用 BindableProperty
public class PlayerModel : AbstractModel
{
    public int Health { get; private set; }
    public int Gold { get; private set; }
    
    protected override void OnInit()
    {
        Health = 100;
        Gold = 0;
    }
}
```

**优势**：

- **数据响应式**：BindableProperty 让数据变化自动通知监听者
- **职责单一**：只存储数据，不包含复杂业务逻辑
- **易于测试**：可以独立测试数据逻辑

### 3. 定义 System（业务逻辑层）

**作用**：System 是应用的"大脑"，处理所有业务逻辑。

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // System 通过事件驱动，响应游戏中的各种事件
        this.RegisterEvent<EnemyAttackEvent>(OnEnemyAttack);
    }
    
    private void OnEnemyAttack(EnemyAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 处理业务逻辑：计算伤害、更新数据
        playerModel.Health.Value -= e.Damage;
        
        // 发送事件通知其他组件
        this.SendEvent(new PlayerTookDamageEvent { Damage = e.Damage });
    }
}
```

**优势**：

- **事件驱动**：通过事件解耦，不同 System 之间松耦合
- **可组合**：多个 System 协同工作，每个专注自己的领域
- **易于扩展**：新增功能只需添加新的 System 和事件监听

### 4. 定义 Controller（控制层）

**作用**：Controller 是"桥梁"，连接 UI 和业务逻辑。

```csharp
public class PlayerController : IController
{
    // 通过依赖注入获取架构
    private readonly IArchitecture _architecture;
    
    public PlayerController(IArchitecture architecture)
    {
        _architecture = architecture;
    }
    
    // 监听模型变化
    public void Initialize()
    {
        var playerModel = _architecture.GetModel<PlayerModel>();
        
        // 数据绑定：Model 数据变化自动更新 UI
        playerModel.Health.RegisterWithInitValue(OnHealthChanged);
    }
    
    private void OnHealthChanged(int hp)
    {
        // 更新 UI 显示
        UpdateHealthDisplay(hp);
    }
    
    private void UpdateHealthDisplay(int hp) { /* UI 更新逻辑 */ }
}
```

**优势**：

- **自动更新 UI**：通过 BindableProperty，数据变化自动反映到界面
- **分离关注点**：UI 逻辑和业务逻辑完全分离
- **易于测试**：可以通过依赖注入模拟架构进行测试

### 完成！现在你有了一个完整的架构

这 4 步完成后，你就拥有了：

- **清晰的数据层**（Model）
- **独立的业务逻辑**（System）
- **灵活的控制层**（Controller）
- **统一的生命周期管理**（Architecture）

### 下一步该做什么？

1. **添加 Command**：封装用户操作（如购买物品、使用技能）
2. **添加 Query**：封装数据查询（如查询背包物品数量）
3. **添加更多 System**：如任务系统、背包系统、商店系统
4. **使用 Utility**：添加工具类（如存档工具、数学工具）
5. **使用模块**：通过 IArchitectureModule 扩展架构功能

## 包说明

| 包名               | 职责              | 文档                           |
|------------------|-----------------|------------------------------|
| **architecture** | 架构核心，管理所有组件生命周期 | [查看](architecture/README.md) |
| **constants**    | 框架常量定义          | 本文档                          |
| **model**        | 数据模型层，存储状态      | [查看](model/README.md)        |
| **system**       | 业务逻辑层，处理业务规则    | [查看](system/README.md)       |
| **controller**   | 控制器层，连接视图和逻辑    | (在 Abstractions 中)           |
| **utility**      | 工具类层，提供无状态工具    | [查看](utility/README.md)      |
| **command**      | 命令模式，封装写操作      | [查看](command/README.md)      |
| **query**        | 查询模式，封装读操作      | [查看](query/README.md)        |
| **events**       | 事件系统，组件间通信      | [查看](events/README.md)       |
| **property**     | 可绑定属性，响应式编程     | [查看](property/README.md)     |
| **ioc**          | IoC 容器，依赖注入     | [查看](ioc/README.md)          |
| **rule**         | 规则接口，定义组件约束     | [查看](rule/README.md)         |
| **extensions**   | 扩展方法，简化 API 调用  | [查看](extensions/README.md)   |
| **logging**      | 日志系统，记录运行日志     | [查看](logging/README.md)      |
| **environment**  | 环境接口，提供运行环境信息   | [查看](environment/README.md)  |

## 组件联动

### 1. 初始化流程

```
创建 Architecture 实例
    └─> Init()
        ├─> RegisterModel → Model.SetContext() → Model.Init()
        ├─> RegisterSystem → System.SetContext() → System.Init()
        └─> RegisterUtility → Utility 注册到容器
```

### 2. Command 执行流程

```
Controller.SendCommand(command)
    └─> command.Execute()
        └─> command.OnDo()  // 子类实现
            ├─> GetModel<T>()    // 获取数据
            ├─> 修改 Model 数据
            └─> SendEvent()      // 发送事件
```

### 3. Event 传播流程

```
组件.SendEvent(event)
    └─> TypeEventSystem.Send(event)
        └─> 通知所有订阅者
            ├─> Controller 响应 → 更新 UI
            ├─> System 响应 → 执行逻辑
            └─> Model 响应 → 更新状态
```

### 4. BindableProperty 数据绑定

```
Model: BindableProperty<int> Health = new(100);
Controller: Health.RegisterWithInitValue(hp => UpdateUI(hp))
修改值: Health.Value = 50 → 触发所有回调 → 更新 UI
```

## 最佳实践

### 1. 分层职责原则

每一层都有明确的职责边界，遵循这些原则能让代码更清晰、更易维护。

**Model 层**：

```csharp
// 好：只存储数据
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    protected override void OnInit() { }
}

// 坏：包含业务逻辑
public class PlayerModel : AbstractModel
{
    public void TakeDamage(int damage)  // 业务逻辑应在 System
    {
        Health.Value -= damage;
        if (Health.Value <= 0) Die();
    }
}
```

**System 层**：

```csharp
// 好：处理业务逻辑
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        var target = this.GetModel<PlayerModel>();
        int finalDamage = CalculateDamage(e.BaseDamage, target);
        target.Health.Value -= finalDamage;
    }
}
```

### 2. 通信方式选择指南

| 通信方式                 | 使用场景      | 优势       |
|----------------------|-----------|----------|
| **Command**          | 用户操作、修改状态 | 可撤销、可记录  |
| **Query**            | 查询数据、检查条件 | 明确只读意图   |
| **Event**            | 通知其他组件    | 松耦合、可扩展  |
| **BindableProperty** | 数据变化通知    | 自动化、不会遗漏 |

### 3. 生命周期管理

**为什么需要注销？**

忘记注销监听器会导致：

- **内存泄漏**：对象无法被 GC 回收
- **逻辑错误**：已销毁的对象仍在响应事件

```csharp
// 使用 UnRegisterList 统一管理
private IUnRegisterList _unregisterList = new UnRegisterList();

public void Initialize()
{
    this.RegisterEvent<Event1>(OnEvent1)
        .AddToUnregisterList(_unregisterList);
    
    model.Property.Register(OnPropertyChanged)
        .AddToUnregisterList(_unregisterList);
}

public void Cleanup()
{
    _unregisterList.UnRegisterAll();
}
```

### 4. 性能优化技巧

```csharp
// 低效：每帧都查询
var model = _architecture.GetModel<PlayerModel>();  // 频繁调用

// 高效：缓存引用
private PlayerModel _playerModel;

public void Initialize()
{
    _playerModel = _architecture.GetModel<PlayerModel>();  // 只查询一次
}
```

## 设计理念

框架的设计遵循 SOLID 原则和经典设计模式。

### 1. 单一职责原则（SRP）

- **Model**：只负责存储数据
- **System**：只负责处理业务逻辑
- **Controller**：只负责协调和输入处理
- **Utility**：只负责提供工具方法

### 2. 开闭原则（OCP）

- 通过**事件系统**添加新功能，无需修改现有代码
- 新的 System 可以监听现有事件，插入自己的逻辑

### 3. 依赖倒置原则（DIP）

- 所有组件通过接口交互
- 通过 IoC 容器注入依赖
- 易于替换实现和编写测试

### 4. 接口隔离原则（ISP）

```csharp
// 小而专注的接口
public interface ICanGetModel : IBelongToArchitecture { }
public interface ICanSendCommand : IBelongToArchitecture { }
public interface ICanRegisterEvent : IBelongToArchitecture { }

// 组合需要的能力
public interface IController :
    ICanGetModel,
    ICanSendCommand,
    ICanRegisterEvent { }
```

### 5. 组合优于继承

通过接口组合获得能力，而不是通过继承。

### 框架核心设计模式

| 设计模式      | 应用位置       | 解决的问题    | 带来的好处  |
|-----------|------------|----------|--------|
| **工厂模式**  | IoC 容器     | 组件的创建和管理 | 解耦创建逻辑 |
| **观察者模式** | Event 系统   | 组件间的通信   | 松耦合通信  |
| **命令模式**  | Command    | 封装操作请求   | 支持撤销重做 |
| **策略模式**  | System     | 不同的业务逻辑  | 易于切换策略 |
| **依赖注入**  | 整体架构       | 组件间的依赖   | 自动管理依赖 |
| **模板方法**  | Abstract 类 | 定义算法骨架   | 统一流程规范 |

### 平台无关性

- **GFramework.Core**：纯 .NET 库，无任何平台特定代码
- **GFramework.Godot**：Godot 特定实现，包含 Node 扩展、GodotLogger 等
- 可以轻松将 Core 框架移植到其他平台（Unity、.NET MAUI 等）

---

**版本**: 1.0.0
**许可证**: Apache 2.0

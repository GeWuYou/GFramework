# Godot 集成概览

GFramework.Godot 提供与 Godot 引擎的深度集成，让开发者能够在保持框架架构优势的同时，充分利用 Godot 的强大功能。

## 核心特性

### 🎮 Godot 深度集成

- **架构生命周期绑定** - 自动同步框架和 Godot 节点生命周期
- **丰富的 Node 扩展** - 50+ 个实用的节点扩展方法
- **类型安全信号** - 强类型的信号连接和处理
- **高效对象池** - 专门的 Node 对象池系统

### 🔧 开发者友好

- **零配置集成** - 简单的安装和配置过程
- **流畅的 API** - 链式调用和现代 C# 语法
- **自动清理** - 智能的资源和事件监听器管理
- **调试支持** - 完善的日志和调试工具

## 核心组件

### 架构集成

- **AbstractArchitecture** - Godot 架构基类
- **AbstractGodotModule** - Godot 模块基类
- **NodeController** - 节点控制器基类

### 节点扩展

- **[NodeExtensions](/godot/node-extensions/node-extensions)** - 节点扩展方法
- **SignalBuilder** - 信号构建器
- **AsyncExtensions** - 异步扩展

### 对象池系统

- **[AbstractNodePoolSystem](/godot/pooling/node-pool)** - 节点池基类
- **NodePoolManager** - 节点池管理器
- **PoolableObject** - 可池化对象接口

### 日志系统

- **[GodotLogger](/godot/logging/godot-logger)** - Godot 集成日志
- **LoggerFactory** - 日志工厂
- **Log Attributes** - 日志特性

## 使用场景

Godot 模块适用于所有使用 Godot 引擎的项目：

- **2D 游戏** - 平台游戏、RPG、策略游戏
- **3D 游戏** - 动作游戏、射击游戏、模拟游戏
- **工具应用** - 编辑器、可视化工具
- **原型开发** - 快速游戏原型制作

## 安装要求

```bash
# 需要安装 Core 和 Game 模块
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Godot

# 需要 Godot 4.5+ 版本
```

## 快速示例

```csharp
using GFramework.Godot.architecture;
using GFramework.Godot.extensions;

[ContextAware]
[Log]
public partial class PlayerController : CharacterBody2D, IController
{
    private PlayerModel _playerModel;
    
    public override void _Ready()
    {
        _playerModel = Context.GetModel<PlayerModel>();
        
        // 安全的节点操作
        var healthBar = GetNodeX<ProgressBar>("UI/HealthBar");
        
        // 类型安全的信号连接
        this.CreateSignalBuilder(Button.SignalName.Pressed)
            .UnRegisterWhenNodeExitTree(this)
            .Connect(OnButtonPressed);
            
        // 响应式数据绑定
        _playerModel.Health.Register(OnHealthChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnButtonPressed()
    {
        Context.SendCommand(new AttackCommand());
    }
    
    private void OnHealthChanged(int newHealth)
    {
        // 自动更新 UI
        var healthBar = GetNode<ProgressBar>("UI/HealthBar");
        healthBar.Value = newHealth;
    }
}
```

## 学习路径

建议按以下顺序学习 Godot 集成：

1. **[架构集成](/godot/integration/architecture-integration)** - 了解集成基础
2. **[节点扩展](/godot/node-extensions/node-extensions)** - 掌握扩展方法
3. **[对象池](/godot/pooling/node-pool)** - 学习性能优化
4. **[日志系统](/godot/logging/godot-logger)** - 掌握调试工具

## 与其它模块的关系

```
GFramework.Core (基础架构)
    ↓
GFramework.Game (游戏功能)
    ↓
GFramework.Godot (Godot 集成)
```

Godot 模块建立在 Core 和 Game 模块之上，提供与 Godot 引擎的无缝集成。

## 性能特点

- **零额外开销** - 扩展方法编译时优化
- **内存安全** - 自动资源管理和清理
- **类型安全** - 编译时类型检查
- **异步支持** - 完善的异步操作支持

## 常见用法

### 节点安全操作

```csharp
// 安全获取节点
var player = GetNodeX<Player>("Player");
var child = FindChildX<HealthBar>("HealthBar");

// 安全添加子节点
AddChildX(bullet);
```

### 信号处理

```csharp
// 流畅的信号连接
this.CreateSignalBuilder(Timer.SignalName.Timeout)
    .WithFlags(ConnectFlags.OneShot)
    .Connect(OnTimeout)
    .UnRegisterWhenNodeExitTree(this);
```

### 异步操作

```csharp
// 等待信号
await ToSignal(this, SignalName.Ready);

// 等待条件
await WaitUntil(() => IsReady);
```

## 下一步

- [深入了解架构集成](/godot/integration/architecture-integration)
- [学习节点扩展方法](/godot/node-extensions/node-extensions)
- [掌握对象池系统](/godot/pooling/node-pool)
- [查看完整 API 参考](/api-reference/godot-api)
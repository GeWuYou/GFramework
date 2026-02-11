# Core 核心框架概览

GFramework.Core 是整个框架的基础，提供了平台无关的核心架构能力。

## 核心特性

### 🏗️ 清洁架构

基于 Model-View-Controller-System-Utility 五层架构，实现清晰的职责分离和高内聚低耦合。

### 🔧 CQRS 模式

命令查询职责分离，提供类型安全的命令和查询系统，支持可撤销操作。

### 📡 事件驱动

强大的事件总线系统，支持类型安全的事件发布订阅，实现组件间松耦合通信。

### 🔄 响应式编程

可绑定属性系统，自动化的数据绑定和 UI 更新机制。

### 🎯 依赖注入

完善的 IoC 容器，支持自动依赖解析和生命周期管理。

## 核心组件

### 架构组件

- **[Architecture](/core/architecture/architecture)** - 应用架构管理器
- **AbstractModel** - 数据模型基类
- **AbstractSystem** - 业务系统基类
- **AbstractUtility** - 工具类基类

### 命令查询系统

- **[AbstractCommand](/core/command-query/commands)** - 命令基类
- **AbstractQuery** - 查询基类
- **CommandExecutor** - 命令执行器

### 事件系统

- **[EventBus](/core/events/event-bus)** - 事件总线
- **EasyEvent** - 简单事件
- **OrEvent** - 组合事件

### 属性系统

- **[BindableProperty](/core/property/bindable-property)** - 可绑定属性
- **BindablePropertyGeneric** - 泛型可绑定属性

### 工具类

- **[IoC 容器](/core/utilities/ioc-container)** - 依赖注入容器
- **Logger** - 日志系统
- **ObjectPool** - 对象池

## 使用场景

Core 模块适用于任何 .NET 应用场景：

- **游戏开发** - 作为游戏架构基础
- **桌面应用** - WPF、WinForms 应用
- **移动应用** - MAUI、Xamarin 应用
- **Web 应用** - ASP.NET Core 应用
- **控制台应用** - 命令行工具

## 安装

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

## 快速示例

```csharp
using GFramework.Core.architecture;

// 定义架构
public class MyAppArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new UserModel());
        RegisterSystem(new UserService());
        RegisterUtility(new StorageUtility());
    }
}

// 定义模型
public class UserModel : AbstractModel
{
    public BindableProperty<string> Name { get; } = new("User");
    public BindableProperty<int> Score { get; } = new(0);
}

// 使用架构
var architecture = new MyAppArchitecture();
architecture.Initialize();

var userModel = architecture.GetModel<UserModel>();
userModel.Name.Value = "New Name"; // 自动触发更新
```

## 学习路径

建议按以下顺序学习 Core 模块：

1. **[架构基础](/core/architecture/architecture)** - 了解架构组件和生命周期
2. **[命令查询](/core/command-query/commands)** - 掌握 CQRS 模式
3. **[事件系统](/core/events/event-bus)** - 学习事件驱动编程
4. **[属性系统](/core/property/bindable-property)** - 理解响应式编程
5. **[工具类](/core/utilities/ioc-container)** - 使用辅助工具

## 与其他模块的关系

```
GFramework.Core (基础层)
    ↓ 依赖
GFramework.Game (游戏层)
    ↓ 依赖  
GFramework.Godot (引擎层)
```

Core 模块是其他所有模块的基础，提供核心的架构能力和设计模式实现。

## 性能特点

- **零额外开销** - 编译时优化，运行时无性能损失
- **内存友好** - 自动内存管理和对象回收
- **类型安全** - 编译时类型检查，避免运行时错误
- **可扩展** - 支持自定义扩展和插件

## 下一步

- [深入了解架构组件](/core/architecture/architecture)
- [查看完整 API 参考](/api-reference/core-api)
- [学习最佳实践](/tutorials/best-practices)
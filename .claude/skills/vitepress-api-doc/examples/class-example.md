---
title: Architecture
description: 架构基类，提供系统、模型、工具等组件的注册与管理功能。专注于生命周期管理、初始化流程控制和架构阶段转换。
outline: deep
---

# Architecture

## 概述

架构基类，提供系统、模型、工具等组件的注册与管理功能。专注于生命周期管理、初始化流程控制和架构阶段转换。

**命名空间**：`GFramework.Core.architecture`
**程序集**：`GFramework.Core`
**继承**：`Object` → `Architecture`
**实现**：`IArchitecture`

## 构造函数

### Architecture

创建架构实例。

**签名**：
```csharp
public Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
)
```

**参数**：
- `configuration` (IArchitectureConfiguration?): 架构配置对象，为 null 时使用默认配置
- `environment` (IEnvironment?): 环境配置对象，为 null 时使用默认环境
- `services` (IArchitectureServices?): 架构服务对象，为 null 时创建新实例
- `context` (IArchitectureContext?): 架构上下文对象，为 null 时创建新实例

## 公共方法

### Initialize

同步初始化架构，阻塞当前线程直到初始化完成。

**签名**：
```csharp
public void Initialize()
```

**特点**：
- 阻塞式初始化
- 适用于简单场景或控制台应用
- 初始化失败时抛出异常并进入 `FailedInitialization` 阶段

### InitializeAsync

异步初始化架构，返回 Task 以便调用者可以等待初始化完成。

**签名**：
```csharp
public async Task InitializeAsync()
```

**返回值**：
- (Task): 表示异步初始化操作的任务

**特点**：
- 非阻塞式初始化
- 支持异步组件初始化
- 适用于需要异步加载资源的场景

### InstallModule

安装架构模块，用于扩展架构功能。

**签名**：
```csharp
public IArchitectureModule InstallModule(IArchitectureModule module)
```

**参数**：
- `module` (IArchitectureModule): 要安装的模块实例

**返回值**：
- (IArchitectureModule): 返回安装的模块实例

### RegisterSystem

注册系统组件到架构中。

**签名**：
```csharp
public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
```

**类型参数**：
- `TSystem`: 系统类型，必须实现 ISystem 接口

**参数**：
- `system` (TSystem): 要注册的系统实例

### RegisterModel

注册模型组件到架构中。

**签名**：
```csharp
public void RegisterModel<TModel>(TModel model) where TModel : IModel
```

**类型参数**：
- `TModel`: 模型类型，必须实现 IModel 接口

**参数**：
- `model` (TModel): 要注册的模型实例

### RegisterUtility

注册工具组件到架构中。

**签名**：
```csharp
public void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
```

**类型参数**：
- `TUtility`: 工具类型，必须实现 IUtility 接口

**参数**：
- `utility` (TUtility): 要注册的工具实例

### SendCommand

发送并执行命令。

**签名**：
```csharp
public void SendCommand(ICommand command)
```

**参数**：
- `command` (ICommand): 要执行的命令实例

### SendCommand&lt;TResult&gt;

发送并执行带返回值的命令。

**签名**：
```csharp
public TResult SendCommand<TResult>(ICommand<TResult> command)
```

**类型参数**：
- `TResult`: 命令返回值类型

**参数**：
- `command` (ICommand&lt;TResult&gt;): 要执行的命令实例

**返回值**：
- (TResult): 命令执行结果

## 公共属性

### CurrentPhase

获取当前架构的阶段。

**类型**：`ArchitecturePhase`
**访问**：get

### Context

获取架构上下文，提供对架构服务的访问。

**类型**：`IArchitectureContext`
**访问**：get

### IsReady

获取一个布尔值，指示当前架构是否处于就绪状态。

**类型**：`bool`
**访问**：get

## 使用示例

### 基本用法

```csharp
// 1. 定义你的架构（继承 Architecture 基类）
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Model
        RegisterModel(new PlayerModel());
        RegisterModel(new InventoryModel());

        // 注册 System
        RegisterSystem(new GameplaySystem());
        RegisterSystem(new SaveSystem());

        // 注册 Utility
        RegisterUtility(new StorageUtility());
        RegisterUtility(new TimeUtility());
    }
}

// 2. 创建并初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();

// 3. 等待架构就绪
await architecture.WaitUntilReadyAsync();
```

### 异步初始化

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync(); // 异步等待初始化完成

// 检查架构是否已就绪
if (architecture.IsReady)
{
    Console.WriteLine("架构已就绪，可以开始游戏");
}
```

### 使用自定义配置

```csharp
var config = new ArchitectureConfiguration
{
    ArchitectureProperties = new ArchitectureProperties
    {
        StrictPhaseValidation = true,      // 启用严格阶段验证
        AllowLateRegistration = false      // 禁止就绪后注册组件
    }
};

var architecture = new GameArchitecture(configuration: config);
architecture.Initialize();
```

## 另请参阅

- [IArchitecture](./iarchitecture.md) - 架构接口
- [ArchitecturePhase](./architecture-phase.md) - 架构阶段枚举
- [IArchitectureModule](./iarchitecture-module.md) - 架构模块接口
- [架构组件](/zh-CN/core/architecture) - 架构使用指南

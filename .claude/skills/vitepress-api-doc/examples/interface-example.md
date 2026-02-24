---
title: IArchitecture
description: 架构接口，定义了框架的核心功能契约。
outline: deep
---

# IArchitecture

## 概述

架构接口，定义了框架的核心功能契约。

**命名空间**：`GFramework.Core.Abstractions.architecture`
**程序集**：`GFramework.Core.Abstractions`
**实现类**：[Architecture](./architecture.md)

## 公共方法

### RegisterSystem&lt;TSystem&gt;

注册系统组件到架构中。

**签名**：
```csharp
void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
```

**类型参数**：
- `TSystem`: 系统类型，必须实现 ISystem 接口

**参数**：
- `system` (TSystem): 要注册的系统实例

### RegisterModel&lt;TModel&gt;

注册模型组件到架构中。

**签名**：
```csharp
void RegisterModel<TModel>(TModel model) where TModel : IModel
```

**类型参数**：
- `TModel`: 模型类型，必须实现 IModel 接口

**参数**：
- `model` (TModel): 要注册的模型实例

### RegisterUtility&lt;TUtility&gt;

注册工具组件到架构中。

**签名**：
```csharp
void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
```

**类型参数**：
- `TUtility`: 工具类型，必须实现 IUtility 接口

**参数**：
- `utility` (TUtility): 要注册的工具实例

### GetModel&lt;T&gt;

从容器中获取已注册的模型。

**签名**：
```csharp
T GetModel<T>() where T : class, IModel
```

**类型参数**：
- `T`: 模型类型

**返回值**：
- (T): 模型实例

### GetSystem&lt;T&gt;

从容器中获取已注册的系统。

**签名**：
```csharp
T GetSystem<T>() where T : class, ISystem
```

**类型参数**：
- `T`: 系统类型

**返回值**：
- (T): 系统实例

### GetUtility&lt;T&gt;

从容器中获取已注册的工具。

**签名**：
```csharp
T GetUtility<T>() where T : class, IUtility
```

**类型参数**：
- `T`: 工具类型

**返回值**：
- (T): 工具实例

### SendCommand

发送并执行命令。

**签名**：
```csharp
void SendCommand(ICommand command)
```

**参数**：
- `command` (ICommand): 要执行的命令实例

### SendCommand&lt;TResult&gt;

发送并执行带返回值的命令。

**签名**：
```csharp
TResult SendCommand<TResult>(ICommand<TResult> command)
```

**类型参数**：
- `TResult`: 命令返回值类型

**参数**：
- `command` (ICommand&lt;TResult&gt;): 要执行的命令实例

**返回值**：
- (TResult): 命令执行结果

### SendQuery&lt;TResult&gt;

发送并执行查询。

**签名**：
```csharp
TResult SendQuery<TResult>(IQuery<TResult> query)
```

**类型参数**：
- `TResult`: 查询返回值类型

**参数**：
- `query` (IQuery&lt;TResult&gt;): 要执行的查询实例

**返回值**：
- (TResult): 查询结果

### SendEvent&lt;T&gt;

发送事件（无参数）。

**签名**：
```csharp
void SendEvent<T>() where T : new()
```

**类型参数**：
- `T`: 事件类型，必须有无参构造函数

### SendEvent&lt;T&gt;

发送事件（带参数）。

**签名**：
```csharp
void SendEvent<T>(T e)
```

**类型参数**：
- `T`: 事件类型

**参数**：
- `e` (T): 事件实例

### RegisterEvent&lt;T&gt;

注册事件监听器。

**签名**：
```csharp
IUnRegister RegisterEvent<T>(Action<T> onEvent)
```

**类型参数**：
- `T`: 事件类型

**参数**：
- `onEvent` (Action&lt;T&gt;): 事件处理回调

**返回值**：
- (IUnRegister): 用于注销事件的对象

### UnRegisterEvent&lt;T&gt;

注销事件监听器。

**签名**：
```csharp
void UnRegisterEvent<T>(Action<T> onEvent)
```

**类型参数**：
- `T`: 事件类型

**参数**：
- `onEvent` (Action&lt;T&gt;): 要注销的事件处理回调

## 公共属性

### CurrentPhase

获取当前架构的阶段。

**类型**：`ArchitecturePhase`
**访问**：get

### Context

获取架构上下文。

**类型**：`IArchitectureContext`
**访问**：get

## 使用示例

### 在 Controller 中使用

```csharp
public class GameController : IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Start()
    {
        // 获取 Model
        var playerModel = this.GetModel<PlayerModel>();

        // 发送命令
        this.SendCommand(new StartGameCommand());

        // 发送查询
        var score = this.SendQuery(new GetScoreQuery());

        // 注册事件
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // 处理玩家死亡事件
    }
}
```

### 实现自定义架构

```csharp
public class GameArchitecture : Architecture
{
    // 单例访问
    public static IArchitecture Interface { get; private set; }

    protected override void Init()
    {
        Interface = this;

        // 注册组件
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
        RegisterUtility(new StorageUtility());
    }
}
```

## 另请参阅

- [Architecture](./architecture.md) - 架构基类实现
- [IModel](./imodel.md) - 模型接口
- [ISystem](./isystem.md) - 系统接口
- [IUtility](./iutility.md) - 工具接口
- [架构组件](/zh-CN/core/architecture) - 架构使用指南

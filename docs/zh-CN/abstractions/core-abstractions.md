# Core Abstractions

> GFramework.Core.Abstractions 核心抽象接口定义

## 概述

GFramework.Core.Abstractions 包含了框架的所有核心接口定义，这些接口定义了组件之间的契约，实现了依赖倒置和面向接口编程。

## 核心接口

### IArchitecture

应用程序架构接口：

```csharp
public interface IArchitecture
{
    void Initialize();
    void Destroy();
    
    T GetModel<T>() where T : IModel;
    T GetSystem<T>() where T : ISystem;
    T GetUtility<T>() where T : IUtility;
    
    void RegisterModel(IModel model);
    void RegisterSystem(ISystem system);
    void RegisterUtility(IUtility utility);
}
```

### IModel

数据模型接口：

```csharp
public interface IModel
{
    void Init();
    void Dispose();
    
    IArchitecture Architecture { get; }
}
```

### ISystem

业务逻辑系统接口：

```csharp
public interface ISystem
{
    void Init();
    void Dispose();
    
    IArchitecture Architecture { get; }
}
```

### IController

控制器接口：

```csharp
public interface IController : IBelongToArchitecture
{
    void Init();
    void Dispose();
}
```

### IUtility

工具类接口：

```csharp
public interface IUtility
{
}
```

## 事件接口

### IEvent

事件基接口：

```csharp
public interface IEvent
{
}
```

### IEventHandler

事件处理器接口：

```csharp
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event);
}
```

## 命令查询接口

### ICommand

命令接口：

```csharp
public interface ICommand
{
    void Execute();
}
```

### IQuery

查询接口：

```csharp
public interface IQuery<TResult>
{
    TResult Execute();
}
```

## 依赖注入接口

### IIocContainer

IOC 容器接口：

```csharp
public interface IIocContainer
{
    void Register<TInterface, TImplementation>() where TImplementation : TInterface;
    void Register<TInterface>(TInterface instance);
    TInterface Resolve<TInterface>();
    bool IsRegistered<TInterface>();
}
```

## 生命周期接口

### ILifecycle

组件生命周期接口：

```csharp
public interface ILifecycle
{
    void OnInit();
    void OnDestroy();
}
```

## 使用示例

### 通过接口实现依赖注入

```csharp
public class MyService : IMyService
{
    private readonly IArchitecture _architecture;
    
    public MyService(IArchitecture architecture)
    {
        _architecture = architecture;
    }
}
```

### 自定义事件

```csharp
public class PlayerDiedEvent : IEvent
{
    public int PlayerId { get; set; }
    public Vector2 Position { get; set; }
}
```

---

**相关文档**：

- [Core 概述](../core/index.md)
- [Architecture](../core/architecture)
- [Events](../core/events)
- [Command](../core/command)
- [Query](../core/query)

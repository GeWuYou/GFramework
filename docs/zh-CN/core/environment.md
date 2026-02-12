# Environment 包使用说明

## 概述

Environment 包定义了环境配置功能。Environment 提供了一个键值对存储系统，用于在运行时存储和获取各种环境特定的值，如配置选项、路径设置等。它允许应用程序在不同环境下灵活调整行为。

## 核心接口

### IEnvironment

环境接口，定义了环境值的存储和获取功能。

**核心成员：**

```csharp
string Name { get; }  // 环境名称
T? Get<T>(string key) where T : class;  // 根据键获取值
bool TryGet<T>(string key, out T value) where T : class;  // 尝试获取值
T GetRequired<T>(string key) where T : class;  // 获取必需值（不存在则抛异常）
void Register(string key, object value);  // 注册键值对
void Initialize();  // 初始化环境
```

## 核心类

### [`EnvironmentBase`](./environment.md)

环境基础抽象类，实现了 IEnvironment 接口，提供环境值的存储和获取功能。

**使用方式：**

```csharp
public abstract class EnvironmentBase : ContextAwareBase, IEnvironment
{
    protected readonly Dictionary<string, object> Values = new();  // 存储环境值
    
    public abstract string Name { get; }  // 环境名称
    
    public virtual T? Get<T>(string key) where T : class
    {
        return TryGet<T>(key, out var value) ? value : null;
    }
    
    public virtual bool TryGet<T>(string key, out T value) where T : class
    {
        if (Values.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }
    
    public virtual T GetRequired<T>(string key) where T : class
    {
        if (TryGet<T>(key, out var value))
            return value;

        throw new InvalidOperationException(
            $"Environment [{Name}] missing required value: key='{key}', type='{typeof(T).Name}'");
    }

    void IEnvironment.Register(string key, object value);
    protected void Register(string key, object value);  // 注册键值对
    public abstract void Initialize();  // 初始化环境
}
```

### [`DefaultEnvironment`](./environment.md)

默认环境实现类，继承自 EnvironmentBase。

**使用方式：**

```csharp
public class DefaultEnvironment : EnvironmentBase
{
    public override string Name { get; } = "Default";  // 环境名称
    
    public override void Initialize()
    {
        // 默认环境初始化逻辑
    }
}
```

## 基本使用

### 1. 定义自定义环境

```csharp
public class GameEnvironment : EnvironmentBase
{
    public override string Name { get; } = "Game";

    public override void Initialize()
    {
        // 注册一些环境特定的值
        Register("GameMode", "Survival");
        Register("MaxPlayers", 4);
        Register("ServerAddress", "localhost");
    }

    // 便捷方法
    public string GameMode => Get<string>("GameMode") ?? "Default";
    public int MaxPlayers => Get<int>("MaxPlayers") ?? 1;
    public string ServerAddress => Get<string>("ServerAddress") ?? "localhost";
}
```

### 2. 在架构中使用环境

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 环境已经在架构初始化过程中自动初始化
        // 但是我们可以在需要的时候获取环境值
        var gameMode = this.Context.Environment.Get<string>("GameMode");

        // 注册模型和系统
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
    }
}
```

### 3. 使用环境值

```csharp
public class NetworkSystem : AbstractSystem
{
    private string _serverAddress;

    protected override void OnInit()
    {
        // 从环境中获取服务器地址
        _serverAddress = this.GetContext().Environment.Get<string>("ServerAddress") ?? "localhost";

        // 注册事件
        this.RegisterEvent<ConnectToServerEvent>(OnConnectToServer);
    }

    private void OnConnectToServer(ConnectToServerEvent e)
    {
        // 使用环境中的服务器地址
        Connect(_serverAddress, e.Port);
    }

    private void Connect(string address, int port)
    {
        // 连接逻辑
    }
}
```

## Environment vs Configuration

### Environment（环境）

- **动态性**：运行时可以更改
- **灵活性**：根据部署环境调整行为
- **存储类型**：运行时值、连接信息等
- **访问方式**：通过键值对访问

### Configuration（配置）

- **静态性**：通常在启动时确定
- **持久性**：保存在文件或外部源
- **存储类型**：应用设置、参数等
- **访问方式**：通过配置对象访问

## 最佳实践

1. **明确环境名称**：为每个环境提供有意义的名称
2. **类型安全**：使用泛型方法确保类型安全
3. **错误处理**：使用 GetRequired 方法获取必需值
4. **初始化时机**：在架构初始化期间完成环境设置
5. **避免过度使用**：仅存储环境相关的值

## 错误示例

```csharp
// ❌ 错误：获取必需值但不存在
public class BadExampleSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 如果"RequiredValue"不存在，这会抛出异常
        var value = this.GetContext().Environment.GetRequired<string>("RequiredValue");
    }
}

// ✅ 正确：安全获取值
public class GoodExampleSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 使用 TryGet 安全获取值
        if (this.GetContext().Environment.TryGet<string>("OptionalValue", out var value))
        {
            // 处理值
        }

        // 或者提供默认值
        var gameMode = this.GetContext().Environment.Get<string>("GameMode") ?? "Default";
    }
}
```

## 相关包

- [`architecture`](./architecture.md) - 在架构中使用环境配置
- [`rule`](./rule.md) - 环境基类继承自 ContextAwareBase
- [`ioc`](./ioc.md) - 环境值可通过IoC容器管理
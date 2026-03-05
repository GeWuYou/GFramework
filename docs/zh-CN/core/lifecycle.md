---
title: 生命周期管理
description: 生命周期管理提供了标准化的组件初始化和销毁机制，确保资源的正确管理和释放。
---

# 生命周期管理

## 概述

生命周期管理是 GFramework 中用于管理组件初始化和销毁的核心机制。通过实现标准的生命周期接口，组件可以在适当的时机执行初始化逻辑和资源清理，确保系统的稳定性和资源的有效管理。

GFramework 提供了同步和异步两套生命周期接口，适用于不同的使用场景。架构会自动管理所有注册组件的生命周期，开发者只需实现相应的接口即可。

**主要特性**：

- 标准化的初始化和销毁流程
- 支持同步和异步操作
- 自动生命周期管理
- 按注册顺序初始化，按逆序销毁
- 与架构系统深度集成

## 核心概念

### 生命周期接口层次

GFramework 提供了一套完整的生命周期接口：

```csharp
// 同步接口
public interface IInitializable
{
    void Initialize();
}

public interface IDestroyable
{
    void Destroy();
}

public interface ILifecycle : IInitializable, IDestroyable
{
}

// 异步接口
public interface IAsyncInitializable
{
    Task InitializeAsync();
}

public interface IAsyncDestroyable
{
    ValueTask DestroyAsync();
}

public interface IAsyncLifecycle : IAsyncInitializable, IAsyncDestroyable
{
}
```

### 初始化阶段

组件在注册到架构后会自动进行初始化：

```csharp
public class PlayerModel : AbstractModel
{
    protected override void OnInit()
    {
        // 初始化逻辑
        Console.WriteLine("PlayerModel 初始化");
    }
}
```

### 销毁阶段

当架构销毁时，所有实现了 `IDestroyable` 的组件会按注册的逆序被销毁：

```csharp
public class GameSystem : AbstractSystem
{
    public void Destroy()
    {
        // 清理资源
        Console.WriteLine("GameSystem 销毁");
    }
}
```

## 基本用法

### 实现同步生命周期

最常见的方式是继承框架提供的抽象基类：

```csharp
using GFramework.Core.model;

public class InventoryModel : AbstractModel
{
    private List<Item> _items = new();

    protected override void OnInit()
    {
        // 初始化库存
        _items = new List<Item>();
        Console.WriteLine("库存系统已初始化");
    }
}
```

### 实现销毁逻辑

对于需要清理资源的组件，实现 `IDestroyable` 接口：

```csharp
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.lifecycle;

public class AudioSystem : ISystem, IDestroyable
{
    private AudioEngine _engine;

    public void Initialize()
    {
        _engine = new AudioEngine();
        _engine.Start();
    }

    public void Destroy()
    {
        // 清理音频资源
        _engine?.Stop();
        _engine?.Dispose();
        _engine = null;
    }
}
```

### 在架构中注册

组件注册后，架构会自动管理其生命周期：

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册顺序：Model -> System -> Utility
        RegisterModel(new PlayerModel());      // 1. 初始化
        RegisterModel(new InventoryModel());   // 2. 初始化
        RegisterSystem(new AudioSystem());     // 3. 初始化

        // 销毁顺序会自动反转：
        // AudioSystem -> InventoryModel -> PlayerModel
    }
}
```

## 高级用法

### 异步初始化

对于需要异步操作的组件（如加载配置、连接数据库），使用异步生命周期：

```csharp
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.system;

public class ConfigurationSystem : ISystem, IAsyncInitializable
{
    private Configuration _config;

    public async Task InitializeAsync()
    {
        // 异步加载配置文件
        _config = await LoadConfigurationAsync();
        Console.WriteLine("配置已加载");
    }

    private async Task<Configuration> LoadConfigurationAsync()
    {
        await Task.Delay(100); // 模拟异步操作
        return new Configuration();
    }
}
```

### 异步销毁

对于需要异步清理的资源（如关闭网络连接、保存数据）：

```csharp
using GFramework.Core.Abstractions.lifecycle;

public class NetworkSystem : ISystem, IAsyncDestroyable
{
    private NetworkClient _client;

    public void Initialize()
    {
        _client = new NetworkClient();
    }

    public async ValueTask DestroyAsync()
    {
        // 异步关闭连接
        if (_client != null)
        {
            await _client.DisconnectAsync();
            await _client.DisposeAsync();
        }
        Console.WriteLine("网络连接已关闭");
    }
}
```

### 完整异步生命周期

同时实现异步初始化和销毁：

```csharp
public class DatabaseSystem : ISystem, IAsyncLifecycle
{
    private DatabaseConnection _connection;

    public async Task InitializeAsync()
    {
        // 异步连接数据库
        _connection = new DatabaseConnection();
        await _connection.ConnectAsync("connection-string");
        Console.WriteLine("数据库已连接");
    }

    public async ValueTask DestroyAsync()
    {
        // 异步关闭数据库连接
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        Console.WriteLine("数据库连接已关闭");
    }
}
```

### 生命周期钩子

监听架构的生命周期阶段：

```csharp
using GFramework.Core.Abstractions.enums;

public class AnalyticsSystem : AbstractSystem
{
    protected override void OnInit()
    {
        Console.WriteLine("分析系统初始化");
    }

    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.Initializing:
                Console.WriteLine("架构正在初始化");
                break;
            case ArchitecturePhase.Ready:
                Console.WriteLine("架构已就绪");
                StartTracking();
                break;
            case ArchitecturePhase.Destroying:
                Console.WriteLine("架构正在销毁");
                StopTracking();
                break;
        }
    }

    private void StartTracking() { }
    private void StopTracking() { }
}
```

## 最佳实践

1. **优先使用抽象基类**：继承 `AbstractModel`、`AbstractSystem` 等基类，它们已经实现了生命周期接口
   ```csharp
   ✓ public class MyModel : AbstractModel { }
   ✗ public class MyModel : IModel, IInitializable { }
   ```

2. **初始化顺序很重要**：按依赖关系注册组件，被依赖的组件先注册
   ```csharp
   protected override void Init()
   {
       RegisterModel(new ConfigModel());      // 先注册配置
       RegisterModel(new PlayerModel());      // 再注册依赖配置的模型
       RegisterSystem(new GameplaySystem());  // 最后注册系统
   }
   ```

3. **销毁时释放资源**：实现 `Destroy()` 方法清理非托管资源
   ```csharp
   public void Destroy()
   {
       // 释放事件订阅
       _eventBus.Unsubscribe<GameEvent>(OnGameEvent);

       // 释放非托管资源
       _nativeHandle?.Dispose();

       // 清空引用
       _cache?.Clear();
   }
   ```

4. **异步操作使用异步接口**：避免在同步方法中阻塞异步操作
   ```csharp
   ✓ public async Task InitializeAsync() { await LoadDataAsync(); }
   ✗ public void Initialize() { LoadDataAsync().Wait(); } // 可能死锁
   ```

5. **避免在初始化中访问其他组件**：初始化顺序可能导致组件尚未就绪
   ```csharp
   ✗ protected override void OnInit()
   {
       var system = this.GetSystem<OtherSystem>(); // 可能尚未初始化
   }

   ✓ public override void OnArchitecturePhase(ArchitecturePhase phase)
   {
       if (phase == ArchitecturePhase.Ready)
       {
           var system = this.GetSystem<OtherSystem>(); // 安全
       }
   }
   ```

6. **使用 OnArchitecturePhase 处理跨组件依赖**：在 Ready 阶段访问其他组件
   ```csharp
   public override void OnArchitecturePhase(ArchitecturePhase phase)
   {
       if (phase == ArchitecturePhase.Ready)
       {
           // 此时所有组件都已初始化完成
           var config = this.GetModel<ConfigModel>();
           ApplyConfiguration(config);
       }
   }
   ```

## 常见问题

### 问题：什么时候使用同步 vs 异步生命周期？

**解答**：

- **同步**：简单的初始化逻辑，如创建对象、设置默认值
- **异步**：需要 I/O 操作的场景，如加载文件、网络请求、数据库连接

```csharp
// 同步：简单初始化
public class ScoreModel : AbstractModel
{
    protected override void OnInit()
    {
        Score = 0; // 简单赋值
    }
}

// 异步：需要 I/O
public class SaveSystem : ISystem, IAsyncInitializable
{
    public async Task InitializeAsync()
    {
        await LoadSaveDataAsync(); // 文件 I/O
    }
}
```

### 问题：组件的初始化和销毁顺序是什么？

**解答**：

- **初始化顺序**：按注册顺序（先注册先初始化）
- **销毁顺序**：按注册的逆序（后注册先销毁）

```csharp
protected override void Init()
{
    RegisterModel(new A());    // 1. 初始化，3. 销毁
    RegisterModel(new B());    // 2. 初始化，2. 销毁
    RegisterSystem(new C());   // 3. 初始化，1. 销毁
}
```

### 问题：如何在初始化时访问其他组件？

**解答**：
不要在 `OnInit()` 中访问其他组件，使用 `OnArchitecturePhase()` 在 Ready 阶段访问：

```csharp
public class DependentSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // ✗ 不要在这里访问其他组件
    }

    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            // ✓ 在这里安全访问其他组件
            var config = this.GetModel<ConfigModel>();
        }
    }
}
```

### 问题：Destroy() 方法一定会被调用吗？

**解答**：
只有在正常销毁架构时才会调用。如果应用程序崩溃或被强制终止，`Destroy()` 可能不会被调用。因此：

- 不要依赖 `Destroy()` 保存关键数据
- 使用自动保存机制保护重要数据
- 非托管资源应该实现 `IDisposable` 模式

### 问题：可以在 Destroy() 中访问其他组件吗？

**解答**：
不推荐。销毁时其他组件可能已经被销毁。如果必须访问，确保检查组件是否仍然可用：

```csharp
public void Destroy()
{
    // ✗ 不安全
    var system = this.GetSystem<OtherSystem>();
    system.DoSomething();

    // ✓ 安全
    try
    {
        var system = this.GetSystem<OtherSystem>();
        system?.DoSomething();
    }
    catch
    {
        // 组件可能已销毁
    }
}
```

## 相关文档

- [架构组件](/zh-CN/core/architecture) - 架构基础和组件注册
- [Model 层](/zh-CN/core/model) - 数据模型的生命周期
- [System 层](/zh-CN/core/system) - 业务系统的生命周期
- [异步初始化](/zh-CN/core/async-initialization) - 异步架构初始化详解

---
title: Godot 架构集成
description: Godot 架构集成提供了 GFramework 与 Godot 引擎的无缝连接，实现生命周期同步和模块化开发。
---

# Godot 架构集成

## 概述

Godot 架构集成是 GFramework.Godot 中连接框架与 Godot 引擎的核心组件。它提供了架构与 Godot 场景树的生命周期绑定、模块化扩展系统，以及与
Godot 节点系统的深度集成。

通过 Godot 架构集成，你可以在 Godot 项目中使用 GFramework 的所有功能，同时保持与 Godot 引擎的完美兼容。

**主要特性**：

- 架构与 Godot 生命周期自动同步
- 模块化的 Godot 扩展系统
- 架构锚点节点管理
- 自动资源清理
- 热重载支持
- 与 Godot 场景树深度集成

## 核心概念

### 抽象架构

`AbstractArchitecture` 是 Godot 项目中架构的基类：

```csharp
public abstract class AbstractArchitecture : Architecture
{
    protected Node ArchitectureRoot { get; }
    protected abstract void InstallModules();
    protected Task InstallGodotModule<TModule>(TModule module);
}
```

### 架构锚点

`ArchitectureAnchor` 是连接架构与 Godot 场景树的桥梁：

```csharp
public partial class ArchitectureAnchor : Node
{
    public void Bind(Action onExit);
    public override void _ExitTree();
}
```

### Godot 模块

`IGodotModule` 定义了 Godot 特定的模块接口：

```csharp
public interface IGodotModule : IArchitectureModule
{
    Node Node { get; }
    void OnPhase(ArchitecturePhase phase, IArchitecture architecture);
    void OnAttach(Architecture architecture);
    void OnDetach();
}
```

## 基本用法

### 创建 Godot 架构

```csharp
using GFramework.Godot.architecture;
using GFramework.Core.Abstractions.architecture;

public class GameArchitecture : AbstractArchitecture
{
    // 单例实例
    public static GameArchitecture Interface { get; private set; }

    public GameArchitecture()
    {
        Interface = this;
    }

    protected override void InstallModules()
    {
        // 注册 Model
        RegisterModel(new PlayerModel());
        RegisterModel(new GameModel());

        // 注册 System
        RegisterSystem(new GameplaySystem());
        RegisterSystem(new AudioSystem());

        // 注册 Utility
        RegisterUtility(new StorageUtility());
    }
}
```

### 在 Godot 场景中初始化架构

```csharp
using Godot;
using GFramework.Godot.architecture;

public partial class GameRoot : Node
{
    private GameArchitecture _architecture;

    public override void _Ready()
    {
        // 创建并初始化架构
        _architecture = new GameArchitecture();
        _architecture.InitializeAsync().AsTask().Wait();

        GD.Print("架构已初始化");
    }
}
```

### 使用架构锚点

架构锚点会自动创建并绑定到场景树：

```csharp
// 架构会自动创建锚点节点
// 节点名称格式: __GFramework__GameArchitecture__[HashCode]__ArchitectureAnchor__

// 当场景树销毁时，锚点会自动触发架构清理
```

## 高级用法

### 创建 Godot 模块

```csharp
using GFramework.Godot.architecture;
using Godot;

public class CoroutineModule : AbstractGodotModule
{
    private Node _coroutineNode;

    public override Node Node => _coroutineNode;

    public CoroutineModule()
    {
        _coroutineNode = new Node { Name = "CoroutineScheduler" };
    }

    public override void Install(IArchitecture architecture)
    {
        // 注册协程调度器
        var scheduler = new CoroutineScheduler(new GodotTimeSource());
        architecture.RegisterSystem<ICoroutineScheduler>(scheduler);

        GD.Print("协程模块已安装");
    }

    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            GD.Print("协程模块已就绪");
        }
    }

    public override void OnDetach()
    {
        GD.Print("协程模块已分离");
        _coroutineNode?.QueueFree();
    }
}
```

### 安装 Godot 模块

```csharp
public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 安装核心模块
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());

        // 安装 Godot 模块
        InstallGodotModule(new CoroutineModule()).Wait();
        InstallGodotModule(new SceneModule()).Wait();
        InstallGodotModule(new UiModule()).Wait();
    }
}
```

### 访问架构根节点

```csharp
public class SceneModule : AbstractGodotModule
{
    private Node _sceneRoot;

    public override Node Node => _sceneRoot;

    public SceneModule()
    {
        _sceneRoot = new Node { Name = "SceneRoot" };
    }

    public override void Install(IArchitecture architecture)
    {
        // 访问架构根节点
        if (architecture is AbstractArchitecture godotArch)
        {
            var root = godotArch.ArchitectureRoot;
            root.AddChild(_sceneRoot);
        }
    }
}
```

### 监听架构阶段

```csharp
public class AnalyticsModule : AbstractGodotModule
{
    private Node _analyticsNode;

    public override Node Node => _analyticsNode;

    public AnalyticsModule()
    {
        _analyticsNode = new Node { Name = "Analytics" };
  }

    public override void Install(IArchitecture architecture)
    {
        // 安装分析系统
    }

    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Initializing:
                GD.Print("架构正在初始化");
                break;

            case ArchitecturePhase.Ready:
                GD.Print("架构已就绪，开始追踪");
                StartTracking();
                break;

            case ArchitecturePhase.Destroying:
                GD.Prin构正在销毁，停止追踪");
                StopTracking();
                break;
        }
    }

    private void StartTracking() { }
    private void StopTracking() { }
}
```

### 自定义架构配置

```csharp
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.environment;

public class GameArchitecture : AbstractArchitecture
{
    public GameArchitecture() : base(
        configuration: CreateConfiguration(),
        environment: CreateEnvironment()
    )
    {
    }

    private static IArchitectureConfiguration CreateConfiguration()
    {
        return new ArchitectureConfiguration
        {
            EnableLogging
            LogLevel = LogLevel.Debug
        };
    }

    private static IEnvironment CreateEnvironment()
    {
        return new DefaultEnvironment
        {
            IsDevelopment = OS.IsDebugBuild()
        };
    }

    protected override void InstallModules()
    {
        // 根据环境配置安装模块
        if (Environment.IsDevelopment)
        {
            InstallGodotModule(new DebugModule()).Wait();
        }

        // 安装核心模块
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
    }
}
```

### 热重载支持

```csharp
public class GameArchitecture : AbstractArchitecture
{
    private static bool _initialized;

    protected override void OnInitialize()
    {
        // 防止热重载时重复初始化
        if (_initialized)
        {
            GD.Print("架构已初始化，跳过重复初始化");
            return;
        }

        base.OnInitialize();
        _initialized = true;
    }

    protected override async ValueTask OnDestroyAsync()
    {
        await base.OnDestroyAsync();
        _initialized = false;
    }
}
```

### 在节点中使用架构

```csharp
using Godot;
using GFramework.Core.Abstractions.controller;
using GFramework.SourceGenerators.Abstractions.rule;

[ContextAware]
public partial class Player : CharacterBody2D, IController
{
    public override void _Ready()
    {
        // 使用扩展方法访问架构（[ContextAware] 实现 IContextAware 接口）
        var playerModel = this.GetModel<PlayerModel>();
        var gameplaySystem = this.GetSystem<GameplaySystem>();

        // 发送事件
        this.SendEvent(new PlayerSpawnedEvent());

        // 执行命令
        this.SendCommand(new InitPlayerCommand());
    }

    public override void _Process(double delta)
    {
        // 在 Process 中使用架构组件
        var inputSystem = this.GetSystem<InputSystem>();
        var movement = inputSystem.GetMovementInput();

        Velocity = movement * 200;
        MoveAndSlide();
    }
}
```

### 多架构支持

```csharp
// 游戏架构
public class GameArchitecture : AbstractArchitecture
{
    public static GameArchitecture Interface { get; private set; }

    public GameArchitecture()
    {
        Interface = this;
    }

    protected override void InstallModules()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
    }
}

// UI 架构
public class UiArchitecture : AbstractArchitecture
{
    public static UiArchitecture Interface { get; private set; }

    public UiArchitecture()
    {
        Interface = this;
    }

    protected override void InstallModules()
    {
        RegisterModel(new UiModel());
        RegisterSystem(new UiSystem());
    }
}

// 在不同节点中使用不同架构
[ContextAware]
public partial class GameNode : Node, IController
{
    // 配置使用 GameArchitecture 的上下文提供者
    static GameNode()
    {
        SetContextProvider(new GameContextProvider());
    }
}

[ContextAware]
public partial class UiNode : Control, IController
{
    // 配置使用 UiArchitecture 的上下文提供者
    static UiNode()
    {
        SetContextProvider(new UiContextProvider());
    }
}
```

## 最佳实践

1. **使用单例模式**：为架构提供全局访问点
   ```csharp
   public class GameArchitecture : AbstractArchitecture
   {
       public static GameArchitecture Interface { get; private set; }

       public GameArchitecture()
       {
           Interface = this;
       }
   }
   ```

2. **在根节点初始化架构**：确保架构在所有节点之前就绪
   ```csharp
   public partial class GameRoot : Node
   {
       public override void _Ready()
       {
           new GameArchitecture().InitializeAsync().AsTask().Wait();
       }
   }
   ```

3. **使用 Godot 模块组织功能**：将相关功能封装为模块
   ```csharp
   InstallGodotModule(new CoroutineModule()).Wait();
   InstallGodotModule(new SceneModule()).Wait();
   InstallGodotModule(new UiModule()).Wait();
   ```

4. **利用架构阶段钩子**：在适当的时机执行逻辑
   ```csharp
   public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
   {
       if (phase == ArchitecturePhase.Ready)
       {
           // 架构就绪后的初始化
       }
   }
   ```

5. **正确清理资源**：在 OnDetach 中释放 Godot 节点
   ```csharp
   public override void OnDetach()
   {
       _node?.QueueFree();
       _node = null;
   }
   ```

6. **避免在构造函数中访问架构**：使用 _Ready 或 OnPhase
   ```csharp
   ✗ public Player()
   {
       var model = this.GetModel<PlayerModel>(); // 架构可能未就绪
   }

   ✓ public override void _Ready()
   {
       var model = this.GetModel<PlayerModel>(); // 安全
   }
   ```

## 常见问题

### 问题：架构什么时候初始化？

**解答**：
在根节点的 `_Ready` 方法中初始化：

```csharp
public partial class GameRoot : Node
{
    public override void _Ready()
    {
        new GameArchitecture().InitializeAsync().AsTask().Wait();
    }
}
```

### 问题：如何在节点中访问架构？

**解答**：
使用 `[ContextAware]` 特性或直接使用单例：

```csharp
using GFramework.SourceGenerators.Abstractions.rule;

// 方式 1: 使用 [ContextAware] 特性（推荐）
[ContextAware]
public partial class Player : Node, IController
{
    public override void _Ready()
    {
        // 使用扩展方法访问架构（[ContextAware] 实现 IContextAware 接口）
        var model = this.GetModel<PlayerModel>();
        var system = this.GetSystem<GameplaySystem>();
    }
}

// 方式 2: 直接使用单例
public partial class Enemy : Node
{
    public override void _Ready()
    {
        var model = GameArchitecture.Interface.GetModel<EnemyModel>();
    }
}
```

**注意**：

- `IController` 是标记接口，不包含任何方法
- 架构访问能力由 `[ContextAware]` 特性提供
- `[ContextAware]` 会自动生成 `Context` 属性和实现 `IContextAware` 接口
- 扩展方法（如 `this.GetModel()`）基于 `IContextAware` 接口，而非 `IController`

### 问题：架构锚点节点是什么？

**解答**：
架构锚点是一个隐藏的节点，用于将架构绑定到 Godot 场景树。当场景树销毁时，锚点会自动触发架构清理。

### 问题：如何支持热重载？

**解答**：
使用静态标志防止重复初始化：

```csharp
private static bool _initialized;

protected override void OnInitialize()
{
    if (_initialized) return;
    base.OnInitialize();
    _initialized = true;
}
```

### 问题：可以有多个架构吗？

**解答**：
可以，但通常一个游戏只需要一个主架构。如果需要多个架构，为每个架构提供独立的单例：

```csharp
public class GameArchitecture : AbstractArchitecture
{
    public static GameArchitecture Interface { get; private set; }
}

public class UiArchitecture : AbstractArchitecture
{
    public static UiArchitecture Interface { get; private set; }
}
```

### 问题：Godot 模块和普通模块有什么区别？

**解答**：

- **普通模块**：纯 C# 逻辑，不依赖 Godot
- **Godot 模块**：包含 Godot 节点，与场景树集成

```csharp
// 普通模块
InstallModule(new CoreModule());

// Godot 模块
InstallGodotModule(new SceneModule()).Wait();
```

## 相关文档

- [架构组件](/zh-CN/core/architecture) - 核心架构系统
- [生命周期管理](/zh-CN/core/lifecycle) - 组件生命周期
- [Godot 场景系统](/zh-CN/godot/scene) - Godot 场景集成
- [Godot UI 系统](/zh-CN/godot/ui) - Godot UI 集成
- [Godot 扩展](/zh-CN/godot/extensions) - Godot 扩展方法

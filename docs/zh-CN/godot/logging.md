---
title: Godot 日志系统
description: Godot 日志系统提供了 GFramework 日志功能与 Godot 引擎控制台的完整集成。
---

# Godot 日志系统

## 概述

Godot 日志系统是 GFramework.Godot 中连接框架日志功能与 Godot 引擎控制台的核心组件。它提供了与 Godot
控制台的深度集成，支持彩色输出、多级别日志记录，以及与 GFramework 日志系统的无缝对接。

通过 Godot 日志系统，你可以在 Godot 项目中使用统一的日志接口，日志会自动输出到 Godot 编辑器控制台，并根据日志级别使用不同的颜色和输出方式。

**主要特性**：

- 与 Godot 控制台深度集成
- 支持彩色日志输出
- 多级别日志记录（Trace、Debug、Info、Warning、Error、Fatal）
- 日志缓存机制
- 时间戳和格式化支持
- 异常信息记录

## 核心概念

### GodotLogger

`GodotLogger` 是 Godot 平台的日志记录器实现，继承自 `AbstractLogger`：

```csharp
public sealed class GodotLogger : AbstractLogger
{
    public GodotLogger(string? name = null, LogLevel minLevel = LogLevel.Info);
    protected override void Write(LogLevel level, string message, Exception? exception);
}
```

### GodotLoggerFactory

`GodotLoggerFactory` 用于创建 Godot 日志记录器实例：

```csharp
public class GodotLoggerFactory : ILoggerFactory
{
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info);
}
```

### GodotLoggerFactoryProvider

`GodotLoggerFactoryProvider` 提供日志工厂实例，并支持日志缓存：

```csharp
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    public LogLevel MinLevel { get; set; }
    public ILogger CreateLogger(string name);
}
```

## 基本用法

### 配置 Godot 日志系统

在架构初始化时配置日志提供程序：

```csharp
using GFramework.Godot.architecture;
using GFramework.Godot.logging;
using GFramework.Core.logging;
using GFramework.Core.Abstractions.logging;

public class GameArchitecture : AbstractArchitecture
{
    public static GameArchitecture Interface { get; private set; }

    public GameArchitecture()
    {
        Interface = this;

        // 配置 Godot 日志系统
        LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
        {
            MinLevel = LogLevel.Debug  // 设置最小日志级别
        };
    }

    protected override void InstallModules()
    {
        var logger = LoggerFactoryResolver.Provider.CreateLogger("GameArchitecture");
        logger.Info("游戏架构初始化开始");

        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());

        logger.Info("游戏架构初始化完成");
    }
}
```

### 创建和使用日志记录器

```csharp
using Godot;
using GFramework.Core.logging;
using GFramework.Core.Abstractions.logging;

public partial class Player : CharacterBody2D
{
    private ILogger _logger;

    public override void _Ready()
    {
        // 创建日志记录器
        _logger = LoggerFactoryResolver.Provider.CreateLogger("Player");

        _logger.Info("玩家初始化");
        _logger.Debug("玩家位置: {0}", Position);
    }

    public override void _Process(double delta)
    {
        if (_logger.IsDebugEnabled())
        {
            _logger.Debug("玩家速度: {0}", Velocity);
        }
    }

    private void TakeDamage(float damage)
    {
        _logger.Warn("玩家受到伤害: {0}", damage);
    }

    private void OnError()
    {
        _logger.Error("玩家状态异常");
    }
}
```

### 记录不同级别的日志

```csharp
var logger = LoggerFactoryResolver.Provider.CreateLogger("GameSystem");

// Trace - 最详细的跟踪信息（灰色）
logger.Trace("执行函数: UpdatePlayerPosition");

// Debug - 调试信息（青色）
logger.Debug("当前帧率: {0}", Engine.GetFramesPerSecond());

// Info - 一般信息（白色）
logger.Info("游戏开始");

// Warning - 警告信息（黄色）
logger.Warn("资源加载缓慢: {0}ms", loadTime);

// Error - 错误信息（红色）
logger.Error("无法加载配置文件");

// Fatal - 致命错误（红色，使用 PushError）
logger.Fatal("游戏崩溃");
```

### 记录异常信息

```csharp
var logger = LoggerFactoryResolver.Provider.CreateLogger("SaveSystem");

try
{
    SaveGame();
}
catch (Exception ex)
{
    // 记录异常信息
    logger.Error("保存游戏失败", ex);
}
```

## 高级用法

### 在 System 中使用日志

```csharp
using GFramework.Core.system;
using GFramework.Core.logging;
using GFramework.Core.Abstractions.logging;

public class CombatSystem : AbstractSystem
{
    private ILogger _logger;

    protected override void OnInit()
    {
        _logger = LoggerFactoryResolver.Provider.CreateLogger("CombatSystem");
        _logger.Info("战斗系统初始化完成");
    }

    public void ProcessCombat(Entity attacker, Entity target, float damage)
    {
        _logger.Debug("战斗处理: {0} 攻击 {1}, 伤害: {2}",
            attacker.Name, target.Name, damage);

        if (damage > 100)
        {
            _logger.Warn("高伤害攻击: {0}", damage);
        }
    }

    protected override void OnDestroy()
    {
        _logger.Info("战斗系统已销毁");
    }
}
```

### 在 Model 中使用日志

```csharp
using GFramework.Core.model;
using GFramework.Core.logging;
using GFramework.Core.Abstractions.logging;

public class PlayerModel : AbstractModel
{
    private ILogger _logger;
    private int _health;

    protected override void OnInit()
    {
        _logger = LoggerFactoryResolver.Provider.CreateLogger("PlayerModel");
        _logger.Info("玩家模型初始化");

        _health = 100;
    }

    public void SetHealth(int value)
    {
        var oldHealth = _health;
        _health = value;

        _logger.Debug("玩家生命值变化: {0} -> {1}", oldHealth, _health);

        if (_health <= 0)
        {
            _logger.Warn("玩家生命值归零");
        }
    }
}
```

### 条件日志记录

```csharp
var logger = LoggerFactoryResolver.Provider.CreateLogger("PerformanceMonitor");

// 检查日志级别是否启用，避免不必要的字符串格式化
if (logger.IsDebugEnabled())
{
    var stats = CalculateComplexStats();  // 耗时操作
    logger.Debug("性能统计: {0}", stats);
}

// 简化写法
if (logger.IsTraceEnabled())
{
    logger.Trace("详细的执行流程信息");
}
```

### 分类日志记录

```csharp
// 为不同模块创建独立的日志记录器
var networkLogger = LoggerFactoryResolver.Provider.CreateLogger("Network");
var databaseLogger = LoggerFactoryResolver.Provider.CreateLogger("Database");
var aiLogger = LoggerFactoryResolver.Provider.CreateLogger("AI");

networkLogger.Info("连接到服务器");
databaseLogger.Debug("查询用户数据");
aiLogger.Trace("AI 决策树遍历");
```

### 自定义日志级别

```csharp
// 在开发环境使用 Debug 级别
#if DEBUG
LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
{
    MinLevel = LogLevel.Debug
};
#else
// 在生产环境使用 Info 级别
LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
{
    MinLevel = LogLevel.Info
};
#endif
```

### 在 Godot 模块中使用日志

```csharp
using GFramework.Godot.architecture;
using GFramework.Core.logging;
using GFramework.Core.Abstractions.logging;
using Godot;

public class SceneModule : AbstractGodotModule
{
    private ILogger _logger;
    private Node _sceneRoot;

    public override Node Node => _sceneRoot;

    public SceneModule()
    {
        _sceneRoot = new Node { Name = "SceneRoot" };
        _logger = LoggerFactoryResolver.Provider.CreateLogger("SceneModule");
    }

    public override void Install(IArchitecture architecture)
    {
        _logger.Info("场景模块安装开始");

        // 安装场景系统
        var sceneSystem = new SceneSystem();
        architecture.RegisterSystem<ISceneSystem>(sceneSystem);

        _logger.Info("场景模块安装完成");
    }

    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        _logger.Debug("场景模块阶段: {0}", phase);

        if (phase == ArchitecturePhase.Ready)
        {
            _logger.Info("场景模块已就绪");
        }
    }

    public override void OnDetach()
    {
        _logger.Info("场景模块已分离");
        _sceneRoot?.QueueFree();
    }
}
```

## 日志输出格式

### 输出格式说明

Godot 日志系统使用以下格式输出日志：

```
[时间戳] 日志级别 [日志器名称] 日志消息
```

**示例输出**：

```
[2025-01-09 10:30:45.123] INFO    [GameArchitecture] 游戏架构初始化开始
[2025-01-09 10:30:45.456] DEBUG   [Player] 玩家位置: (100, 200)
[2025-01-09 10:30:46.789] WARNING [CombatSystem] 高伤害攻击: 150
[2025-01-09 10:30:47.012] ERROR   [SaveSystem] 保存游戏失败
```

### 日志级别与 Godot 输出方法

| 日志级别        | Godot 方法         | 颜色 | 说明       |
|-------------|------------------|----|----------|
| **Trace**   | `GD.PrintRich`   | 灰色 | 最详细的跟踪信息 |
| **Debug**   | `GD.PrintRich`   | 青色 | 调试信息     |
| **Info**    | `GD.Print`       | 白色 | 一般信息     |
| **Warning** | `GD.PushWarning` | 黄色 | 警告信息     |
| **Error**   | `GD.PrintErr`    | 红色 | 错误信息     |
| **Fatal**   | `GD.PushError`   | 红色 | 致命错误     |

### 异常信息格式

当记录异常时，异常信息会附加到日志消息后：

```
[2025-01-09 10:30:47.012] ERROR   [SaveSystem] 保存游戏失败
System.IO.IOException: 文件访问被拒绝
   at SaveSystem.SaveGame() in SaveSystem.cs:line 42
```

## 最佳实践

1. **在架构初始化时配置日志系统**：
   ```csharp
   public GameArchitecture()
   {
       LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
       {
           MinLevel = LogLevel.Debug
       };
   }
   ```

2. **为每个类创建独立的日志记录器**：
   ```csharp
   private ILogger _logger;

   public override void _Ready()
   {
       _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);
   }
   ```

3. **使用合适的日志级别**：
    - `Trace`：详细的执行流程，仅在深度调试时使用
    - `Debug`：调试信息，开发阶段使用
    - `Info`：重要的业务流程和状态变化
    - `Warning`：潜在问题但不影响功能
    - `Error`：错误但程序可以继续运行
    - `Fatal`：严重错误，程序无法继续

4. **检查日志级别避免性能损失**：
   ```csharp
   if (_logger.IsDebugEnabled())
   {
       var expensiveData = CalculateExpensiveData();
       _logger.Debug("数据: {0}", expensiveData);
   }
   ```

5. **提供有意义的上下文信息**：
   ```csharp
   // ✗ 不好
   logger.Error("错误");

   // ✓ 好
   logger.Error("加载场景失败: SceneKey={0}, Path={1}", sceneKey, scenePath);
   ```

6. **记录异常时提供上下文**：
   ```csharp
   try
   {
       LoadScene(sceneKey);
   }
   catch (Exception ex)
   {
       logger.Error($"加载场景失败: {sceneKey}", ex);
   }
   ```

7. **使用分类日志记录器**：
   ```csharp
   var networkLogger = LoggerFactoryResolver.Provider.CreateLogger("Network");
   var aiLogger = LoggerFactoryResolver.Provider.CreateLogger("AI");
   ```

8. **在生命周期方法中记录关键事件**：
   ```csharp
   protected override void OnInit()
   {
       _logger.Info("系统初始化完成");
   }

   protected override void OnDestroy()
   {
       _logger.Info("系统已销毁");
   }
   ```

## 性能考虑

1. **日志缓存**：
    - `GodotLoggerFactoryProvider` 使用 `CachedLoggerFactory` 缓存日志记录器实例
    - 相同名称和级别的日志记录器会被复用

2. **级别检查**：
    - 日志方法会自动检查日志级别
    - 低于最小级别的日志不会被处理

3. **字符串格式化**：
    - 使用参数化日志避免不必要的字符串拼接
   ```csharp
   // ✗ 不好 - 总是执行字符串拼接
   logger.Debug("位置: " + position.ToString());

   // ✓ 好 - 只在 Debug 启用时格式化
   logger.Debug("位置: {0}", position);
   ```

4. **条件日志**：
    - 对于耗时的数据计算，先检查日志级别
   ```csharp
   if (logger.IsDebugEnabled())
   {
       var stats = CalculateComplexStats();
       logger.Debug("统计: {0}", stats);
   }
   ```

## 常见问题

### 问题：如何配置 Godot 日志系统？

**解答**：
在架构构造函数中配置日志提供程序：

```csharp
public GameArchitecture()
{
    LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
    {
        MinLevel = LogLevel.Debug
    };
}
```

### 问题：日志没有输出到 Godot 控制台？

**解答**：
检查以下几点：

1. 确认已配置 `GodotLoggerFactoryProvider`
2. 检查日志级别是否低于最小级别
3. 确认使用了正确的日志记录器

```csharp
// 确认配置
LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
{
    MinLevel = LogLevel.Trace  // 设置为最低级别测试
};

// 创建日志记录器
var logger = LoggerFactoryResolver.Provider.CreateLogger("Test");
logger.Info("测试日志");  // 应该能看到输出
```

### 问题：如何在不同环境使用不同的日志级别？

**解答**：
使用条件编译或环境检测：

```csharp
public GameArchitecture()
{
    var minLevel = OS.IsDebugBuild() ? LogLevel.Debug : LogLevel.Info;

    LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
    {
        MinLevel = minLevel
    };
}
```

### 问题：如何禁用某个模块的日志？

**解答**：
为该模块创建一个高级别的日志记录器：

```csharp
// 只记录 Error 及以上级别
var logger = new GodotLogger("VerboseModule", LogLevel.Error);
```

### 问题：日志输出影响性能怎么办？

**解答**：

1. 提高最小日志级别
2. 使用条件日志
3. 避免在高频调用的方法中记录日志

```csharp
// 提高日志级别
LoggerFactoryResolver.Provider = new GodotLoggerFactoryProvider
{
    MinLevel = LogLevel.Warning  // 只记录警告及以上
};

// 使用条件日志
if (_logger.IsDebugEnabled())
{
    _logger.Debug("高频数据: {0}", data);
}

// 避免在 _Process 中频繁记录
public override void _Process(double delta)
{
    // ✗ 不好 - 每帧都记录
    // _logger.Debug("帧更新");

    // ✓ 好 - 只在特定条件下记录
    if (someErrorCondition)
    {
        _logger.Error("检测到错误");
    }
}
```

### 问题：如何记录结构化日志？

**解答**：
使用参数化日志或 `IStructuredLogger` 接口：

```csharp
// 参数化日志
logger.Info("玩家登录: UserId={0}, UserName={1}, Level={2}",
    userId, userName, level);

// 使用结构化日志（如果实现了 IStructuredLogger）
if (logger is IStructuredLogger structuredLogger)
{
    structuredLogger.Log(LogLevel.Info, "玩家登录",
        ("UserId", userId),
        ("UserName", userName),
        ("Level", level));
}
```

## 相关文档

- [核心日志系统](/zh-CN/core/logging) - GFramework 核心日志功能
- [Godot 架构集成](/zh-CN/godot/architecture) - Godot 架构系统
- [Godot 扩展](/zh-CN/godot/extensions) - Godot 扩展方法
- [最佳实践](/zh-CN/best-practices/architecture-patterns) - 架构最佳实践

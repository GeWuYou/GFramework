---
title: Godot 日志系统
description: 以当前 GFramework.Godot.Logging 源码与 CoreGrid 接线为准，说明 Godot 日志 provider、控制台输出语义与接入边界。
---

# Godot 日志系统

`GFramework.Godot` 当前的日志能力很收敛：它不是一套独立于 Core 的新日志框架，而是把现有 `ILogger` 调用面接到
Godot 控制台。

换句话说，Godot 侧真正新增的是 provider / factory / logger 这层输出适配，而不是新的日志 API。业务代码仍然继续使用
`LoggerFactoryResolver.Provider.CreateLogger(...)` 或 `[Log]` 生成的 `ILogger` 字段。

## 当前公开入口

### `GodotLogger`

`GodotLogger` 继承自 `AbstractLogger`，负责把日志写到 Godot 的输出 API：

```csharp
public sealed class GodotLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info)
    : AbstractLogger(name ?? RootLoggerName, minLevel)
```

当前实现里的几个关键语义：

- 时间戳使用 `DateTime.UtcNow`
- 输出前缀格式是 `[yyyy-MM-dd HH:mm:ss.fff] LEVEL [LoggerName]`
- `exception` 不会被单独结构化处理，而是直接追加到消息后面
- `Trace` / `Debug` 走 `GD.PrintRich(...)`
- `Info` / `Warning` / `Error` / `Fatal` 分别走 Godot 自身的普通、警告和错误输出通道

### `GodotLoggerFactory`

`GodotLoggerFactory` 只负责按名称和最小级别创建 `GodotLogger`：

```csharp
public class GodotLoggerFactory : ILoggerFactory
{
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info);
}
```

它本身不做缓存，也不额外增加过滤规则。

### `GodotLoggerFactoryProvider`

`GodotLoggerFactoryProvider` 是当前最常用的接入点：

```csharp
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    public LogLevel MinLevel { get; set; }
    public ILogger CreateLogger(string name);
}
```

它内部用 `CachedLoggerFactory` 包装 `GodotLoggerFactory`。缓存 key 由 `name` 和 `MinLevel` 共同组成，所以：

- 同名、同 `MinLevel` 的 logger 会复用实例
- 调整 `MinLevel` 后，新创建的 logger 会走新的缓存 key
- 已经持有的旧 logger 不会被原地改写

## 最小接入路径

### 1. 在 `ArchitectureConfiguration` 中挂上 Godot provider

当前仓库里更稳的接法，不是到处直接改全局 `LoggerFactoryResolver.Provider`，而是在架构配置里显式提供
`LoggerProperties.LoggerFactoryProvider`。`ai-libs/CoreGrid/global/GameEntryPoint.cs` 现在就是这样接的。

```csharp
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Architectures;
using GFramework.Godot.Logging;

var architecture = new GameArchitecture(
    new ArchitectureConfiguration
    {
        LoggerProperties = new LoggerProperties
        {
            LoggerFactoryProvider = new GodotLoggerFactoryProvider
            {
                MinLevel = LogLevel.Debug
            }
        }
    },
    environment);

architecture.Initialize();
```

这样做的好处是：

- 日志 provider 和架构启动配置放在同一个入口
- 不会把“Godot 控制台输出”误写成全局静态默认前提
- 和 `ArchitectureConfiguration` 默认使用 `ConsoleLoggerFactoryProvider` 的 Core 接线方式保持一致

### 2. 业务代码继续使用标准 `ILogger`

配置好 provider 之后，Godot 节点、System、Model、router、factory 都继续通过统一入口拿 logger：

```csharp
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using Godot;

public partial class SettingsPanel : Control
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(SettingsPanel));

    public override void _Ready()
    {
        Log.Info("SettingsPanel ready.");
    }
}
```

如果你已经在用 `GFramework.Core.SourceGenerators`，也可以继续让 `[Log]` 生成字段。Godot provider 只改变输出落点，
不会改变 `[Log]` 的生成契约。

### 3. Scene / UI 迁移日志会自动复用同一套 provider

`GFramework.Game.Scene.Handler.LoggingTransitionHandler` 和
`GFramework.Game.UI.Handler.LoggingTransitionHandler` 都是普通 `ILogger` 使用者。只要当前架构挂的是
`GodotLoggerFactoryProvider`，这些迁移日志就会直接进 Godot 控制台。

```csharp
using GFramework.Game.Scene.Handler;
using GFramework.Game.UI.Handler;

RegisterHandler(new LoggingTransitionHandler());
```

这也说明 Godot 日志页不需要重新定义一套“Godot 专用场景日志接口”；现有 Game 运行时日志在 Godot 宿主里本来就会复用
这套 provider。

## Godot 控制台输出语义

当前 `GodotLogger.Write(...)` 的级别映射如下：

| 日志级别 | Godot 输出 API | 当前行为 |
| --- | --- | --- |
| `Trace` | `GD.PrintRich(...)` | 使用灰色富文本输出 |
| `Debug` | `GD.PrintRich(...)` | 使用青色富文本输出 |
| `Info` | `GD.Print(...)` | 普通控制台输出 |
| `Warning` | `GD.PushWarning(...)` | 进入 Godot 警告通道 |
| `Error` | `GD.PrintErr(...)` | 输出到错误流 |
| `Fatal` | `GD.PushError(...)` | 进入 Godot 错误通道 |

异常追加格式也来自当前实现本身：

```text
[2026-04-22 10:30:47.012] ERROR   [SaveSystem] 保存游戏失败
System.IO.IOException: ...
```

如果你需要 JSON formatter、rolling file、namespace 级过滤、structured sink 组合，这已经超出
`GFramework.Godot.Logging` 当前职责，应该回到 [Core 日志系统](../core/logging.md) 设计 provider 组合。

## 什么时候用手写 logger，什么时候用 `[Log]`

- 手写 `LoggerFactoryResolver.Provider.CreateLogger(...)`
  - 少量入口类
  - 需要自己控制字段名、静态/实例生命周期
  - 想明确看到 logger 初始化位置
- 用 `[Log]`
  - Godot 节点、controller、system 上有大量重复 logger 字段样板
  - 你已经引用 `GFramework.Core.SourceGenerators`
  - 想把 logger 字段生成交给编译期

这里的边界要分清：

- Godot provider：来自 `GFramework.Godot`
- `[Log]` 生成器：来自 `GFramework.Core.SourceGenerators`

它们是可组合关系，不是上下位替代关系。

## 当前边界

- 当前推荐接法是把 `GodotLoggerFactoryProvider` 放进 `ArchitectureConfiguration.LoggerProperties`；直接赋值
  `LoggerFactoryResolver.Provider` 仍然可用，但不该再写成默认采用路径
- `GFramework.Godot.Logging` 只解决 Godot 控制台输出，不提供文件落盘、JSON formatter、异步 appender 或按 namespace
  的复杂过滤
- `GodotLogger` 只改变输出方式，不改变 `ILogger` 接口本身；业务代码不需要切换到 Godot 专用日志 API
- `[Log]`、`[ContextAware]` 这类字段注入能力不属于 `GFramework.Godot.Logging`
- Scene / UI 的 `LoggingTransitionHandler` 位于 `GFramework.Game`，Godot 侧只是通过 provider 让它们输出到 Godot 控制台
- 当前 `GodotLogger` 使用的是 UTC 时间戳；如果项目需要本地时区展示，需要自定义 provider / logger，而不是假定当前实现会自动转换

## 继续阅读

- [Core 日志系统](../core/logging.md)
- [日志生成器](../source-generators/logging-generator.md)
- [Godot 运行时集成](./index.md)
- [Godot 场景系统](./scene.md)
- [Godot UI 系统](./ui.md)

---
title: Godot 日志系统
description: 以当前 GFramework.Godot.Logging 源码与 CoreGrid 接线为准，说明 Godot 日志 provider、控制台输出语义与接入边界。
---

# Godot 日志系统

`GFramework.Godot` 当前的日志能力仍然以 Core 的 `ILogger` 调用面为中心，但已经不再只是一个薄输出适配层。
除了把日志写到 Godot 控制台，它现在还补上了 Godot 宿主常见的接入便利层：

- `GodotLog` 静态入口
- 配置文件自动发现
- 运行期配置热重载
- 延迟 logger 解析，适合 `static readonly` 字段

业务代码仍然继续使用 `LoggerFactoryResolver.Provider.CreateLogger(...)`、`GodotLog.CreateLogger(...)` 或 `[Log]`
生成的 `ILogger` 字段；Godot 侧没有额外引入第二套业务日志 API。

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

当前 provider 会按 logger 名称缓存实例，但 logger 本身会在写入时读取当前配置快照，所以：

- 同名 logger 会复用实例
- 调整 provider 最小级别或热更新配置后，已持有的 logger 会立即看到新行为
- 不需要为了刷新模板、颜色或级别而重新创建 logger

### `GodotLog`

`GodotLog` 是新增的 Godot 宿主友好入口：

```csharp
using GFramework.Godot.Logging;

GodotLog.Configure(options =>
{
    options.Mode = GodotLoggerMode.Debug;
});

GodotLog.UseAsDefaultProvider();

var logger = GodotLog.CreateLogger<Main>();
```

它提供三件事：

- 在第一次真正创建 provider 前允许代码覆写 `GodotLoggerOptions`
- 自动按 `GODOT_LOGGER_CONFIG` -> 可执行目录 `appsettings.json` -> `res://appsettings.json` 顺序发现配置
- 返回延迟解析 logger，避免 `static readonly` 字段过早锁死配置

## 最小接入路径

### 1. 在 `ArchitectureConfiguration` 中挂上 Godot provider

当前更稳的接法，不是到处直接改全局 `LoggerFactoryResolver.Provider`，而是在架构配置里显式提供
`LoggerProperties.LoggerFactoryProvider`。

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
不会改变 `[Log]` 的生成契约。需要静态字段延迟初始化时，也可以直接用 `GodotLog.CreateLogger<T>()`。

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

结构化属性如果通过 `IStructuredLogger` 或 `LogContext` 传入，也会追加到模板里的 `{properties}` 占位符。

异常追加格式仍然来自当前实现本身：

```text
[2026-04-22 10:30:47.012] ERROR   [SaveSystem] 保存游戏失败
System.IO.IOException: ...
```

如果你需要 JSON formatter、rolling file、namespace 级过滤或 structured sink 组合，可继续阅读
[Core 日志系统](../core/logging.md) 里的 provider 组合方式。

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

- 当前推荐接法仍然是把 `GodotLoggerFactoryProvider` 放进 `ArchitectureConfiguration.LoggerProperties`；如果项目是纯
  Godot 宿主，也可以在入口直接调用 `GodotLog.UseAsDefaultProvider()`
- `GFramework.Godot.Logging` 只解决 Godot 控制台输出，不提供文件落盘、JSON formatter、异步 appender 或按 namespace
  的复杂过滤
- `GodotLogger` 只改变输出方式，不改变 `ILogger` 接口本身；业务代码不需要切换到 Godot 专用日志 API
- `[Log]`、`[ContextAware]` 这类字段注入能力不属于 `GFramework.Godot.Logging`
- Scene / UI 的 `LoggingTransitionHandler` 位于 `GFramework.Game`，Godot 侧只是通过 provider 让它们输出到 Godot 控制台
- 当前 `GodotLogger` 使用的是 UTC 时间戳；如果项目需要本地时区展示，需要自定义 provider / logger，而不是假定当前实现会自动转换
- 当前配置热重载只覆盖 Godot logger 自身的模板、颜色、模式和级别；它没有把 `Microsoft.Extensions.Logging` 的整个
  options / builder 模型搬进来

## 继续阅读

- [Core 日志系统](../core/logging.md)
- [日志生成器](../source-generators/logging-generator.md)
- [Godot 运行时集成](./index.md)
- [Godot 场景系统](./scene.md)
- [Godot UI 系统](./ui.md)

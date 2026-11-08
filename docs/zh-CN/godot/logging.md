---
title: Godot 日志系统
description: 说明 Godot 宿主如何通过 GodotLog provider 接入 Microsoft.Extensions.Logging，并把标准日志输出到 Godot 控制台。
---

# Godot 日志系统

`GFramework.Godot` 的日志能力围绕标准 `Microsoft.Extensions.Logging` 契约组织：业务代码面向 `ILogger` / `ILogger<T>`，Godot 侧提供一个标准 `ILoggerProvider`，把日志调用渲染到 Godot 控制台。Godot 侧不引入第二套业务日志 API。

- `GodotLog`：静态组合入口，负责配置发现、热重载与 provider 生命周期
- `GodotLog.Provider`：标准 `ILoggerProvider`，由宿主的 `ILoggerFactory` 持有
- `GodotLogAppender`：Core 输出引擎（appender 管线）的 Godot 控制台落点
- 配置文件自动发现与运行期热重载

不存在进程级默认 provider 覆盖。每个宿主用 `LoggerFactory.Create(...)` 组合自己的工厂，架构实例之间互不影响。

## 最小接入路径

### 1. 组合工厂并交给架构配置

DI 宿主用 `LoggerFactory.Create` 把 `GodotLog.Provider` 挂进标准工厂，再交给 `LoggerProperties.LoggerFactory`：

```csharp
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Architectures;
using GFramework.Godot.Logging;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddProvider(GodotLog.Provider);
});

var architecture = new GameArchitecture(
    new ArchitectureConfiguration
    {
        LoggerProperties = new LoggerProperties
        {
            LoggerFactory = loggerFactory
        }
    },
    environment);

await architecture.InitializeAsync();
```

传入的工厂所有权归调用方；需要文件输出等更多落点时，在同一个 builder 上继续 `AddProvider(new AppenderLoggerProvider(...))`，见 [Core 日志系统](../core/logging.md)。

### 2. 业务代码通过构造注入或 [Log] 获取 logger

配置好工厂后，节点、System、Utility 等组件继续面向标准 `ILogger`：

```csharp
using Microsoft.Extensions.Logging;
using Godot;

public partial class SettingsPanel : Control
{
    private readonly ILogger<SettingsPanel> _logger;

    public SettingsPanel(ILogger<SettingsPanel> logger)
    {
        _logger = logger;
    }

    public override void _Ready()
    {
        _logger.LogInformation("SettingsPanel ready.");
    }
}
```

非 DI 宿主（例如没有容器的编辑器工具脚本）可以直接使用 `GodotLog.CreateLogger`：

```csharp
using GFramework.Godot.Logging;

private static readonly ILogger Log = GodotLog.CreateLogger(nameof(SettingsPanel));
```

如果你在用 `GFramework.Core.SourceGenerators`，也可以让 `[Log]` 生成实例日志属性，见[日志生成器](../source-generators/logging-generator.md)。`GodotLog.CreateLogger<T>()` 适合 `static readonly` 字段：logger 延迟解析配置，首次写入才锁定行为。

## `GodotLog` 入口

```csharp
using GFramework.Godot.Logging;
using Microsoft.Extensions.Logging;

// 必须在 provider 或配置源首次创建前调用
GodotLog.Configure(options =>
{
    options.Mode = GodotLoggerMode.Debug;
});

// 标准 ILoggerProvider，交给宿主工厂组合
ILoggerProvider provider = GodotLog.Provider;

// 非 DI 宿主的直接入口
ILogger logger = GodotLog.CreateLogger("Bootstrap");
ILogger typed = GodotLog.CreateLogger<Main>();
```

- `GodotLog.Configure(Action<GodotLoggerOptions>)`：在日志管线物化前用代码覆盖默认配置；provider 或配置源创建后再调用会抛出异常。
- `GodotLog.Provider`：访问时会创建 provider，但不会锁定配置源，后续的 `Configure` 和热重载仍然生效。
- `GodotLog.ConfigurationPath`：返回当前会命中的配置文件路径，适合启动诊断和测试断言；读取它不会提前创建配置源。
- `GodotLog.Shutdown()`：停止配置热重载监听并释放 provider。长生命周期服务器或测试 teardown 可以在退出时调用；已创建的 logger 继续使用最后一次成功发布的配置快照，之后访问 `Provider` 或 `CreateLogger` 会抛出异常。

## 配置文件与热重载

最小可复制的 `appsettings.json` 只需要 `Logging` 根节点。`LogLevel` 使用 `Default` 和类别名控制过滤阈值，`GodotLogger` 控制 Godot 输出模式、模板和颜色：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Game.Services": "Debug"
    },
    "GodotLogger": {
      "Mode": "Debug",
      "DebugMinLevel": "Debug",
      "ReleaseMinLevel": "Information",
      "DebugOutputTemplate": "[{timestamp:HH:mm:ss.fff}] [color={color}]{level:u3}[/color] {message}{properties}",
      "ReleaseOutputTemplate": "[{timestamp:HH:mm:ss.fff}] [{level:u3}] [{category:l16}] {message}{properties}",
      "Colors": {
        "Information": "white",
        "Warning": "orange",
        "Error": "red"
      }
    }
  }
}
```

级别别名使用 `Microsoft.Extensions.Logging` 名称（`information`、`critical` 等），大小写不敏感。

配置文件发现顺序固定为：

1. `GODOT_LOGGER_CONFIG` 环境变量指向的文件
2. 导出程序或测试进程所在目录的 `appsettings.json`
3. Godot 项目资源根目录的 `res://appsettings.json`

在编辑器项目里，`res://appsettings.json` 放在项目根目录；在导出包或专用服务器里，优先把 `appsettings.json` 放到可执行文件同目录，便于运维脚本替换。

运行中修改已发现的配置文件会热重载 `Logging:LogLevel` 与 `Logging:GodotLogger` 下的模式、最小级别、模板和颜色。已创建的 logger 不会重新实例化，但下一次级别判定和写入会读取最新成功发布的配置快照。热重载解析失败或文件被短暂锁定时保留上一份可用配置。

## Godot 控制台输出语义

`GodotLog.Provider` 渲染标准日志调用时按级别选择 Godot 输出 API：

| 日志级别 | Debug 模式 | Release 模式 |
| --- | --- | --- |
| `Trace` / `Debug` / `Information` | `GD.PrintRich(...)` | `GD.Print(...)` |
| `Warning` | `GD.PrintRich(...)` + `GD.PushWarning(...)` | `GD.Print(...)` |
| `Error` / `Critical` | `GD.PrintRich(...)` + `GD.PushError(...)` | `GD.Print(...)` |

- Debug 模式使用富文本模板与颜色；Release 模式使用普通文本模板。
- 模板里的 `{properties}` 占位符来自标准结构化日志状态（`ILogger.Log` 的参数）。
- 异常通过 `GD.PrintErr(...)` 追加在渲染后的主消息之后。
- 时间戳使用 UTC。

## Godot 作为 Core appender 管线的落点

`GodotLogAppender` 实现 Core 的 `ILogAppender`，适合在文件、JSON、异步输出等 Core appender 组合中把 Godot 控制台作为其中一个落点：

```csharp
using GFramework.Core.Logging;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Logging.Formatters;
using GFramework.Godot.Logging;

var provider = new AppenderLoggerProvider(new ILogAppender[]
{
    new GodotLogAppender(),
    new AsyncLogAppender(new FileAppender(
        ProjectSettings.GlobalizePath("user://logs/game.log"),
        new DefaultLogFormatter()))
});

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddProvider(provider);
});
```

Godot 项目的 `user://` 路径需要先用 `ProjectSettings.GlobalizePath(...)` 转换为普通文件系统路径再交给 `FileAppender`。JSON formatter、namespace filter、rolling file 等继续由 Core logging 组件提供，见 [Core 日志系统](../core/logging.md)。

## 当前边界

- `GFramework.Godot.Logging` 只提供 Godot 控制台落点；文件落盘、JSON formatter、异步 appender 或按 namespace 的过滤继续使用 Core 日志组件组合
- `GodotLog.Provider` 只改变输出方式，不改变 `ILogger` 契约；业务代码不需要切换到 Godot 专用日志 API
- 没有 `GodotLog.UseAsDefaultProvider()` 之类的进程级覆盖；每个宿主组合自己的工厂
- `[Log]`、`[ContextAware]` 这类注入能力属于 `GFramework.Core.SourceGenerators`，不属于 `GFramework.Godot.Logging`
- 配置热重载只覆盖 Godot logger 自身的模板、颜色、模式和级别；不包含 `Microsoft.Extensions.Logging` 的完整 options / builder 模型
- 时间戳使用 UTC；需要本地时区展示时自定义模板或 appender

## 继续阅读

- [Core 日志系统](../core/logging.md)
- [日志生成器](../source-generators/logging-generator.md)
- [Godot 运行时集成](./index.md)
- [Godot 场景系统](./scene.md)
- [Godot UI 系统](./ui.md)

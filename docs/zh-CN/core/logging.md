---
title: Logging
description: 说明 GFramework.Core.Logging 的日志接口、组合方式与常见使用入口。
---

# Logging

`GFramework.Core.Logging` 是 Core runtime 的默认日志实现。只加载抽象层时，`LoggerFactoryResolver` 会退回
silent provider；加载 `GFramework.Core` 或在 `ArchitectureConfiguration` 里显式提供 provider 后，日志才会
真正输出。

## 最小用法

```csharp
using GFramework.Core.Abstractions.Logging;

var logger = LoggerFactoryResolver.Provider.CreateLogger("Bootstrap");

logger.Info("Application started");
logger.Warn("Config file missing");
```

默认 `ArchitectureConfiguration` 会把 provider 配成 `ConsoleLoggerFactoryProvider`，最小级别是 `Info`。如果你
直接走标准 `Architecture` 启动路径，这条配置会自动生效。

## 在 Architecture 中调整日志级别

```csharp
using GFramework.Core.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Logging;

var configuration = new ArchitectureConfiguration
{
    LoggerProperties = new LoggerProperties
    {
        LoggerFactoryProvider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Debug
        }
    }
};
```

如果你只是想减少噪音或临时打开 `Debug`，通常只调 `MinLevel` 就够了。

## 结构化日志与上下文

默认 Core logger 实现支持 `IStructuredLogger` 和 `LogContext`。当你需要把 `requestId`、`sceneName` 之类的
上下文随异步流透传时，优先用上下文属性，而不是把所有信息拼进字符串。

```csharp
using GFramework.Core.Abstractions.Logging;

var logger = LoggerFactoryResolver.Provider.CreateLogger("Matchmaking");

using (LogContext.Push("RequestId", requestId))
{
    if (logger is IStructuredLogger structured)
    {
        structured.Log(
            LogLevel.Info,
            "Player matched",
            ("PlayerId", playerId),
            ("RoomId", roomId));
    }
}
```

## 当前仓库内置的常用实现

- `ConsoleLoggerFactoryProvider`
- `ConsoleLoggerFactory`
- `CompositeLogger`
- `LoggingConfigurationLoader`

如果你需要文件输出、rolling file、async appender 或 JSON formatter，可以先用
`LoggingConfigurationLoader` 读取 `LoggingConfiguration`，再把自定义 `ILoggerFactoryProvider` 挂到
`ArchitectureConfiguration.LoggerProperties.LoggerFactoryProvider` 或 `LoggerFactoryResolver.Provider`。

## 什么时候该换 provider

下面这些场景通常不该只靠改 `MinLevel`：

- 需要文件输出、rolling file 或 async appender
- 需要按 namespace / level 做过滤
- 需要 JSON 格式日志
- 需要组合多个 appender

这时更合理的做法是保留 `ILogger` 调用面不变，只替换 provider / factory / formatter / appender 组合。

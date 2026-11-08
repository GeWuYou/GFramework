---
title: 日志（Logging）
description: 说明 GFramework 如何使用 Microsoft.Extensions.Logging 契约记录日志、架构实例如何接线日志工厂，以及如何继续使用内置输出引擎。
---

# 日志（Logging）

GFramework 的日志契约是 `Microsoft.Extensions.Logging` 的标准接口：`ILogger`、`ILogger<T>`、`ILoggerFactory`、`ILoggerProvider` 与 `LogLevel`（`Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical`）。框架不再提供自有日志接口，也没有进程级全局日志入口；每个架构实例的日志互不影响。

组件获取 logger 的首选方式是构造注入 `ILogger<T>`：

```csharp
using Microsoft.Extensions.Logging;

public sealed class MatchmakingService
{
    private readonly ILogger<MatchmakingService> _logger;

    public MatchmakingService(ILogger<MatchmakingService> logger)
    {
        _logger = logger;
    }

    public void OnPlayerMatched(string playerId, string roomId)
    {
        _logger.LogInformation("Player matched: {PlayerId} in {RoomId}", playerId, roomId);
    }
}
```

## 架构实例接线

### 保留 ILogger 构造入口的服务

`ResourceManager`、`ConfigurationManager` 与 `PauseStackManager` 保留可选的 `ILogger` 构造参数。标准 `AddLogging()` 提供 `ILogger<T>`，不会自动提供按调用方类别区分的非泛型 `ILogger`。因此直接 `AddSingleton<ResourceManager>()` 等泛型激活仍会使用构造函数的空日志默认值；需要日志时，使用明确的注册工厂选择类别：

```csharp
using GFramework.Core.Resource;
using GFramework.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

services.AddLogging(builder => builder.AddConsole());
services.AddSingleton(provider => new ResourceManager(
    provider.GetRequiredService<ILogger<ResourceManager>>()));
services.AddSingleton(provider => new ConfigurationManager(
    provider.GetRequiredService<ILogger<ConfigurationManager>>()));
```

Game 默认服务登记对 `PauseStackManager` 使用同样的类别选择。不要为了这些构造入口注册一个全局非泛型 `ILogger`，否则不同服务会共用错误的类别。

### 实例工厂

日志工厂在 `ArchitectureConfiguration.LoggerProperties.LoggerFactory` 上配置，类型是标准 `ILoggerFactory?`：

```csharp
using GFramework.Core.Architectures;
using GFramework.Core.Abstractions.Properties;
using Microsoft.Extensions.Logging;

var configuration = new ArchitectureConfiguration
{
    LoggerProperties = new LoggerProperties
    {
        LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        })
    }
};
```

接线规则：

- `LoggerFactory` 为空时，架构实例会创建一个独立的默认控制台工厂，并在架构销毁时负责释放它。
- 外部传入的工厂所有权归调用方，架构销毁不会释放它。
- 实例工厂会注册进该架构的 DI 容器；组件通过构造注入 `ILogger<T>` 获取日志。
- 不存在进程级日志覆盖：两个架构实例可以分别使用不同工厂，互不干扰。

## 通过 JSON 配置创建工厂

`LoggingConfigurationLoader` 可以从 JSON 读取输出配置，并用 `CreateFactory` 得到标准 `ILoggerFactory`：

```csharp
using GFramework.Core.Logging;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Architectures;

var factory = LoggingConfigurationLoader.CreateFactory(
    LoggingConfigurationLoader.LoadFromJson("logging.json"));

var configuration = new ArchitectureConfiguration
{
    LoggerProperties = new LoggerProperties
    {
        LoggerFactory = factory
    }
};
```

- `LoggingConfiguration.MinLevel` 映射为工厂的 `SetMinimumLevel(...)`。
- `LoggingConfiguration.LoggerLevels` 中的每一项映射为 `AddFilter(category, level)`。
- JSON 中的级别别名使用 `Microsoft.Extensions.Logging` 名称（`information`、`critical` 等），大小写不敏感。
- `LoadFromJson(string filePath)` 与 `LoadFromJsonString(string json)` 的读取方式保持不变。
- 创建工厂时，省略、空或 `null` 的 `Appenders` 均采用默认控制台输出；`LoggerLevels` 为 `null` 时不添加分类覆盖规则。

## 输出引擎：appender 管线

GFramework.Core 保留了自己的输出引擎。`GFramework.Core.Logging.AppenderLoggerProvider` 实现标准 `ILoggerProvider`，把标准日志调用转换为 `LogEntry` 交给 appender 管线。这样标准 `ILogger` 调用面和既有输出能力可以组合使用：

- Appender：`ConsoleAppender`（支持颜色）、`FileAppender`、`RollingFileAppender`、`AsyncLogAppender`、`StatisticsAppender`
- Formatter：`DefaultLogFormatter`、`JsonLogFormatter`
- Filter：`LogLevelFilter`、`NamespaceFilter`、`SamplingFilter`、`CompositeFilter`

appender 管线内的级别统一使用 MEL 的 `LogLevel`。单个 appender 抛出异常不会影响其它 appender 的输出；结构化模板中的 `{OriginalFormat}` 占位符本身不会进入属性集合，属性仍然来自标准结构化状态。

### 自定义 appender

需要新的输出落点时，实现 `ILogAppender`（位于 `GFramework.Core.Abstractions.Logging`），再通过 `AppenderLoggerProvider` 接入标准工厂：

```csharp
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using Microsoft.Extensions.Logging;

// 自定义 appender：实现 ILogAppender，在 Append(LogEntry) 中写出条目
public sealed class OtelLogAppender : ILogAppender
{
    public void Append(LogEntry entry)
    {
        // 把 entry 写入你的目标系统
    }

    public void Flush()
    {
        // 需要时冲刷缓冲
    }

    public void Dispose()
    {
        // 释放持有的外部资源；实现为空也可以
    }
}

var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new AppenderLoggerProvider(new[]
    {
        new OtelLogAppender()
    }));
});
```

`AppenderLoggerProvider` 由标准工厂持有并随工厂一起释放；实现了 `IDisposable` 的 appender 会在释放时被转发调用。

### 用 JSON 声明 appender

`LoggingConfiguration` 的 `Appenders` 列表同样交给 `AppenderLoggerProvider` 组装：

```json
{
  "MinLevel": "Information",
  "Appenders": [
    { "Type": "Console", "Formatter": "Default", "UseColors": true },
    { "Type": "RollingFile", "FilePath": "logs/game.log", "MaxFileSize": 10485760, "MaxFileCount": 5 },
    {
      "Type": "Async",
      "BufferSize": 10000,
      "InnerAppender": { "Type": "File", "FilePath": "logs/game.async.log" }
    }
  ]
}
```

## 什么时候直接注入，什么时候接入输出引擎

- 普通服务和组件：直接构造注入 `ILogger<T>`，把输出落地交给宿主选择。
- 需要文件、rolling file、异步缓冲、采样过滤或 JSON 格式输出时：用 `LoggingConfigurationLoader` 或手动 `AddProvider(new AppenderLoggerProvider(...))` 组装工厂，再交给 `LoggerProperties.LoggerFactory`。
- 需要 Godot 控制台输出时：见 [Godot 日志系统](../godot/logging.md)。

---
title: "应用日志"
description: "向游戏服务注入标准 .NET logger。"
---

# 应用日志 {#overview}

在组合根配置 `Microsoft.Extensions.Logging`，由服务通过构造函数接收 `ILogger<T>`。使用结构化参数保留事件字段，例如 `logger.LogInformation("Player matched: {PlayerId}", playerId)`。

[MinimalApp](../getting-started/quick-start.md) 展示控制台日志和实例级 Architecture 日志。引擎输出见[Godot 日志](../godot/logging.md)。

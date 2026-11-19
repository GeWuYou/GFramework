---
title: "输出到 Godot 控制台"
description: "将标准 .NET 日志接到 Godot。"
---

# 输出到 Godot 控制台 {#overview}

在宿主的 `ILoggerFactory` 上使用 `GodotLog.Provider`，由业务服务通过构造函数接收 `ILogger<T>`。每个架构使用自己的日志配置。需要额外输出时，在同一个工厂中配置相应 provider。

先在编辑器中触发一条信息和一条错误，确认级别、分类和异常内容出现在预期的控制台。参见[应用日志](../core/logging.md)。

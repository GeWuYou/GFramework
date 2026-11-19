---
title: "处理读取请求"
description: "从应用查询返回只读快照。"
---

# 处理读取请求 {#overview}

从查询方法返回调用者需要的投影或快照。使用统一管线时定义 `IRequest<TResponse>`，向处理器注入读取依赖，再通过 `ICqrsRuntime.SendAsync` 取得结果。

读取与写入可共享同一领域模型。注册及作用域见[CQRS 接入](../core/cqrs.md)。

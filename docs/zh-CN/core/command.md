---
title: "处理写入请求"
description: "在用例中维护业务约束，通过 CQRS 按需统一分发。"
---

# 处理写入请求 {#overview}

让服务方法或 CQRS 处理器执行写入。在需要管道时定义 `IRequest<TResponse>`，通过对应 `IRequestHandler<TRequest, TResponse>` 组织用例，并将取消令牌传给持久化或其他异步操作。

写入成功后返回操作结果，再按需要通知观察者。登记方式见[CQRS 接入](../core/cqrs.md)。

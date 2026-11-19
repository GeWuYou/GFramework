---
title: "CQRS 请求、通知与流"
description: "注册处理器，通过统一入口分发消息并组装管道行为。"
---

# CQRS 请求、通知与流 {#overview}

## 接入 {#setup}

安装 `GeWuYou.GFramework.Cqrs`，在组合根调用 `AddCqrsRuntime`。处理器通过构造函数接收依赖，消费方取得 `ICqrsRuntime` 后选择入口：

| 任务 | 调用 |
| --- | --- |
| 请求一个响应 | `SendAsync` |
| 通知多个处理器 | `PublishAsync` |
| 读取多个异步结果 | `CreateStream` |

需要[生成注册表](../source-generators/cqrs-handler-registry-generator.md#install)时，在声明处理器的项目中同时安装运行时和源码生成器。对运行时才取得的程序集，使用 `AddCqrsHandlersFromAssembly`。传入调用方的取消令牌。

## 管道与作用域 {#pipeline}

按外层到内层的顺序登记管道行为。为依赖 scoped 对象的行为使用 scoped 生命周期，并在同一调用方作用域解析 runtime；流式调用的作用域覆盖完整枚举。

<<< ../../../Samples/CqrsPipelineDemo/Program.cs#scoped-pipeline

```bash
dotnet run --project Samples/CqrsPipelineDemo -c Release
```

输出 `CQRS_PIPELINE_PASS` 表示处理器已返回响应，作用域已释放。另见[处理器注册表生成器](../source-generators/cqrs-handler-registry-generator.md)。

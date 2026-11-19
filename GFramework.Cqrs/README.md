# Cqrs

[简体中文](#简体中文) · [English](#english)

## 简体中文

使用标准 DI 分发异步请求、通知和流。安装 Cqrs 后在组合根添加 runtime、登记处理器，并把 ICqrsRuntime 注入调用方。

安装 `GFramework.Cqrs` 时会带入 `GFramework.Cqrs.Abstractions` 和 `GFramework.Core.Abstractions`。共享消息的工程可单独引用 `GFramework.Cqrs.Abstractions`。需要在编译时生成处理器注册表时，在处理器工程中另装 `GFramework.Cqrs.SourceGenerators`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/core/cqrs.html)

## English

Dispatch asynchronous requests, notifications, and streams with standard DI. Add the runtime in the composition root, register handlers, and inject ICqrsRuntime into callers.

`GFramework.Cqrs` brings in `GFramework.Cqrs.Abstractions` and `GFramework.Core.Abstractions`. Reference `GFramework.Cqrs.Abstractions` directly from projects that share message contracts. To generate a handler registry at build time, add `GFramework.Cqrs.SourceGenerators` to the handler project.

[Read the guide](https://gewuyou.github.io/GFramework/core/cqrs.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Cqrs
```

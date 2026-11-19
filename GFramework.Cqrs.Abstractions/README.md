# Cqrs.Abstractions

[简体中文](#简体中文) · [English](#english)

## 简体中文

在共享项目中定义 CQRS 消息、处理器与管道契约。分发消息使用 `GFramework.Cqrs`。需要生成处理器注册表时，在声明处理器的项目中同时安装 `GFramework.Cqrs` 和 `GFramework.Cqrs.SourceGenerators`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/core/cqrs.html)

## English

Define CQRS messages, handlers, and pipeline contracts in a shared project. Use `GFramework.Cqrs` to dispatch messages. To generate a handler registry, install both `GFramework.Cqrs` and `GFramework.Cqrs.SourceGenerators` in the project that declares the handlers.

[Read the guide](https://gewuyou.github.io/GFramework/core/cqrs.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions
```

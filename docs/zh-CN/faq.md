---
title: "常见问题"
description: "回答安装、架构和 Godot 采用中的常见问题。"
---

# 常见问题 {#overview}

## 项目需要哪些包？ {#packages}

从[安装表](./getting-started/installation.md)选择所需功能。Godot 包带入 Game 与 Core，源码生成器按功能添加。

## 每个服务都需要加入生命周期吗？ {#lifecycle}

普通类通过标准 DI 登记。需要应用协调启动或停止的对象通过 `AddComponent` 显式登记。参见[应用装配](./core/architecture.md)。

## 本地事件和 CQRS 请求如何选择？ {#messages}

用[同步事件](./core/events.md)通知观察者，用显式方法或[CQRS 请求](./core/cqrs.md)取得结果。需要统一的异步处理管线时使用 CQRS。

---
title: "应用装配与生命周期"
description: "用标准 DI 注册服务，并在需要时由 Architecture 协调启动和停止。"
---

# 应用装配与生命周期 {#overview}

## 注册和启动 {#registration}

在 `ConfigureServices` 登记依赖。普通类使用标准 DI 注册；需要初始化、停止或两者兼有的服务使用 `AddComponent`。通过构造函数传入依赖，保留每个组件的一个注册服务键。

<<< ../../../Samples/MinimalApp/OrderArchitecture.cs

## 生命周期 {#lifecycle}

1. 等待 `InitializeAsync` 创建 provider、按顺序启动组件并完成宿主接线。
2. 宿主就绪后解析服务并开放用户操作。对 scoped 服务在操作范围内创建 scope。
3. 退出时等待 `DestroyAsync`。组件逆序停止，宿主分离后释放 provider。

`order` 从小到大排列启动回调，同值按登记顺序。把依赖其他组件就绪的工作放进生命周期回调。

## 所有权 {#ownership}

DI 创建的可释放服务随 provider 或 scope 释放。通过实例重载登记已创建的组件时，由原调用者负责释放实例，Architecture 执行已登记的业务停止。

相关主题：[异步初始化](../core/async-initialization.md) · [依赖注入](../core/ioc.md)。

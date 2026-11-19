---
title: "连接 Godot 信号"
description: "使用 fluent 包装连接原生信号。"
---

# 连接 Godot 信号 {#overview}

使用 `SignalFluentExtensions.Signal` 从 `GodotObject` 和 `StringName` 开始建立连接。`SignalBuilder` 用于配置回调和连接参数，最后通过 Godot 的连接机制注册。

将连接放到拥有该节点的生命周期中。对于固定的场景节点绑定，可使用[BindNodeSignal 生成器](../source-generators/bind-node-signal-generator.md)。

---
title: "生成节点信号绑定"
description: "声明 Godot CLR 信号回调和生命周期接线。"
---

# 生成节点信号绑定 {#overview}

给回调方法添加 `[BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]`。让字段指向实际节点，回调签名匹配信号事件。

1. 在 `_Ready()` 中准备节点引用。若使用 `[GetNode]`，先调用 `__InjectGetNodes_Generated()`。
2. 调用 `__BindNodeSignals_Generated()` 订阅。
3. 在 `_ExitTree()` 中调用 `__UnbindNodeSignals_Generated()` 释放。

检查生成诊断，补上自定义生命周期中缺少的调用。运行时连接见[Godot 信号](../godot/signal.md)。

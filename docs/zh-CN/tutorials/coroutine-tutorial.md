---
title: "编排跨帧游戏流程"
description: "在游戏循环里启动、等待并观察协程。"
---

# 编排跨帧游戏流程 {#overview}

## 驱动调度器 {#host}

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Host{csharp}

## 启动流程 {#flow}

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Flow{csharp}

## 观察结束 {#completion}

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Observe{csharp}

游戏关闭时由所属组件停止调度工作。接入原生阶段见[Godot 协程](../godot/coroutine.md)。

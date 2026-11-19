---
title: "协程调度"
description: "在宿主循环中推进跨帧游戏流程。"
---

# 协程调度 {#overview}

用 `IEnumerator<IYieldInstruction>` 表达跨帧流程，通过 `CoroutineScheduler.Run` 启动，在宿主循环中调用 `Update`。`Run` 执行到首个等待并返回句柄。

## 选择等待与阶段 {#stages}

为宿主声明实际推进的 `CoroutineExecutionStage`。游戏内计时选择缩放时间，跨暂停的计时选择实时时间。将启动、控制和阶段推进集中到驱动线程。跨线程取消请求通过令牌传递，由驱动线程完成清理。

## 观察结果 {#completion}

保留句柄观察完成、取消或失败。使用拥有明确寿命的 scope 管理一起停止的流程。参见[可运行协程示例](../tutorials/coroutine-tutorial.md)和[Godot 阶段接入](../godot/coroutine.md)。

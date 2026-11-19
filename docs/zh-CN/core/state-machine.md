---
title: "异步状态机"
description: "按顺序执行状态进入、退出、回退和清理。"
---

# 异步状态机 {#overview}

```csharp
using GFramework.Core.State;

await using var machine = new StateMachine(maxHistorySize: 10);
machine.Register(new MenuState()).Register(new PlayingState());
await machine.ChangeToAsync<MenuState>();
await machine.ChangeToAsync<PlayingState>();

public sealed class MenuState : StateBase;
public sealed class PlayingState : StateBase;
```

## 切换 {#transitions}

先注册所有状态，再开始切换。切换依次检查退出条件、退出旧状态、进入新状态，成功后更新历史。`GoBackAsync` 同样检查退出条件。把后续切换的触发放到状态回调之外。

## 故障与停止 {#shutdown}

状态回调故障可从 `Fault` 查看。等待 `StopAsync` 完成退出及业务清理后，再创建替代机器。状态对象由创建者或 DI 容器释放。涉及 Godot 节点的操作在引擎线程发起。

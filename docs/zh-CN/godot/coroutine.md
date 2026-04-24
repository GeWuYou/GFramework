---
title: Godot 协程系统
description: 说明 GFramework.Godot.Coroutine 的宿主集成能力、阶段语义与使用方式。
---

# Godot 协程系统

## 概述

`GFramework.Godot.Coroutine` 在 Core 协程内核之上提供 Godot 宿主集成，负责把 Godot 的不同更新循环映射为真实的协程阶段语义：

- `Segment.Process`
- `Segment.ProcessIgnorePause`
- `Segment.PhysicsProcess`
- `Segment.DeferredProcess`

它同时补充了以下宿主能力：

- 节点归属协程运行入口
- 节点退树自动终止
- Godot 真实时间源
- 句柄控制与快照查询

## 启动协程

### 直接运行枚举器

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;

public partial class DemoNode : Node
{
    public override void _Ready()
    {
        Demo().RunCoroutine();
    }

    private IEnumerator<IYieldInstruction> Demo()
    {
        GD.Print("start");
        yield return new Delay(1.0);
        yield return new WaitForEndOfFrame();
        GD.Print("done");
    }
}
```

默认情况下，`RunCoroutine()` 会在 `Segment.Process` 上运行。

### 以 Node 作为生命周期所有者运行

更推荐的方式是以节点为入口运行协程：

```csharp
public override void _Ready()
{
    this.RunCoroutine(LongRunningTask(), Segment.Process, tag: "ui-blink");
}
```

这会自动把协程登记为该节点归属协程，并在节点退出场景树时终止它。

你仍然可以继续使用 `CancelWith(...)` 包装已有枚举器；它适合把一个协程显式绑定到多个节点生命周期。

## Segment 与阶段语义

Godot 层会把不同 segment 映射为不同的 `CoroutineExecutionStage`：

- `Segment.Process`
    - 对应默认更新阶段
    - 场景树暂停时不会推进
- `Segment.ProcessIgnorePause`
    - 同样对应默认更新阶段
    - 场景树暂停时仍会推进
- `Segment.PhysicsProcess`
    - 对应固定更新阶段
    - `WaitForFixedUpdate` 会在这里真实完成
- `Segment.DeferredProcess`
    - 对应帧结束阶段
    - `WaitForEndOfFrame` 会在这里真实完成

示例：

```csharp
this.RunCoroutine(PhysicsRoutine(), Segment.PhysicsProcess);
this.RunCoroutine(UiAnimation(), Segment.ProcessIgnorePause);
```

## 时间等待语义

Godot 集成层为每个调度器同时提供了两套时间源：

- 缩放时间
    - 来自 `_Process` / `_PhysicsProcess` 的帧增量
- 真实时间
    - 来自 Godot 单调时钟，不受时间缩放和暂停影响

因此：

- `Delay` / `WaitForSecondsScaled` 使用宿主帧增量
- `WaitForSecondsRealtime` 使用真实时间

这意味着 UI 或暂停菜单中的协程可以安全使用 `WaitForSecondsRealtime` 保持真实计时。

## 生命周期管理

### 自动归属

```csharp
var handle = this.RunCoroutine(LoadAvatar(), tag: "avatar");
```

### 手动绑定多个节点

```csharp
LongRunningTask()
    .CancelWith(this, panelNode)
    .RunCoroutine();
```

### 主动清理

```csharp
Timing.KillCoroutine(handle);
Timing.KillCoroutines(this);
Timing.KillCoroutines("avatar");
Timing.KillAllCoroutines();
```

## 调试与查询

```csharp
if (Timing.TryGetCoroutineSnapshot(handle, out var snapshot))
{
    GD.Print(snapshot.ExecutionStage);
    GD.Print(snapshot.WaitingInstructionType);
}

var ownedCount = Timing.GetOwnedCoroutineCount(this);
```

实例级计数器：

- `Timing.Instance.ProcessCoroutines`
- `Timing.Instance.ProcessIgnorePauseCoroutines`
- `Timing.Instance.PhysicsCoroutines`
- `Timing.Instance.DeferredCoroutines`

## 延迟调用

```csharp
Timing.CallDelayed(1.0, () => GD.Print("1 秒后执行"));
Timing.CallDelayed(1.0, () => GD.Print("节点仍然有效时执行"), this);
```

第二个重载内部使用节点归属语义，因此节点退树后不会再触发动作。

## 与 IContextAware 集成

Godot 层还提供以下扩展方法，用于把命令、查询和通知直接包装成协程并交给 Timing 调度：

- `RunCommandCoroutine(...)`
- `RunCommandCoroutine<TResponse>(...)`
- `RunQueryCoroutine<TResponse>(...)`
- `RunPublishCoroutine(...)`

这些 API 仍然可以与 `Segment`、节点归属和标签控制一起使用。

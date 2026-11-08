---
title: Godot 协程系统
description: 将协程阶段、暂停策略、作用域及父子任务接入 Godot 节点生命周期。
---

# Godot 协程系统

`GFramework.Godot.Coroutine.Timing` 把 Godot 的 Process、Physics 和延迟回调接到同一个 Core 调度器。阶段切换保留句柄、所有权、取消与完成结果；节点归属协程在节点退出场景树时取消。

## 节点归属入口

```csharp
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

public partial class DemoNode : Node
{
    public override void _Ready()
    {
        this.RunCoroutine(PlaySequence(), tag: "intro");
    }

    private IEnumerator<IYieldInstruction> PlaySequence()
    {
        yield return new Delay(1);
        yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
        GD.Print("fixed tick");
        yield return new SwitchTo(CoroutineExecutionStage.Deferred);
        GD.Print("deferred callback");
        yield return new SwitchTo(CoroutineExecutionStage.Update);
    }
}
```

推荐通过 Node 入口启动有明确所有者的流程。需要全局独立任务时可以使用 `Timing.RunCoroutine(...)`，并由调用方负责其生命周期。

`Run` 会立即执行至首次等待，因此首次等待前的代码在调用位置执行。需要等待物理回调后才能执行的代码，应先切换到 `FixedUpdate`。切换后保持目标阶段；普通延时和帧等待不会自动切回 Process。

## Segment、阶段与暂停

| 启动 Segment | 初始阶段 | 场景暂停时 |
| --- | --- | --- |
| `Process` | `Update` | 冻结 |
| `ProcessIgnorePause` | `Update` | 继续 |
| `PhysicsProcess` | `FixedUpdate` | 冻结 |
| `DeferredProcess` | `Deferred` | 冻结 |

Segment 选择初始阶段与暂停策略；随后 `SwitchTo` 只改变执行阶段，不改变任务的暂停策略。例如从 `ProcessIgnorePause` 启动的 UI 任务切换到 Physics 后仍忽略场景暂停。

`Deferred` 使用 Godot 延迟调用，是引擎空闲处理阶段，不承诺屏幕已经绘制或 GPU 渲染完成。同阶段切换至少等待下一次该阶段推进，不会在同一次回调中反复执行。

暂停会冻结普通游戏任务的所有阶段，取消和节点退出清理仍可处理。`PauseCoroutine(handle)` 是任务自身的暂停请求，与场景暂停及子任务自身的请求分别管理。

## 时间等待

`Delay` 和 `WaitForSecondsScaled` 使用引擎阶段的缩放增量。`WaitForSecondsRealtime` 使用 Godot 单调时钟，不受时间缩放影响；宿主对各阶段分别采样，普通任务恢复后不会补算冻结期间的等待进度。

```csharp
this.RunCoroutine(UiCountdown(), Segment.ProcessIgnorePause);

IEnumerator<IYieldInstruction> UiCountdown()
{
    yield return new WaitForSecondsRealtime(2);
    GD.Print("two real seconds");
}
```

忽略场景暂停与使用真实时间是两个选择。暂停期间需要继续的 UI 计时应同时选择这两者。

## 作用域与父子流程

节点默认持有 Supervisor 容器：一个独立任务失败不会取消该节点的其他独立任务。每个任务内部的子流程默认 FailFast，父任务取消时统一取消后代。

```csharp
this.RunCoroutine(context => RunSkill(context));

IEnumerator<IYieldInstruction> RunSkill(GFramework.Core.Coroutine.CoroutineContext context)
{
    context.RunChild(PlayEffect());
    context.RunChild(ApplyDamage());
    yield break;
}
```

父枚举器结束后，父任务等待两个子步骤收尾。子步骤失败使默认技能流程失败，并取消另一个步骤；节点下其他独立流程继续运行。

需要多个独立任务组成一个共同失败的操作时，显式创建节点拥有的作用域：

```csharp
var loading = this.CreateCoroutineScope(CoroutineFailurePolicy.FailFast);
loading.Run(LoadMap());
loading.Run(LoadCharacters());
```

作用域策略创建后固定。`Cancel()` 取消当前成员并保留作用域，`Dispose()` 关闭作用域。节点退树会关闭其全部作用域，包括显式创建的额外作用域；调用方无需再为子任务手动登记节点所有权。

## 控制、结果与清理

```csharp
Timing.PauseCoroutine(handle);
Timing.ResumeCoroutine(handle);
Timing.KillCoroutine(handle);
Timing.KillCoroutines(this);
Timing.KillCoroutines("intro");

if (Timing.TryGetCoroutineSnapshot(handle, out var snapshot))
{
    GD.Print(snapshot.ExecutionStage);
    GD.Print(snapshot.WaitingInstructionType);
}
```

启动后可先取得 `Timing.WaitForResultAsync(handle)`，再等待最终状态和异常；也可用 `Timing.TryGetCoroutineResult(handle, out result)` 查询保留历史。宿主退出时，已经取得的完成任务仍会收到收尾结果。监督模式隔离任务失败，但不会将失败当作成功或丢弃原始异常。

`GetOwnedCoroutineCount(node)` 包含节点作用域内的活跃子任务。句柄不再依赖有限的宿主槽位前缀，多宿主之间不会互相控制。

`CancelWith(node1, node2)` 用于额外绑定多个节点：经 Timing 启动后，任意节点退树会请求取消，即使协程正阻塞在长期等待中；内部枚举器仍被释放。它只在 Godot 主线程创建和释放。普通单节点任务优先采用 Node 归属入口或节点拥有的作用域。

`CallDelayed(delay, action, owner)` 在 owner 退树后不会再执行动作。自定义等待指令、组合器及业务资源应提供确定的释放路径；调度器会隔离清理异常和完成通知异常。

Task 桥接的取消与错误语义见 [Core 协程系统](/zh-CN/core/coroutine.md)。协程控制及节点操作在 Godot 主线程执行；Task 的 `await` 延续不因此自动获得引擎线程保证。

## 运行样例

[原生集成样例](https://github.com/GeWuYou/GFramework/tree/main/Samples/GodotIntegration) 提供真实节点退树、场景暂停、跨阶段流程和子任务清理的运行入口。托管阶段测试不替代原生引擎及导出态检查。

框架采用边界与独立性能测量见 [Core 协程系统](/zh-CN/core/coroutine.md) 和[协程教程](/zh-CN/tutorials/coroutine-tutorial.md)。

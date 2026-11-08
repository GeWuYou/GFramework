---
title: 协程系统
description: 使用引擎无关的协程调度器管理阶段、作用域、取消、结果与资源清理。
---

# 协程系统

`GFramework.Core.Coroutine` 使用 `IEnumerator<IYieldInstruction>` 表达跨帧流程。调度器在宿主驱动线程上推进协程，管理等待、优先级、父子任务和资源释放。它适合技能时序、刷怪、任务、过场和 UI 流程；CPU 密集计算和海量实体逐帧模拟应使用对应的计算或实体系统。

## 宿主接入

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

var scheduler = new CoroutineScheduler(timeSource);
var handle = scheduler.Run(PlaySequence(), tag: "intro");

// 在宿主循环中调用；timeSource 和可选 realtimeTimeSource 由调度器更新。
scheduler.Update();
```

`Run` 会执行至首个等待。没有等待而直接结束的协程也会返回可查询结果的句柄。控制 API、查询与阶段推进均在同一驱动线程使用；外部取消令牌可从其他线程请求取消，实际清理由驱动线程处理。

`initialCapacity` 控制初始存储容量，可为零。槽位会复用，旧句柄不能操作复用后的协程，也不能操作其他调度器的协程。优先级决定同次阶段推进的执行顺序，同优先级按进入队列的顺序执行；它不提供抢占或单步时间上限，业务代码必须及时 `yield`。

## 阶段与暂停

宿主需要显式声明并推进实际支持的阶段：

```csharp
var scheduler = new CoroutineScheduler(
    timeSource,
    supportedStages: new[]
    {
        CoroutineExecutionStage.Update,
        CoroutineExecutionStage.FixedUpdate,
        CoroutineExecutionStage.Deferred
    });

// 各自在对应宿主回调执行一次。增量以秒为单位。
scheduler.Update(CoroutineExecutionStage.Update, scaledDelta, realtimeDelta, isPaused);
scheduler.Update(CoroutineExecutionStage.FixedUpdate, fixedDelta, fixedRealtimeDelta, isPaused);
scheduler.Update(CoroutineExecutionStage.Deferred, scaledDelta, deferredRealtimeDelta, isPaused);
```

未指定 `supportedStages` 时，只支持构造参数 `executionStage` 对应的阶段，默认为 `Update`。显式增量入口不再自行更新时间源。

```csharp
IEnumerator<IYieldInstruction> PlaySequence()
{
    yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
    // 从下一次 FixedUpdate 推进开始在固定阶段执行。
    yield return new Delay(0.2);
    // 仍在 FixedUpdate。
    yield return new SwitchTo(CoroutineExecutionStage.Deferred);
    yield return new SwitchTo(CoroutineExecutionStage.Update);
}
```

切换后保持目标阶段，直到再次切换。切换到当前阶段也至少等待下一次该阶段推进。目标阶段未声明时流程明确失败。`Deferred` 是宿主定义的延迟执行阶段，不代表 GPU 渲染完成。

`isPaused` 冻结普通协程的全部阶段；启动时传入 `ignorePause: true` 的协程仍可推进。`Pause(handle)` 是单独的暂停请求，会向所属子任务生效；恢复父任务不会撤销子任务自己的暂停请求。取消和资源清理不依赖等待指令完成，也不会因场景暂停而永久搁置。

## 时间和条件等待

| 指令 | 含义 |
| --- | --- |
| `Delay`、`WaitForSecondsScaled` | 累计当前阶段的缩放时间 |
| `WaitForSecondsRealtime` | 累计当前阶段的真实时间增量 |
| `WaitOneFrame`、`WaitForNextFrame`、`WaitForFrames` | 等待当前阶段的下一次或若干次推进 |
| `WaitUntil`、`WaitWhile`、`WaitForPredicate` | 等待条件满足 |
| `WaitUntilOrTimeout` | 等待条件或超时，完成后检查 `ConditionMet` 与 `IsTimedOut` |
| `WaitForEvent<T>`、`WaitForEventWithTimeout<T>` | 等待事件，并在完成、取消或失败后注销监听 |

真实时间与暂停是独立选择：普通游戏协程暂停时不会恢复执行；需要暂停期间继续的倒计时应同时采用真实时间和 `ignorePause`。仅提供默认时间源时，真实等待退化为默认增量；需要真实计时的宿主必须提供独立真实增量或 `realtimeTimeSource`。

## 作用域与父子流程

`CoroutineScope` 是一组任务的生命周期边界。创建后策略固定：

| 策略 | 子任务失败时 | 适用场景 |
| --- | --- | --- |
| `FailFast` | 终止所属流程或作用域，并取消同级相关任务 | 技能、过场、关卡加载等一个业务操作 |
| `Supervisor` | 保留失败结果，其他独立任务继续 | 节点、场景、独立后台任务容器 |

`scheduler.CreateScope()` 默认 `FailFast`，调度器根作用域为 `Supervisor`。因此直接 `scheduler.Run(...)` 的独立任务互不牵连；每个流程内部的子任务默认使用 FailFast。

```csharp
using var flow = scheduler.CreateScope(CoroutineFailurePolicy.FailFast);
var handle = flow.Run(context => EnterBattle(context));

IEnumerator<IYieldInstruction> EnterBattle(CoroutineContext context)
{
    context.RunChild(SpawnEnemies());
    context.RunChild(PlayIntro());
    yield break;
}
```

即使父枚举器已经结束，父任务也会等待所属子任务收尾。父取消或失败向下取消后代；单独取消一个子任务不自动取消父任务。`childFailurePolicy` 可显式选择流程内部的失败策略。

`context.RunChild` 继承当前阶段、暂停策略及生命周期。需要独立任务时，显式从它真正所属的容器作用域启动；等待某个任务不会自动取得其所有权。`scope.Cancel()` 取消当前任务，作用域仍可使用；`Dispose()` 关闭作用域并取消其全部任务。

自定义枚举器包装器可实现 `ICoroutineCancellationSource` 并公开自己的 `CancellationToken`。调度器在所有启动入口执行首步前绑定该令牌，因此 `scope.Run`、`context.RunChild` 和工厂返回的枚举器都能在长等待或暂停期间响应取消。包装器负责令牌源的释放；组合多个包装器时需要传递或链接内层令牌。取消回调只提交请求，资源清理仍在调度线程完成。`Then` 和 `Sequence` 会绑定仍拥有的枚举器（包括尚未开始的后续项）的取消来源；任一来源取消会终止整个组合并跳过成功回调。某项自然完成后解除其绑定，避免之后的取消影响后续流程。

## 等待子任务与观察已有任务

```csharp
// 创建并等待一个受当前流程拥有的子任务。
yield return new WaitForCoroutine(LoadAssets());

// 观察已有任务，不取得其生命周期所有权。
var join = new JoinCoroutine(scheduler, existingHandle);
yield return join;
Console.WriteLine(join.Result.Status);

// 等待已有任务，并将其失败或取消传播到等待流程。
yield return new AwaitCoroutine(scheduler, existingHandle);
```

目标句柄须属于同一调度器。不存在或已淘汰的结果以 `Unknown` 呈现，`Await` 不会将未知结果视为成功。`Sequence`、`Then` 和上下文并行组合负责释放自己拥有的枚举器；并行流程通过 `scheduler.Run(context => context.ParallelCoroutines(...))` 启动。自定义组合器也需要用 `using` 或 `finally` 保证在正常结束、失败和取消时都释放内部资源。自等待和循环句柄依赖会被拒绝。

## Task 桥接

```csharp
var task = LoadDataAsync();
yield return task.AsCoroutineInstruction();
```

Task 失败会使等待流程失败，Task 取消会使等待流程取消，不会把二者报告为成功。泛型 `WaitForTask<T>.Result` 提供结果；提前读取会抛出 `InvalidOperationException`，避免阻塞驱动线程。

停止等待一个外部创建的 Task，只会停止协程等待。要让底层操作同时取消，应使用接收作用域令牌的任务工厂，或将 `CoroutineContext.CancellationToken` 传入底层 API。底层操作自身仍须遵守取消协议。

`AsyncOperation` 的等待器使用标准 Task 完成机制。Task 的 `await` 延续遵守 .NET 上下文规则；需要操作引擎对象时，调用方必须保证回到引擎线程，不能将 Task 完成等同于主线程恢复。

## 结果、诊断与资源释放

```csharp
var completion = scheduler.WaitForResultAsync(handle);
var result = await completion;
Console.WriteLine(result.Status);
Console.WriteLine(result.Exception);

if (scheduler.TryGetSnapshot(handle, out var snapshot))
{
    Console.WriteLine(snapshot.ExecutionStage);
    Console.WriteLine(snapshot.WaitingInstructionType);
    Console.WriteLine(snapshot.Parent);
}
```

最终状态为 `Completed`、`Cancelled`、`Faulted` 或未知结果 `Unknown`。`WaitForCompletionAsync` 仅返回状态，`WaitForResultAsync` 返回状态与异常。调度器保留最近 1024 个终结结果；历史淘汰后查询可能得到 `Unknown`，已取得的完成任务不受淘汰影响。`WhenAll` 在启动每个子任务时保留其完成结果，慢任务不会让先完成的同级任务因历史淘汰而变成未知状态。

父任务终结前等待后代清理完成；生命周期收尾统一处理资源释放、索引移除、等待者与完成任务通知。等待指令和枚举器的释放异常仍会保留，并继续处理其他清理；通知订阅者分别隔离，某个订阅者抛错不会阻止其他任务收尾。通知在驱动线程调用，不应执行长时间阻塞操作。

宿主停止时调用 `scheduler.Dispose()`，它先关闭接纳再取消任务。已持有的作用域不能绕过关闭状态重新启动任务；完成任务与保留结果仍可读取。`Clear()` 仅取消当前任务，不关闭调度器。

`enableStatistics: true` 启用统计。执行耗时与包含等待的存活时间分别统计，`LastUpdateElapsedMs` 记录最近一次推进耗时。`SlotCapacity` 和 `AllocatedSlotCount` 可用于观察存储峰值；快照及报告属于按需诊断入口，可能分配内存。

性能复核见[协程性能样例](https://github.com/GeWuYou/GFramework/tree/main/Samples/CoroutinePerformance)，原生宿主接入见 [Godot 协程](/zh-CN/godot/coroutine.md)。千级空业务调度结果不能替代目标设备、实际业务或渲染帧预算的测量。

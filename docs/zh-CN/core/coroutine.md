---
title: 协程系统
description: 协程系统提供基于 IEnumerator<IYieldInstruction> 的调度、等待和组合能力，可与事件、Task、命令与查询集成。
---

# 协程系统

## 概述

GFramework 的 Core 协程系统基于 `IEnumerator<IYieldInstruction>` 构建，通过 `CoroutineScheduler`
统一推进协程执行。它适合处理分帧逻辑、时间等待、条件等待、Task 桥接，以及事件驱动的异步流程。

协程系统主要由以下部分组成：

- `CoroutineScheduler`：负责运行、更新和控制协程
- `CoroutineHandle`：用于标识协程实例并控制其状态
- `IYieldInstruction`：定义等待行为的统一接口
- `Instructions`：内置等待指令集合
- `CoroutineHelper`：提供常用等待与生成器辅助方法
- `Extensions`：提供 Task、组合、命令、查询和 Mediator 场景下的扩展方法

## 核心概念

### CoroutineScheduler

`CoroutineScheduler` 是协程系统的核心调度器。构造时需要提供 `ITimeSource`，调度器会在每次 `Update()` 时读取时间增量并推进所有活跃协程。

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;

ITimeSource timeSource = /* 你的时间源实现 */;
var scheduler = new CoroutineScheduler(timeSource);

var handle = scheduler.Run(MyCoroutine());

// 在你的主循环中推进协程
scheduler.Update();
```

如果需要统计信息，可以启用构造函数的 `enableStatistics` 参数。

### CoroutineHandle

`CoroutineHandle` 用于引用具体协程，并配合调度器进行控制：

```csharp
var handle = scheduler.Run(MyCoroutine(), tag: "gameplay", group: "battle");

if (scheduler.IsCoroutineAlive(handle))
{
    scheduler.Pause(handle);
    scheduler.Resume(handle);
    scheduler.Kill(handle);
}
```

### IYieldInstruction

协程通过 `yield return IYieldInstruction` 表达等待逻辑：

```csharp
public interface IYieldInstruction
{
    bool IsDone { get; }
    void Update(double deltaTime);
}
```

## 基本用法

### 创建简单协程

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

public IEnumerator<IYieldInstruction> SimpleCoroutine()
{
    Console.WriteLine("开始");

    yield return new Delay(2.0);
    Console.WriteLine("2 秒后");

    yield return new WaitOneFrame();
    Console.WriteLine("下一帧");
}
```

### 使用 CoroutineHelper

`CoroutineHelper` 提供了一组常用等待和生成器辅助方法：

```csharp
using GFramework.Core.Coroutine;

public IEnumerator<IYieldInstruction> HelperCoroutine()
{
    yield return CoroutineHelper.WaitForSeconds(1.5);
    yield return CoroutineHelper.WaitForOneFrame();
    yield return CoroutineHelper.WaitForFrames(10);
    yield return CoroutineHelper.WaitUntil(() => isReady);
    yield return CoroutineHelper.WaitWhile(() => isLoading);
}
```

除了直接返回等待指令，`CoroutineHelper` 也可以直接生成可运行的协程枚举器：

```csharp
scheduler.Run(CoroutineHelper.DelayedCall(2.0, () => Console.WriteLine("延迟执行")));
scheduler.Run(CoroutineHelper.RepeatCall(1.0, 5, () => Console.WriteLine("重复执行")));

using var cts = new CancellationTokenSource();
scheduler.Run(CoroutineHelper.RepeatCallForever(1.0, () => Console.WriteLine("持续执行"), cts.Token));
```

### 控制协程状态

```csharp
var handle = scheduler.Run(LoadResources(), tag: "loading", group: "bootstrap");

scheduler.Pause(handle);
scheduler.Resume(handle);
scheduler.Kill(handle);

scheduler.KillByTag("loading");
scheduler.PauseGroup("bootstrap");
scheduler.ResumeGroup("bootstrap");
scheduler.KillGroup("bootstrap");

var cleared = scheduler.Clear();
```

## 常用等待指令

### 时间与帧

```csharp
yield return new Delay(1.0);
yield return new WaitForSecondsRealtime(1.0);
yield return new WaitOneFrame();
yield return new WaitForNextFrame();
yield return new WaitForFrames(5);
yield return new WaitForEndOfFrame();
yield return new WaitForFixedUpdate();
```

### 条件等待

```csharp
yield return new WaitUntil(() => health > 0);
yield return new WaitWhile(() => isLoading);
yield return new WaitForPredicate(() => hp >= maxHp);
yield return new WaitForPredicate(() => isBusy, waitForTrue: false);
yield return new WaitUntilOrTimeout(() => connected, timeoutSeconds: 5.0);
yield return new WaitForConditionChange(() => isPaused, waitForTransitionTo: true);
```

### Task 桥接

```csharp
using System.Threading.Tasks;
using GFramework.Core.Coroutine.Extensions;

Task loadTask = LoadDataAsync();
yield return loadTask.AsCoroutineInstruction();
```

也可以将 `Task` 转成协程枚举器后直接交给调度器：

```csharp
var coroutine = LoadDataAsync().ToCoroutineEnumerator();
var handle1 = scheduler.Run(coroutine);

var handle2 = scheduler.StartTaskAsCoroutine(LoadDataAsync());
```

- `AsCoroutineInstruction()` 适合已经处在某个协程内部，只需要在当前位置等待 `Task` 完成的场景。
- `ToCoroutineEnumerator()` 适合需要把 `Task` 先转换成 `IEnumerator<IYieldInstruction>`，再传给 `scheduler.Run(...)`、
  `Sequence(...)` 或其他只接受协程枚举器的 API。
- `StartTaskAsCoroutine()` 适合已经持有 `CoroutineScheduler`，并希望把 `Task` 直接作为一个顶层协程启动的场景。

### 等待事件

```csharp
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Coroutine.Instructions;

public IEnumerator<IYieldInstruction> WaitForEventExample(IEventBus eventBus)
{
    using var waitEvent = new WaitForEvent<PlayerDiedEvent>(eventBus);
    yield return waitEvent;

    var eventData = waitEvent.EventData;
    Console.WriteLine($"玩家 {eventData!.PlayerId} 死亡");
}
```

为事件等待附加超时：

```csharp
public IEnumerator<IYieldInstruction> WaitForEventWithTimeoutExample(IEventBus eventBus)
{
    using var waitEvent = new WaitForEvent<PlayerJoinedEvent>(eventBus);
    var timeoutWait = new WaitForEventWithTimeout<PlayerJoinedEvent>(waitEvent, 5.0f);

    yield return timeoutWait;

    if (timeoutWait.IsTimeout)
        Console.WriteLine("等待超时");
    else
        Console.WriteLine($"玩家加入: {timeoutWait.EventData!.PlayerName}");
}
```

等待两个事件中的任意一个：

```csharp
public IEnumerator<IYieldInstruction> WaitForEitherEvent(IEventBus eventBus)
{
    using var wait = new WaitForMultipleEvents<PlayerReadyEvent, PlayerQuitEvent>(eventBus);
    yield return wait;

    if (wait.TriggeredBy == 1)
        Console.WriteLine($"Ready: {wait.FirstEventData}");
    else
        Console.WriteLine($"Quit: {wait.SecondEventData}");
}
```

### 协程组合

等待子协程完成：

```csharp
public IEnumerator<IYieldInstruction> ParentCoroutine()
{
    Console.WriteLine("父协程开始");

    yield return new WaitForCoroutine(ChildCoroutine());

    Console.WriteLine("子协程完成");
}

private IEnumerator<IYieldInstruction> ChildCoroutine()
{
    yield return CoroutineHelper.WaitForSeconds(1.0);
    Console.WriteLine("子协程执行");
}
```

等待多个句柄全部完成：

```csharp
public IEnumerator<IYieldInstruction> WaitForMultipleCoroutines(CoroutineScheduler scheduler)
{
    var handles = new List<CoroutineHandle>
    {
        scheduler.Run(LoadTexture()),
        scheduler.Run(LoadAudio()),
        scheduler.Run(LoadModel())
    };

    yield return new WaitForAllCoroutines(scheduler, handles);

    Console.WriteLine("所有资源加载完成");
}
```

### 进度等待

```csharp
public IEnumerator<IYieldInstruction> LoadingWithProgress()
{
    yield return CoroutineHelper.WaitForProgress(
        duration: 3.0,
        onProgress: progress => Console.WriteLine($"加载进度: {progress * 100:F0}%"));
}
```

## 扩展方法

### 组合扩展

`CoroutineComposeExtensions` 提供链式顺序组合能力：

```csharp
using GFramework.Core.Coroutine.Extensions;

var chained =
    LoadConfig()
        .Then(() => Console.WriteLine("配置加载完成"))
        .Then(StartBattle());

scheduler.Run(chained);
```

### 协程生成扩展

`CoroutineExtensions` 提供了一些常用的协程生成器：

```csharp
using GFramework.Core.Coroutine.Extensions;

var delayed = CoroutineExtensions.ExecuteAfter(2.0, () => Console.WriteLine("延迟执行"));
var repeated = CoroutineExtensions.RepeatEvery(1.0, () => Console.WriteLine("tick"), count: 5);
var progress = CoroutineExtensions.WaitForSecondsWithProgress(3.0, p => Console.WriteLine(p));

scheduler.Run(delayed);
scheduler.Run(repeated);
scheduler.Run(progress);
```

顺序或并行组合多个协程：

```csharp
var sequence = CoroutineExtensions.Sequence(LoadConfig(), LoadScene(), StartBattle());
scheduler.Run(sequence);

var parallel = scheduler.ParallelCoroutines(LoadTexture(), LoadAudio(), LoadModel());
scheduler.Run(parallel);
```

### Task 扩展

`TaskCoroutineExtensions` 提供了三类扩展：

- `AsCoroutineInstruction()`：把 `Task` / `Task<T>` 包装成等待指令
- `ToCoroutineEnumerator()`：把 `Task` / `Task<T>` 转成协程枚举器
- `StartTaskAsCoroutine()`：直接通过调度器启动 Task 协程

### 命令、查询与 Mediator 扩展

这些扩展都定义在 `GFramework.Core.Coroutine.Extensions` 命名空间中。

### 命令协程

```csharp
using GFramework.Core.Coroutine.Extensions;

public IEnumerator<IYieldInstruction> ExecuteCommand(IContextAware contextAware)
{
    yield return contextAware.SendCommandCoroutineWithErrorHandler(
        new LoadSceneCommand(),
        ex => Console.WriteLine(ex.Message));
}
```

如果命令执行后需要等待事件：

```csharp
public IEnumerator<IYieldInstruction> ExecuteCommandAndWaitEvent(IContextAware contextAware)
{
    yield return contextAware.SendCommandAndWaitEventCoroutine<LoadSceneCommand, SceneLoadedEvent>(
        new LoadSceneCommand(),
        evt => Console.WriteLine($"场景加载完成: {evt.SceneName}"),
        timeout: 5.0f);
}
```

### 查询协程

`SendQueryCoroutine` 会同步执行查询，并通过回调返回结果：

```csharp
public IEnumerator<IYieldInstruction> QueryPlayer(IContextAware contextAware)
{
    yield return contextAware.SendQueryCoroutine<GetPlayerDataQuery, PlayerData>(
        new GetPlayerDataQuery { PlayerId = 1 },
        playerData => Console.WriteLine($"玩家名称: {playerData.Name}"));
}
```

### Mediator 协程

如果项目使用 `Mediator.IMediator`，还可以使用 `MediatorCoroutineExtensions`：

```csharp
public IEnumerator<IYieldInstruction> ExecuteMediatorCommand(IContextAware contextAware)
{
    yield return contextAware.SendCommandCoroutine(
        new SaveArchiveCommand(),
        ex => Console.WriteLine(ex.Message));
}
```

## 异常处理

调度器会在协程抛出未捕获异常时触发 `OnCoroutineException`：

```csharp
scheduler.OnCoroutineException += (handle, exception) =>
{
    Console.WriteLine($"协程 {handle} 异常: {exception.Message}");
};
```

如果协程等待的是 `Task`，也可以通过 `WaitForTask` / `WaitForTask<T>` 检查任务异常。

## 常见问题

### 协程什么时候执行？

协程在调度器的 `Update()` 中推进。调度器每次更新都会先更新 `ITimeSource`，再推进所有活跃协程。

### 协程是多线程的吗？

不是。协程本身仍由调用 `Update()` 的线程推进，通常用于主线程上的分帧流程控制。

### `Delay` 和 `CoroutineHelper.WaitForSeconds()` 有什么区别？

两者表达的是同一类等待语义。`CoroutineHelper.WaitForSeconds()` 只是 `Delay` 的辅助构造方法。

### 如何等待异步方法？

在现有协程里等待 `Task` 时，优先使用 `yield return task.AsCoroutineInstruction()`；如果要把 `Task` 单独交给调度器启动，使用
`scheduler.StartTaskAsCoroutine(task)`；如果中间还需要传给只接受协程枚举器的 API，则先调用 `task.ToCoroutineEnumerator()`。

## 相关文档

- [事件系统](/zh-CN/core/events)
- [CQRS](/zh-CN/core/cqrs)
- [协程系统教程](/zh-CN/tutorials/coroutine-tutorial)

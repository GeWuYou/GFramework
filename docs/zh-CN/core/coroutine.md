---
title: 协程系统
description: 协程系统提供了轻量级的异步操作管理机制，支持时间延迟、事件等待、任务等待等多种场景。
---

# 协程系统

## 概述

协程系统是 GFramework 中用于管理异步操作的核心机制。通过协程，你可以编写看起来像同步代码的异步逻辑，避免回调地狱，使代码更加清晰易读。

协程系统基于 C# 的迭代器（IEnumerator）实现，提供了丰富的等待指令（YieldInstruction），可以轻松处理时间延迟、事件等待、任务等待等各种异步场景。

**主要特性**：

- 轻量级协程调度器
- 丰富的等待指令（30+ 种）
- 支持协程嵌套和组合
- 协程标签和批量管理
- 与事件系统、命令系统、CQRS 深度集成
- 异常处理和错误恢复

## 核心概念

### 协程调度器

`CoroutineScheduler` 是协程系统的核心，负责管理和执行所有协程：

```csharp
using GFramework.Core.coroutine;

// 创建调度器（通常由架构自动管理）
var scheduler = new CoroutineScheduler(timeSource);

// 运行协程
var handle = scheduler.Run(MyCoroutine());

// 每帧更新
scheduler.Update();
```

### 协程句柄

`CoroutineHandle` 用于标识和控制协程：

```csharp
// 运行协程并获取句柄
var handle = scheduler.Run(MyCoroutine());

// 检查协程是否存活
if (scheduler.IsCoroutineAlive(handle))
{
    // 停止协程
    scheduler.Stop(handle);
}
```

### 等待指令

等待指令（YieldInstruction）定义了协程的等待行为：

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
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;

public IEnumerator<IYieldInstruction> SimpleCoroutine()
{
    Console.WriteLine("开始");

    // 等待 2 秒
    yield return new Delay(2.0);

    Console.WriteLine("2 秒后");

    // 等待 1 帧
    yield return new WaitOneFrame();

    Console.WriteLine("下一帧");
}
```

### 使用协程辅助方法

```csharp
using GFramework.Core.coroutine;

public IEnumerator<IYieldInstruction> HelperCoroutine()
{
    // 等待指定秒数
    yield return CoroutineHelper.WaitForSeconds(1.5);

    // 等待一帧
    yield return CoroutineHelper.WaitForOneFrame();

    // 等待多帧
    yield return CoroutineHelper.WaitForFrames(10);

    // 等待条件满足
    yield return CoroutineHelper.WaitUntil(() => isReady);

    // 等待条件不满足
    yield return CoroutineHelper.WaitWhile(() => isLoading);
}
```

### 在架构组件中使用

```csharp
using GFramework.Core.model;
using GFramework.Core.extensions;

public class PlayerModel : AbstractModel
{
    protected override void OnInit()
    {
        // 启动协程
        this.StartCoroutine(RegenerateHealth());
    }

    private IEnumerator<IYieldInstruction> RegenerateHealth()
    {
        while (true)
        {
            // 每秒恢复 1 点生命值
            yield return CoroutineHelper.WaitForSeconds(1.0);
            Health = Math.Min(Health + 1, MaxHealth);
        }
    }
}
```

## 高级用法

### 等待事件

```csharp
using GFramework.Core.coroutine.instructions;

public IEnumerator<IYieldInstruction> WaitForEventExample()
{
    Console.WriteLine("等待玩家死亡事件...");

    // 等待事件触发
    var waitEvent = new WaitForEvent<PlayerDiedEvent>(eventBus);
    yield return waitEvent;

    // 获取事件数据
    var eventData = waitEvent.EventData;
    Console.WriteLine($"玩家 {eventData.PlayerId} 死亡");
}
```

### 等待事件（带超时）

```csharp
public IEnumerator<IYieldInstruction> WaitForEventWithTimeout()
{
    var waitEvent = new WaitForEventWithTimeout<PlayerJoinedEvent>(
        eventBus,
        timeout: 5.0
    );

    yield return waitEvent;

    if (waitEvent.IsTimeout)
    {
        Console.WriteLine("等待超时");
    }
    else
    {
        Console.WriteLine($"玩家加入: {waitEvent.EventData.PlayerName}");
    }
}
```

### 等待 Task

```csharp
public IEnumerator<IYieldInstruction> WaitForTaskExample()
{
    // 创建异步任务
    var task = LoadDataAsync();

    // 在协程中等待 Task 完成
    var waitTask = new WaitForTask(task);
    yield return waitTask;

    // 检查异常
    if (waitTask.Exception != null)
    {
        Console.WriteLine($"任务失败: {waitTask.Exception.Message}");
    }
    else
    {
        Console.WriteLine("任务完成");
    }
}

private async Task LoadDataAsync()
{
    await Task.Delay(1000);
    // 加载数据...
}
```

### 等待多个协程

```csharp
public IEnumerator<IYieldInstruction> WaitForMultipleCoroutines()
{
    var coroutine1 = LoadTexture();
    var coroutine2 = LoadAudio();
    var coroutine3 = LoadModel();

    // 等待所有协程完成
    yield return new WaitForAllCoroutines(
        scheduler,
        coroutine1,
        coroutine2,
        coroutine3
    );

    Console.WriteLine("所有资源加载完成");
}
```

### 协程嵌套

```csharp
public IEnumerator<IYieldInstruction> ParentCoroutine()
{
    Console.WriteLine("父协程开始");

    // 等待子协程完成
    yield return new WaitForCoroutine(scheduler, ChildCoroutine());

    Console.WriteLine("子协程完成");
}

private IEnumerator<IYieldInstruction> ChildCoroutine()
{
    yield return CoroutineHelper.WaitForSeconds(1.0);
    Console.WriteLine("子协程执行");
}
```

### 带进度的等待

```csharp
public IEnumerator<IYieldInstruction> LoadingWithProgress()
{
    Console.WriteLine("开始加载...");

    yield return CoroutineHelper.WaitForProgress(
        duration: 3.0,
        onProgress: progress =>
        {
            Console.WriteLine($"加载进度: {progress * 100:F0}%");
        }
    );

    Console.WriteLine("加载完成");
}
```

### 协程标签管理

```csharp
// 使用标签运行协程
var handle1 = scheduler.Run(Coroutine1(), tag: "gameplay");
var handle2 = scheduler.Run(Coroutine2(), tag: "gameplay");
var handle3 = scheduler.Run(Coroutine3(), tag: "ui");

// 停止所有带特定标签的协程
scheduler.StopAllWithTag("gameplay");

// 获取标签下的所有协程
var gameplayCoroutines = scheduler.GetCoroutinesByTag("gameplay");
```

### 延迟调用和重复调用

```csharp
// 延迟 2 秒后执行
scheduler.Run(CoroutineHelper.DelayedCall(2.0, () =>
{
    Console.WriteLine("延迟执行");
}));

// 每隔 1 秒执行一次，共执行 5 次
scheduler.Run(CoroutineHelper.RepeatCall(1.0, 5, () =>
{
    Console.WriteLine("重复执行");
}));

// 无限重复，直到条件不满足
scheduler.Run(CoroutineHelper.RepeatCallWhile(1.0, () => isRunning, () =>
{
    Console.WriteLine("条件重复");
}));
```

### 与命令系统集成

```csharp
using GFramework.Core.coroutine.extensions;

public IEnumerator<IYieldInstruction> ExecuteCommandInCoroutine()
{
    // 在协程中执行命令
    var command = new LoadSceneCommand();
    yield return command.ExecuteAsCoroutine(this);

    Console.WriteLine("场景加载完成");
}
```

### 与 CQRS 集成

```csharp
public IEnumerator<IYieldInstruction> QueryInCoroutine()
{
    // 在协程中执行查询
    var query = new GetPlayerDataQuery { PlayerId = 1 };
    var waitQuery = query.SendAsCoroutine<GetPlayerDataQuery, PlayerData>(this);

    yield return waitQuery;

    var playerData = waitQuery.Result;
    Console.WriteLine($"玩家名称: {playerData.Name}");
}
```

## 最佳实践

1. **使用扩展方法启动协程**：通过架构组件的扩展方法启动协程更简洁
   ```csharp
   ✓ this.StartCoroutine(MyCoroutine());
   ✗ scheduler.Run(MyCoroutine());
   ```

2. **合理使用协程标签**：为相关协程添加标签，便于批量管理
   ```csharp
   this.StartCoroutine(BattleCoroutine(), tag: "battle");
   this.StartCoroutine(EffectCoroutine(), tag: "battle");

   // 战斗结束时停止所有战斗相关协程
   this.StopCoroutinesWithTag("battle");
   ```

3. **避免在协程中执行耗时操作**：协程在主线程执行，不要阻塞
   ```csharp
   ✗ public IEnumerator<IYieldInstruction> BadCoroutine()
   {
       Thread.Sleep(1000); // 阻塞主线程
       yield return null;
   }

   ✓ public IEnumerator<IYieldInstruction> GoodCoroutine()
   {
       yield return CoroutineHelper.WaitForSeconds(1.0); // 非阻塞
   }
   ```

4. **正确处理协程异常**：使用 try-catch 捕获异常
   ```csharp
   public IEnumerator<IYieldInstruction> SafeCoroutine()
   {
       var waitTask = new WaitForTask(riskyTask);
       yield return waitTask;

       if (waitTask.Exception != null)
       {
           // 处理异常
           Logger.Error($"任务失败: {waitTask.Exception.Message}");
       }
   }
   ```

5. **及时停止不需要的协程**：避免资源泄漏
   ```csharp
   private CoroutineHandle? _healthRegenHandle;

   public void StartHealthRegen()
   {
       _healthRegenHandle = this.StartCoroutine(RegenerateHealth());
   }

   public void StopHealthRegen()
   {
       if (_healthRegenHandle.HasValue)
       {
           this.StopCoroutine(_healthRegenHandle.Value);
           _healthRegenHandle = null;
       }
   }
   ```

6. **使用 WaitForEvent 时记得释放资源**：避免内存泄漏
   ```csharp
   public IEnumerator<IYieldInstruction> WaitEventExample()
   {
       using var waitEvent = new WaitForEvent<GameEvent>(eventBus);
       yield return waitEvent;
       // using 确保资源被释放
   }
   ```

## 常见问题

### 问题：协程什么时候执行？

**解答**：
协程在调度器的 `Update()` 方法中执行。在 GFramework 中，架构会自动在每帧调用调度器的更新方法。

### 问题：协程是多线程的吗？

**解答**：
不是。协程在主线程中执行，是单线程的。它们通过分帧执行来实现异步效果，不会阻塞主线程。

### 问题：如何在协程中等待异步方法？

**解答**：
使用 `WaitForTask` 等待 Task 完成：

```csharp
public IEnumerator<IYieldInstruction> WaitAsyncMethod()
{
    var task = SomeAsyncMethod();
    yield return new WaitForTask(task);
}
```

### 问题：协程可以返回值吗？

**解答**：
协程本身不能直接返回值，但可以通过闭包或类成员变量传递结果：

```csharp
private int _result;

public IEnumerator<IYieldInstruction> CoroutineWithResult()
{
    yield return CoroutineHelper.WaitForSeconds(1.0);
    _result = 42;
}

// 使用
this.StartCoroutine(CoroutineWithResult());
// 稍后访问 _result
```

### 问题：如何停止所有协程？

**解答**：
使用调度器的 `StopAll()` 方法：

```csharp
// 停止所有协程
scheduler.StopAll();

// 或通过扩展方法
this.StopAllCoroutines();
```

### 问题：协程中的异常会怎样？

**解答**：
协程中未捕获的异常会触发 `OnCoroutineException` 事件，并停止该协程：

```csharp
scheduler.OnCoroutineException += (handle, exception) =>
{
    Logger.Error($"协程异常: {exception.Message}");
};
```

### 问题：WaitForSeconds 和 Delay 有什么区别？

**解答**：
它们是相同的，`WaitForSeconds` 是辅助方法，内部创建 `Delay` 实例：

```csharp
// 两者等价
yield return CoroutineHelper.WaitForSeconds(1.0);
yield return new Delay(1.0);
```

## 相关文档

- [事件系统](/zh-CN/core/events) - 协程与事件系统集成
- [命令系统](/zh-CN/core/command) - 在协程中执行命令
- [CQRS](/zh-CN/core/cqrs) - 在协程中执行查询和命令
- [协程系统教程](/zh-CN/tutorials/coroutine-tutorial) - 分步教程

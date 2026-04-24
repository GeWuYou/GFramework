---
title: 使用协程系统
description: 学习如何在 GFramework 中创建调度器、运行协程，并结合时间、阶段、Task 与生命周期管理实现常见异步流程。
---

# 使用协程系统

## 学习目标

完成本教程后，你将能够：

- 创建并驱动 `CoroutineScheduler`
- 编写 `IEnumerator<IYieldInstruction>` 协程
- 区分缩放时间、真实时间与阶段等待
- 使用句柄、取消令牌和快照查询控制协程
- 在 Godot 中把协程绑定到节点生命周期

## 步骤 1：创建第一个协程

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

public sealed class TutorialLoop
{
    private readonly CoroutineScheduler _scheduler;

    public TutorialLoop(ITimeSource timeSource)
    {
        _scheduler = new CoroutineScheduler(timeSource);
    }

    public void Start()
    {
        _scheduler.Run(MyFirstCoroutine(), tag: "tutorial");
    }

    public void Tick()
    {
        _scheduler.Update();
    }

    private IEnumerator<IYieldInstruction> MyFirstCoroutine()
    {
        Console.WriteLine("协程开始");

        yield return new Delay(1.0);
        Console.WriteLine("1 秒后");

        yield return new WaitOneFrame();
        Console.WriteLine("下一帧");

        yield return new WaitForFrames(3);
        Console.WriteLine("3 帧后");
    }
}
```

关键点：

- 协程返回类型必须是 `IEnumerator<IYieldInstruction>`
- 调度器不会自动运行，你必须在宿主主循环中调用 `Update()`
- `Run(...)` 返回 `CoroutineHandle`，后续控制都依赖这个句柄

## 步骤 2：控制协程生命周期

```csharp
using var cts = new CancellationTokenSource();

var handle = _scheduler.Run(
    HealthRegenerationCoroutine(),
    tag: "regen",
    group: "player",
    cancellationToken: cts.Token);

_scheduler.Pause(handle);
_scheduler.Resume(handle);

// 外部取消会在下一次 Update 时生效
cts.Cancel();

var status = await _scheduler.WaitForCompletionAsync(handle);
Console.WriteLine(status);
```

如果你需要观察运行中状态：

```csharp
if (_scheduler.TryGetSnapshot(handle, out var snapshot))
{
    Console.WriteLine(snapshot.State);
    Console.WriteLine(snapshot.WaitingInstructionType);
}
```

## 步骤 3：区分时间等待

```csharp
private IEnumerator<IYieldInstruction> CooldownCoroutine()
{
    // 使用宿主默认时间
    yield return new Delay(2.0);

    // 使用真实时间，需要调度器提供 realtimeTimeSource
    yield return new WaitForSecondsRealtime(2.0);
}
```

建议：

- 普通游戏逻辑优先使用 `Delay`
- 暂停菜单、真实倒计时、网络超时等场景使用 `WaitForSecondsRealtime`

## 步骤 4：使用阶段等待

只有宿主为调度器提供了匹配阶段时，阶段等待才会真实生效：

```csharp
var fixedScheduler = new CoroutineScheduler(
    fixedTimeSource,
    executionStage: CoroutineExecutionStage.FixedUpdate);

private IEnumerator<IYieldInstruction> PhysicsCoroutine()
{
    yield return new WaitForFixedUpdate();
    Console.WriteLine("下一次固定步到达");
}
```

同理，`WaitForEndOfFrame` 需要运行在 `CoroutineExecutionStage.EndOfFrame` 的调度器上。

## 步骤 5：等待 Task

```csharp
using GFramework.Core.Coroutine.Extensions;

private IEnumerator<IYieldInstruction> LoadCoroutine()
{
    var task = LoadDataAsync();
    yield return task.AsCoroutineInstruction();
    Console.WriteLine("Task 已完成");
}
```

如果你已经持有调度器，也可以直接把 `Task` 作为顶层协程启动：

```csharp
var handle = _scheduler.StartTaskAsCoroutine(LoadDataAsync());
```

## 步骤 6：在 Godot 中绑定 Node 生命周期

```csharp
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

public partial class DemoNode : Node
{
    public override void _Ready()
    {
        // 推荐：节点作为所有者运行协程
        this.RunCoroutine(BlinkCoroutine(), Segment.ProcessIgnorePause, tag: "blink");
    }

    private IEnumerator<IYieldInstruction> BlinkCoroutine()
    {
        while (true)
        {
            Visible = !Visible;
            yield return new WaitForSecondsRealtime(0.5);
        }
    }
}
```

当 `DemoNode` 退出场景树时，上面的协程会被自动终止。

如果你需要绑定多个节点，可以继续使用：

```csharp
BlinkCoroutine()
    .CancelWith(this, anotherNode)
    .RunCoroutine();
```

## 下一步

- Core 侧更完整的 API 说明见 [Core 协程系统](/zh-CN/core/coroutine.md)
- Godot 集成细节见 [Godot 协程系统](/zh-CN/godot/coroutine.md)

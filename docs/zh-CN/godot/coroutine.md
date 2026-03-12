# Godot 协程系统

## 概述

GFramework 的协程系统由两层组成：

- `GFramework.Core.Coroutine` 提供通用调度器、`IYieldInstruction` 和一组等待指令。
- `GFramework.Godot.Coroutine` 提供 Godot 环境下的运行入口、分段调度以及节点生命周期辅助方法。

Godot 集成层的核心入口包括：

- `RunCoroutine(...)`
- `Timing.RunGameCoroutine(...)`
- `Timing.RunUiCoroutine(...)`
- `Timing.CallDelayed(...)`
- `CancelWith(...)`

协程本身使用 `IEnumerator<IYieldInstruction>`。

## 主要能力

- 在 Godot 中按不同更新阶段运行协程
- 等待时间、帧、条件、Task 和事件总线事件
- 显式将协程与一个或多个 `Node` 的生命周期绑定
- 通过 `CoroutineHandle` 暂停、恢复、终止协程
- 将命令、查询、发布操作直接包装为协程运行

## 基本用法

### 启动协程

```csharp
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

public partial class MyNode : Node
{
    public override void _Ready()
    {
        Demo().RunCoroutine();
    }

    private IEnumerator<IYieldInstruction> Demo()
    {
        GD.Print("开始执行");

        yield return new Delay(2.0);
        GD.Print("2 秒后继续执行");

        yield return new WaitForEndOfFrame();
        GD.Print("当前帧结束后继续执行");
    }
}
```

`RunCoroutine()` 默认在 `Segment.Process` 上运行，也就是普通帧更新阶段。

除了枚举器扩展方法，也可以直接使用 `Timing` 的静态入口：

```csharp
Timing.RunCoroutine(Demo());
Timing.RunGameCoroutine(GameLoop());
Timing.RunUiCoroutine(MenuAnimation());
```

### 显式绑定节点生命周期

可以使用 `CancelWith(...)` 将协程与一个或多个节点的生命周期关联。

```csharp
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

public partial class MyNode : Node
{
    public override void _Ready()
    {
        LongRunningTask()
            .CancelWith(this)
            .RunCoroutine();
    }

    private IEnumerator<IYieldInstruction> LongRunningTask()
    {
        while (true)
        {
            GD.Print("tick");
            yield return new Delay(1.0);
        }
    }
}
```

`CancelWith` 目前有三种重载：

- `CancelWith(Node node)`
- `CancelWith(Node node1, Node node2)`
- `CancelWith(params Node[] nodes)`

`CancelWith(...)` 内部通过 `Timing.IsNodeAlive(...)` 判断节点是否仍然有效。只要任一被监视的节点出现以下任一情况，包装后的协程就会停止继续枚举：

- 节点引用为 `null`
- Godot 实例已经失效或已被释放
- 节点已进入 `queue_free` / `IsQueuedForDeletion()`
- 节点已退出场景树，`IsInsideTree()` 返回 `false`

这意味着协程不只会在节点真正释放时停止；节点一旦退出场景树，下一次推进时也会停止。

## Segment 分段

Godot 层通过 `Segment` 决定协程挂在哪个调度器上：

```csharp
public enum Segment
{
    Process,
    ProcessIgnorePause,
    PhysicsProcess,
    DeferredProcess
}
```

- `Process`：普通 `_Process` 段，场景树暂停时不会推进。
- `ProcessIgnorePause`：同样使用 process delta，但即使场景树暂停也会推进。
- `PhysicsProcess`：在 `_PhysicsProcess` 段推进。
- `DeferredProcess`：通过 `CallDeferred` 在当前帧之后推进，场景树暂停时不会推进。

示例：

```csharp
UiAnimation().RunCoroutine(Segment.ProcessIgnorePause);
PhysicsRoutine().RunCoroutine(Segment.PhysicsProcess);
```

如果你更偏向语义化入口，也可以直接使用：

```csharp
Timing.RunGameCoroutine(GameLoop());
Timing.RunUiCoroutine(MenuAnimation());
```

### 延迟调用

`Timing` 还提供了两个延迟调用快捷方法：

```csharp
Timing.CallDelayed(1.0, () => GD.Print("1 秒后执行"));
Timing.CallDelayed(1.0, () => GD.Print("节点仍然有效时执行"), this);
```

第二个重载会在执行前检查传入节点是否仍然存活。

## 常用等待指令

以下类型可直接用于 `yield return`：

### 时间与帧

```csharp
yield return new Delay(1.0);
yield return new WaitForSecondsRealtime(1.0);
yield return new WaitOneFrame();
yield return new WaitForNextFrame();
yield return new WaitForFrames(5);
yield return new WaitForEndOfFrame();
```

说明：

- `Delay` 是最直接的秒级等待。
- `WaitForSecondsRealtime` 常用于需要独立计时语义的协程场景。
- `WaitOneFrame`、`WaitForNextFrame`、`WaitForEndOfFrame` 用于帧级调度控制。

### 条件等待

```csharp
yield return new WaitUntil(() => health > 0);
yield return new WaitWhile(() => isLoading);
```

### Task 等待

```csharp
using System.Threading.Tasks;
using GFramework.Core.Coroutine.Extensions;

Task loadTask = LoadSomethingAsync();
yield return loadTask.AsCoroutineInstruction();
```

也可以先把 `Task` 转成协程枚举器，再直接运行：

```csharp
LoadSomethingAsync()
    .ToCoroutineEnumerator()
    .RunCoroutine();
```

- 已经在一个协程内部时，优先使用 `yield return task.AsCoroutineInstruction()`，这样可以直接把 `Task` 嵌入当前协程流程。
- 如果要把一个现成的 `Task` 当作独立协程入口交给 Godot 协程系统运行，再使用
  `task.ToCoroutineEnumerator().RunCoroutine()`。

### 等待事件总线事件

可以通过事件总线等待业务事件：

```csharp
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Coroutine.Instructions;

private IEnumerator<IYieldInstruction> WaitForGameEvent(IEventBus eventBus)
{
    using var wait = new WaitForEvent<PlayerSpawnedEvent>(eventBus);
    yield return wait;

    var evt = wait.EventData;
}
```

如需为事件等待附加超时控制，可结合 `WaitForEventWithTimeout<TEvent>`。

## 协程控制

协程启动后会返回 `CoroutineHandle`，可用于控制运行状态：

```csharp
var handle = Demo().RunCoroutine(tag: "demo");

Timing.PauseCoroutine(handle);
Timing.ResumeCoroutine(handle);
Timing.KillCoroutine(handle);

Timing.KillCoroutines("demo");
Timing.KillAllCoroutines();
```

如果希望在场景初始化阶段主动确保调度器存在，也可以调用：

```csharp
Timing.Prewarm();
```

## 与 IContextAware 集成

`GFramework.Godot.Coroutine` 还提供了一组扩展方法，用于把命令、查询和通知直接包装成协程：

- `RunCommandCoroutine(...)`
- `RunCommandCoroutine<TResponse>(...)`
- `RunQueryCoroutine<TResponse>(...)`
- `RunPublishCoroutine(...)`

这些方法会把异步操作转换为协程，并交给 `RunCoroutine(...)` 调度执行。

例如：

```csharp
public void StartCoroutines(IContextAware contextAware)
{
    contextAware.RunCommandCoroutine(
        new EnterBattleCommand(),
        Segment.Process,
        tag: "battle");

    contextAware.RunQueryCoroutine(
        new LoadPlayerQuery(),
        Segment.ProcessIgnorePause,
        tag: "ui");
}
```

这些扩展适合在 Godot 节点或控制器中直接启动和跟踪业务协程。

## 相关文档

- [Godot 概述](./index.md)
- [Godot 扩展方法](./extensions.md)
- [信号扩展](./signal.md)
- [事件系统](../core/events.md)

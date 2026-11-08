---
title: 使用协程系统
description: 从可编译样例学习宿主推进、父子流程、阶段切换、结果观察和 Godot 节点归属。
---

# 使用协程系统

本教程使用[协程性能样例中的接入示例](https://github.com/GeWuYou/GFramework/blob/main/Samples/CoroutinePerformance/CoroutineExamples.cs)。示例与 Core 一起编译，性能测量只运行独立的测试工作负载。

## 接入宿主循环

协程返回 `IEnumerator<IYieldInstruction>`。`Run` 执行至首次等待，后续由宿主持续推进。下面的宿主声明了普通、固定和延迟三个阶段：

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Host{csharp}

宿主应在各自对应的回调中调用 `Tick`，传入该阶段的缩放与真实增量。没有固定或延迟阶段的宿主，应只声明实际支持的阶段；不要声明后不再推进。

## 编排一个游戏流程

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Flow{csharp}

`CoroutineContext.RunChild` 建立父子所有权。父枚举器结束后，父任务等待子任务收尾；父取消会取消全部后代。一个流程默认 FailFast，子步骤失败会使流程失败并取消兄弟步骤。

`SwitchTo` 在目标阶段下一次推进时恢复，并保持该阶段。上例 `ApplyDamage` 的 `WaitOneFrame` 等待的是下一次固定步，不会自动回到普通更新。需要回到普通更新时，显式切换到 `CoroutineExecutionStage.Update`。

## 区分独立任务和一个操作

调度器根作用域为 Supervisor，直接 `scheduler.Run(...)` 启动的独立流程互不牵连。一个关卡加载操作可以创建默认 FailFast 作用域：

```csharp
using var loading = scheduler.CreateScope();
loading.Run(LoadMap());
loading.Run(LoadCharacters());
```

作用域必须由实际业务生命周期持有，不能在启动任务后立刻离开 `using`。释放它会关闭作用域并取消所有所属任务。对于互相独立的任务集合，创建时显式指定 `CoroutineFailurePolicy.Supervisor`。

仅观察已有任务时使用 `JoinCoroutine`；需要传播失败或取消时使用 `AwaitCoroutine`。观察者取消不自动取消被观察任务。完整区别见 [Core 协程系统](/zh-CN/core/coroutine.md)。

## 取消和观察结果

<<< ../../../Samples/CoroutinePerformance/CoroutineExamples.cs#Observe{csharp}

控制 API 在驱动线程使用。外部线程可以取消传入的 `CancellationToken`，下一次宿主推进负责清理；即使游戏暂停，也应继续调用阶段推进，使取消能够处理。

`WaitForResultAsync` 返回状态和异常。需要操作引擎对象时，仍需遵守 Task 的上下文规则；上例只输出结果，不访问引擎对象。

## 接入 Godot 节点

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
        this.RunCoroutine(Blink(), Segment.ProcessIgnorePause);
    }

    private IEnumerator<IYieldInstruction> Blink()
    {
        while (true)
        {
            GD.Print("tick");
            yield return new WaitForSecondsRealtime(0.5);
        }
    }
}
```

节点作为所有者时，退出场景树会取消其任务及后代。节点容器默认 Supervisor，不会因为一个独立闪烁任务失败而停止全部其他节点任务。`ProcessIgnorePause` 忽略场景暂停，真实等待忽略时间缩放；两者分别控制是否继续执行与如何计时。

Godot 会驱动各阶段，不需要再自行调用 Core `Update`。原生运行工程与作用域示例见 [Godot 协程系统](/zh-CN/godot/coroutine.md)。

## 性能复核

从仓库根目录运行：

```bash
dotnet run --project Samples/CoroutinePerformance/CoroutinePerformance.csproj -c Release
```

样例输出三次重复的 JSON 结果，覆盖 100、1000、10000 活跃协程和累计 100000 次创建销毁。它测量调度开销，不包含游戏业务、渲染或目标设备差异；采用边界见 [Core 协程系统](/zh-CN/core/coroutine.md)。

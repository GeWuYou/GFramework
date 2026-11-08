---
title: Arch ECS 接入
description: 直接构造或通过标准 DI 驱动原生 Arch 系统，读取统计并正确关闭。
---

# Arch ECS 接入

## 直接构造

安装 `GeWuYou.GFramework.Ecs.Arch`。World 和系统由调用方创建时，先停止 runtime，再释放系统，最后释放 World。runtime 不接管这些对象的最终释放责任。

下列代码来自可运行的 ArchRuntimeDemo，使用普通 Arch 原生系统：

<<< ../../../Samples/ArchRuntimeDemo/Program.cs#native-runtime

```bash
dotnet run --project Samples/ArchRuntimeDemo -c Release
dotnet run --project Samples/ArchRuntimeDemo -c Release -- --benchmark
```

第二条命令测量本机空业务的统计开关成本；不代表真实游戏或目标设备性能。

## 标准 DI 与可选编排

```csharp
using Arch.System;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;
using GFramework.Ecs.Arch.Systems;
using Microsoft.Extensions.DependencyInjection;

// 放入 Architecture.ConfigureServices，使用同一个正式 provider。
services.AddArch(options =>
{
    options.EnableStatistics = true;
    options.Priority = 50;
});
services.AddSingleton<ISystem<float>, MovementSystem>();

// 宿主在启动完成后取得 runtime，业务循环直接注入 IArchRuntime。
var runtime = architecture.Services.GetRequiredService<IArchRuntime>();
runtime.Update(deltaTime);
var snapshot = runtime.GetStatistics();
```

`ArchEcsModule` 只配置服务，`IArchRuntime` 管理运行状态。模块为同一 provider 登记 World、runtime 和生命周期参与者；不在登记阶段创建 World。类型／工厂登记的系统和 World 由 provider 最终释放，显式传入的实例仍归调用方。

`ArchOptions` 位于契约包，只有一份类型。`WorldCapacity` 表示 World 初始 archetype 容量，`Priority` 表示 Architecture 中的启动顺序；统计策略在配置／构造时固定。

## 系统、阶段与失败

直接采用 `ISystem<float>`、`BaseSystem<World, float>` 或 `Group<float>`。系统实现 `IPrioritized` 时按 Priority 升序排列，同值保持登记顺序。每帧先执行所有系统的 BeforeUpdate，再执行全部 Update，最后执行全部 AfterUpdate。

`ArchSystemAdapter<T>` 是可选便利基类，提供 `OnArchInitialize`、`OnArchStopped` 和 `OnArchDispose`。业务停止和最终资源释放分开；它的原生与框架初始化入口共享一次初始化逻辑。

初始化失败仍保留已尝试系统的停止责任。任一更新阶段抛错立即终止本帧，原异常传播，runtime 进入失败状态，不再接受更新或重启。调用 Stop 后逆序执行顶层系统可选的 `IDestroyable.Destroy`；单个错误不跳过其他系统，最终聚合报告。重复 Stop 不重复回调，并保持同一失败结果。

原生 Group 是一个顶层系统和内部资源拥有者。它的内部系统不能同时作为其他 DI 拥有注册；Group 内部若有独立业务停止需求，由该 Group 的拥有者显式组织，runtime 不反射遍历内部对象。

同一 runtime 在初始化所在线程更新、读取统计和停止，禁止回调重入。宿主关闭时先停止接收帧工作，保持拥有线程和依赖存活，完成 Stop 后再释放系统与 World。

## 统计快照

`EnableStatistics` 默认关闭，此时不读取计时器，`GetStatistics()` 返回 null。开启后在宿主线程调用该方法，获得可跨线程分享的不可变副本：

- 实体数、顶层系统数、完成／失败帧数。
- 最近尝试帧的总耗时，以及每个顶层系统的三阶段耗时。
- 未执行阶段耗时为 null；失败阶段保留已耗时间和 FailedPhase。
- Group 只记录整体耗时，不单独检测其内部系统。

快照按需分配，帧推进复用内部测量存储；统计启用会增加计时成本。停止后的快照保留停止时的数据，不再访问已释放的 World。

## Godot 宿主

GodotIntegration 在物理帧中驱动 runtime，并显式遵守场景树暂停。其原生自检覆盖帧推进、完整阶段、统计和关闭时的一次释放；Godot 原生主循环仍由引擎拥有。

<<< ../../../Samples/GodotIntegration/DemoRoot.Ecs.cs

Godot 等具有线程约束的宿主应显式提供 `AddArch(..., hostContext: SynchronizationContext.Current)`，并用异步 Architecture 启动／停止。该路径把 World、系统和 runtime 的激活、初始化及业务停止派发回拥有上下文，避免前置异步组件导致线程切换；普通同步宿主省略此参数。最终资源释放仍由对象拥有者负责，持有原生对象的系统应在自己的释放边界进行宿主调度。

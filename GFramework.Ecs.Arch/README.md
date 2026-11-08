# GFramework.Ecs.Arch

可选的 Arch ECS 运行时与标准 DI／Architecture 接入。直接接受原生 `ISystem<float>`，支持 `BaseSystem<World, float>` 和 `Group<float>`；不要求框架基类。

## 包与入口

- `ArchRuntime`：直接构造的宿主线程运行时，负责初始化、三阶段帧更新、业务停止和按需统计。
- `ArchEcsModule`／`Extensions.AddArch`：只组织服务和可选生命周期编排。
- `ArchSystemAdapter<T>`：可选基类，区分业务停止与最终资源释放。
- `Components`／`Systems`：最小组件与系统示例。
- `Ecs.Arch.Abstractions`：`IArchRuntime`、唯一的 `ArchOptions` 及不可变快照。Core 提供可选编排；Arch 提供实体与原生系统。

## 最小采用

```csharp
services.AddArch(options => options.EnableStatistics = true);
services.AddSingleton<Arch.System.ISystem<float>, MovementSystem>();
```

这些登记放在 Architecture 的组合根，使用 `GFramework.Ecs.Arch.Extensions` 与标准 DI。启动后宿主注入 `IArchRuntime` 并调用 `Update(deltaTime)`，不再通过配置模块更新。普通宿主可以直接构造 `new ArchRuntime(world, systems, options)` 并显式 Initialize／Stop。

系统按 `IPrioritized` 升序、同值按登记顺序运行。每帧完成全部 BeforeUpdate，再完成全部 Update 和 AfterUpdate。更新失败会封闭运行时，调用方负责停止；部分初始化失败也参与逆序业务清理。

## 所有权与统计

runtime 借用系统和 World。DI 创建的对象由 provider 最终释放，直接构造对象由调用方释放；先 Stop，再释放系统，最后释放 World。Group 自己拥有其内部系统，不能重复登记这些内部对象的最终释放责任。

统计默认关闭。启用后在宿主拥有线程读取 `GetStatistics()`，得到实体数、系统数、完成／失败帧和顶层系统阶段耗时的不可变快照。失败阶段保留耗时，未执行阶段为 null。快照可跨线程分享，runtime 本身不允许跨线程调用或回调重入。

## 文档与运行工程

- [ECS 概览](../docs/zh-CN/ecs/index.md)
- [Arch ECS 接入](../docs/zh-CN/ecs/arch.md)
- [原生 Arch 示例](../Samples/ArchRuntimeDemo/README.md)
- [Godot 集成工程](../Samples/GodotIntegration/README.md)

Godot 等具有线程约束的宿主应显式提供 `AddArch(..., hostContext: SynchronizationContext.Current)`，并用异步 Architecture 启动／停止。该路径把 World、系统和 runtime 的激活、初始化及业务停止派发回拥有上下文，避免前置异步组件导致线程切换；普通同步宿主省略此参数。最终资源释放仍由对象拥有者负责，持有原生对象的系统应在自己的释放边界进行宿主调度。

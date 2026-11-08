# GFramework.Ecs.Arch

`GFramework.Ecs.Arch` 将 Arch ECS 的 World 与系统接入标准 DI 和可选 Architecture 生命周期。宿主负责主循环，`IArchEcsModule.Update(deltaTime)` 驱动已启动的系统。

## 包关系与目录

- Core 提供可选的组件启动与停止编排。
- `GFramework.Ecs.Arch.Abstractions` 提供模块与系统适配契约。
- 本包提供 `ArchEcsModule`、`ArchSystemAdapter<T>`、`ArchOptions` 与 `AddArch` 注册入口。
- `Extensions` 提供装配入口；`Components`、`Systems` 提供最小数据与系统示例。

## 最小接入

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

在组合根配置 World、模块与系统：

```csharp
using System;
using System.Threading.Tasks;
using Arch.Core;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;
using GFramework.Ecs.Arch.Systems;
using Microsoft.Extensions.DependencyInjection;

await using var architecture = new GameArchitecture();
await architecture.InitializeAsync();
var world = architecture.Services.GetRequiredService<World>();
var ecs = architecture.Services.GetRequiredService<IArchEcsModule>();
// The owning host calls ecs.Update(deltaTime) from its frame loop.

public sealed class GameArchitecture : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddArch(options =>
        {
            options.WorldCapacity = 2048;
            options.Priority = 50;
        });
        services.AddSingleton<ArchSystemAdapter<float>, MovementSystem>();
    }
}
```

`AddArch` 只描述当前组合根，不立即创建 World。系统通过构造函数接收同一个 provider 拥有的 World；业务代码不需要取得容器或框架上下文。普通宿主可保留已解析的模块引用并驱动更新。

## 编写系统

```csharp
using Arch.Core;
using GFramework.Ecs.Arch;
using GFramework.Ecs.Arch.Components;

public sealed class PlayerMovementSystem(World world) : ArchSystemAdapter<float>(world)
{
    private QueryDescription _query;

    protected override void OnArchInitialize()
    {
        _query = new QueryDescription().WithAll<Position, Velocity>();
    }

    protected override void OnUpdate(in float deltaTime)
    {
        var step = deltaTime;
        World.Query(in _query, (ref Position position, ref Velocity velocity) =>
        {
            position.X += velocity.X * step;
            position.Y += velocity.Y * step;
        });
    }
}
```

将该系统注册为 `ArchSystemAdapter<float>` 服务即可由模块驱动。系统实现 `IPrioritized` 时按其 Priority 升序启动；相同值保持注册顺序。模块自身的 `ArchOptions.Priority` 决定它在 Architecture 组件中的启动顺序。

## 停止与所有权

初始化中途失败的系统也会参与业务停止。模块逆序停止已尝试启动的系统，继续收集停止错误；World 与通过类型/工厂创建的系统最终由同一 provider 释放。停止后模块不再更新系统。

系统业务停止使用 `OnArchStopped`，资源清理使用 `OnArchDispose`。两者分别由生命周期编排和对象拥有者调用，不在业务停止中重复释放 DI 拥有的 World 或依赖。

不要同时将同一系统登记为独立 `AddComponent` 参与者；它已由 ECS 模块管理业务生命周期。不要手动销毁 provider 拥有的 World，也不要使用多个 owning 工厂为同一可释放系统建立别名。显式传入的外部实例保留调用者的最终资源责任。

## 相关文档

- [ECS 入门](../docs/zh-CN/ecs/index.md)
- [Arch ECS 采用指南](../docs/zh-CN/ecs/arch.md)
- [ECS 契约](../docs/zh-CN/abstractions/ecs-arch-abstractions.md)

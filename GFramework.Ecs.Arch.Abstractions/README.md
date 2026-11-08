# GFramework.Ecs.Arch.Abstractions

宿主可只依赖 ECS 运行契约、配置和不可变统计类型，而无需引用 Arch 原生实现。此包依赖 Core 契约，提供可选适配器的业务生命周期约定。

## 契约与目录

本目录公开 `IArchRuntime`、`ArchOptions`、`ArchStatisticsSnapshot`、`ArchSystemStatistics` 和 `ArchUpdatePhase`。`IArchSystemAdapter<T>` 是可选适配基类使用的生命周期组合，不是原生系统的接入要求。

`IArchRuntime` 公开 Initialize、Update、Stop 和 GetStatistics。它不提供服务登记；组合根的配置契约使用标准 `IArchitectureModule`，实现位于运行时包。

## 最小采用

```csharp
public sealed class GameLoop(IArchRuntime runtime)
{
    public void Tick(float deltaTime) => runtime.Update(deltaTime);
}
```

使用 `GFramework.Ecs.Arch.Abstractions` 命名空间。宿主在拥有线程启动、更新、取快照和停止，保持 World 与系统存活直到停止完成。快照不可变，可以传给其他线程展示；关闭统计时返回 null。

需要 World、原生系统、`AddArch` 或 `ArchSystemAdapter<T>` 时引用运行时包。完整接入见 [Arch ECS 指南](../docs/zh-CN/ecs/arch.md) 和 [ECS 契约说明](../docs/zh-CN/abstractions/ecs-arch-abstractions.md)。

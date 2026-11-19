# Ecs.Arch.Abstractions

[简体中文](#简体中文) · [English](#english)

## 简体中文

共享 Arch 运行时、系统适配和统计契约。可在测试中为这些接口提供替身。

`GFramework.Ecs.Arch.Abstractions` 依赖 `GFramework.Core.Abstractions` 的生命周期契约。用它在共享工程中声明 `IArchRuntime`，再由宿主安装 `GFramework.Ecs.Arch` 并注册实际运行时。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/abstractions/ecs-arch-abstractions.html)

## English

Share Arch runtime, system adapter, and statistics contracts. Supply test doubles for these interfaces in tests.

`GFramework.Ecs.Arch.Abstractions` builds on lifecycle contracts from `GFramework.Core.Abstractions`. Use it to declare `IArchRuntime` in a shared project, then install `GFramework.Ecs.Arch` and register the runtime in the host.

[Read the guide](https://gewuyou.github.io/GFramework/abstractions/ecs-arch-abstractions.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch.Abstractions
```

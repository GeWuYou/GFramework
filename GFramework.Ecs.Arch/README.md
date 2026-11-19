# Ecs.Arch

[简体中文](#简体中文) · [English](#english)

## 简体中文

装配 Arch World、原生系统和可选运行时统计。直接构造或调用 AddArch，并从应用宿主驱动 Initialize、Update 和 Stop。

`GFramework.Ecs.Arch` 会安装 `GFramework.Core`、`GFramework.Ecs.Arch.Abstractions` 和 Arch 依赖。只需向宿主声明 `IArchRuntime` 等接口的项目可直接引用 `GFramework.Ecs.Arch.Abstractions`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/ecs/arch.html)

## English

Compose an Arch World, native systems, and optional runtime statistics. Construct the runtime directly or use AddArch, then drive Initialize, Update, and Stop from the host.

`GFramework.Ecs.Arch` brings in `GFramework.Core`, `GFramework.Ecs.Arch.Abstractions`, and the Arch dependencies. Reference `GFramework.Ecs.Arch.Abstractions` directly in projects that only need host interfaces such as `IArchRuntime`.

[Read the guide](https://gewuyou.github.io/GFramework/ecs/arch.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Ecs.Arch
```

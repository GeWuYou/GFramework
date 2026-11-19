# Core.Abstractions

[简体中文](#简体中文) · [English](#english)

## 简体中文

向共享库提供生命周期、事件、状态和资源接口。

共享项目可直接引用 `GFramework.Core.Abstractions`，通过 `IInitializable`、`IEventBus` 或 `IResourceLoader<T>` 声明依赖。应用宿主安装 `GFramework.Core` 取得这些接口的默认实现或装配入口。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/abstractions/core-abstractions.html)

## English

Lifecycle, event, state, and resource interfaces for shared libraries.

Reference `GFramework.Core.Abstractions` from a shared project to declare dependencies such as `IInitializable`, `IEventBus`, or `IResourceLoader<T>`. Install `GFramework.Core` in the application host for default implementations and composition.

[Read the guide](https://gewuyou.github.io/GFramework/abstractions/core-abstractions.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

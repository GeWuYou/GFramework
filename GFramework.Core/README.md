# Core

[简体中文](#简体中文) · [English](#english)

## 简体中文

标准 DI 上的生命周期、同步事件、状态、资源、对象池与协程。安装 Core 后，可在现有组合根逐项采用。需要协调启停时，使用 Architecture 与 AddComponent。

`GFramework.Core` 会安装提供生命周期、事件和资源接口的 `GFramework.Core.Abstractions`。只需要共享接口的项目可以直接引用契约包。需要枚举扩展或优先级生成时，在声明对应类型的项目中另装 `GFramework.Core.SourceGenerators`。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/core/)

## English

Lifecycle coordination, synchronous events, state, resources, pools, and coroutines on standard DI. Install Core and adopt the services you need in an existing composition root. Use Architecture and AddComponent to coordinate startup and shutdown.

`GFramework.Core` brings in `GFramework.Core.Abstractions` for lifecycle, event, and resource contracts. Reference the contract package directly from a project that only shares interfaces. For enum extensions or generated priorities, add `GFramework.Core.SourceGenerators` to the project that declares the types.

[Read the guide](https://gewuyou.github.io/GFramework/core/)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Core
```

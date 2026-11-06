---
title: 快速开始
description: 通过只依赖 Core 的最小示例快速跑通 GFramework 基础架构。
---

# 快速开始

本页给出一个只依赖 `Core` 的最小路径，用来确认你已经成功接入 `Architecture`、`Model`、`System` 与事件系统。

## 1. 安装最小依赖

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

## 2. 定义一个最小架构

```csharp
using GFramework.Core.Architectures;

public sealed class CounterArchitecture : Architecture
{
    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        RegisterModel(new CounterModel());
        RegisterSystem(new CounterSystem());
    }
}
```

这里要点只有两个：

- 架构入口是 `Architecture`
- 当前版本使用 `protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)` 注册模型、系统和工具

## 3. 定义一个模型

```csharp
using GFramework.Core.Model;
using GFramework.Core.Property;

public sealed class CounterModel : AbstractModel
{
    public BindableProperty<int> Count { get; } = new(0);

    protected override void OnInit()
    {
    }
}
```

`BindableProperty<T>` 适合承载可观察状态；如果你只需要一个最小例子，保持 `OnInit()` 为空即可。

## 4. 定义一个系统

```csharp
using GFramework.Core.Systems;

public sealed class CounterSystem : AbstractSystem
{
    protected override void OnInit()
    {
    }
}
```

## 5. 初始化并验证上下文访问

```csharp
var architecture = new CounterArchitecture();
architecture.Initialize();

var model = architecture.Context.GetModel<CounterModel>();
var system = architecture.Context.GetSystem<CounterSystem>();

Console.WriteLine(model.Count.Value); // 0
```

架构会在启动时向组件注入上下文，Model / System 内部可以用 `GetModel<T>()`、`GetSystem<T>()` 等扩展访问彼此；组合根则直接通过 `architecture.Context` 访问。

如果你能走通这一步，说明以下链路已经成立：

- 架构初始化
- 模型 / 系统注册
- 上下文访问

## 下一步

- request / notification / stream 消息模型与接入：看 [CQRS 运行时](../core/cqrs.md)
- 游戏层能力与接入顺序：看 [Game 模块总览](../game/index.md)
- 包目录与最小安装组合：看 [安装配置](./installation.md)

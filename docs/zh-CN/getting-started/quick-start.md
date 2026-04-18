# 快速开始

本页给出一个只依赖 `Core` 的最小路径，用来确认你已经成功接入 `Architecture`、`Model`、`System` 与旧版命令执行器。

> 说明：当前仓库同时存在旧版 `Command` / `Query` 执行器与新版 `CQRS` runtime。本页故意先用最短路径说明基础架构如何跑起来；如果你要写新功能，随后应继续阅读 [`../core/cqrs.md`](../core/cqrs.md)。

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
    protected override void OnInitialize()
    {
        RegisterModel(new CounterModel());
        RegisterSystem(new CounterSystem());
    }
}
```

这里要点只有两个：

- 架构入口是 `Architecture`
- 当前版本使用 `protected override void OnInitialize()` 注册模型、系统和工具

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

## 5. 定义一个命令

```csharp
using GFramework.Core.Command;

public sealed class IncreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = GetModel<CounterModel>();
        model.Count.Value += 1;
    }
}
```

`AbstractCommand` 继承自 `ContextAwareBase`，所以命令内部可以直接访问 `GetModel<T>()`、`GetSystem<T>()` 等上下文方法。

## 6. 初始化并执行

```csharp
var architecture = new CounterArchitecture();
architecture.Initialize();

architecture.Context.SendCommand(new IncreaseCountCommand());

var count = architecture.Context.GetModel<CounterModel>().Count.Value;
Console.WriteLine(count); // 1
```

如果你能走通这一步，说明以下链路已经成立：

- 架构初始化
- 模型 / 系统注册
- 上下文访问
- 旧版命令执行

## 下一步

- 想切到推荐的新请求模型：看 [`../core/cqrs.md`](../core/cqrs.md)
- 想进入游戏层能力：看 [`../game/index.md`](../game/index.md)
- 想看模块入口而不是栏目页：回到对应模块目录下的 `README.md`

# Command

本页只说明 `GFramework.Core.Command` 里的旧命令体系。

它仍然被保留，用来兼容存量代码；但如果你在写新功能，优先使用 [cqrs](./cqrs.md) 里的新请求模型。

## 当前仍然可用的基类

旧命令体系当前最常见的三个基类是：

- `AbstractCommand`
  - 无输入、无返回值
- `AbstractCommand<TInput>`
  - 有输入、无返回值
- `AbstractCommand<TInput, TResult>`
  - 有输入、有返回值

注意一个和旧文档不同的点：泛型命令现在通过构造函数接收输入，而不是依赖 `Input` 可写属性。

## 无输入命令

```csharp
using GFramework.Core.Command;
using GFramework.Core.Extensions;

public sealed class RestoreHealthCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.Health.Value = playerModel.MaxHealth.Value;
        this.SendEvent(new PlayerHealthRestoredEvent());
    }
}
```

发送方式：

```csharp
this.SendCommand(new RestoreHealthCommand());
```

## 带输入命令

旧命令输入类型现在直接复用 CQRS 抽象层里的 `ICommandInput`：

```csharp
using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

public sealed record DamagePlayerInput(int Amount) : ICommandInput;

public sealed class DamagePlayerCommand(DamagePlayerInput input)
    : AbstractCommand<DamagePlayerInput>(input)
{
    protected override void OnExecute(DamagePlayerInput input)
    {
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.Health.Value -= input.Amount;
    }
}
```

发送方式：

```csharp
this.SendCommand(new DamagePlayerCommand(new DamagePlayerInput(10)));
```

## 带返回值命令

```csharp
using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

public sealed record GetGoldRewardInput(int EnemyLevel) : ICommandInput;

public sealed class GetGoldRewardCommand(GetGoldRewardInput input)
    : AbstractCommand<GetGoldRewardInput, int>(input)
{
    protected override int OnExecute(GetGoldRewardInput input)
    {
        return input.EnemyLevel * 10;
    }
}
```

```csharp
var reward = this.SendCommand(new GetGoldRewardCommand(new GetGoldRewardInput(3)));
```

## 发送入口

旧命令由 `IArchitectureContext` 的兼容入口执行：

- `SendCommand(ICommand)`
- `SendCommand<TResult>(ICommand<TResult>)`
- `SendCommandAsync(IAsyncCommand)`
- `SendCommandAsync<TResult>(IAsyncCommand<TResult>)`

在 `IContextAware` 对象内，通常直接通过扩展使用：

```csharp
using GFramework.Core.Extensions;
```

## 什么时候还应该用旧命令

- 你在维护既有 `Core.Command` 代码
- 你的调用链已经依赖旧 `CommandExecutor`
- 当前改动目标是局部修复，不值得同时做 CQRS 迁移

## 什么时候该切到 CQRS

下面这些场景更适合新 CQRS runtime：

- 需要 request / notification / stream 的统一模型
- 需要 pipeline behaviors
- 需要 handler registry 生成器
- 你正在写新的业务模块，而不是维护历史命令代码

迁移后常见写法见：[cqrs](./cqrs.md)

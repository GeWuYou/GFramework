# Query

本页说明 `GFramework.Core.Query` 里的旧查询体系。

和旧命令系统一样，它仍然保留用于兼容存量代码；新功能优先使用 [cqrs](./cqrs.md) 中的新查询模型。

## 当前仍然可用的基类

旧查询体系最常见的两个基类是：

- `AbstractQuery<TResult>`
  - 无输入查询
- `AbstractQuery<TInput, TResult>`
  - 带输入查询

与旧文档不同，带输入查询现在通过构造函数接收输入，不再依赖 `Input` 属性赋值。

## 无输入查询

```csharp
using GFramework.Core.Extensions;
using GFramework.Core.Query;

public sealed class GetPlayerHealthQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        return this.GetModel<PlayerModel>().Health.Value;
    }
}
```

发送方式：

```csharp
var health = this.SendQuery(new GetPlayerHealthQuery());
```

## 带输入查询

旧查询输入类型现在直接复用 CQRS 抽象层里的 `IQueryInput`：

```csharp
using GFramework.Core.Extensions;
using GFramework.Core.Query;
using GFramework.Cqrs.Abstractions.Cqrs.Query;

public sealed record GetItemCountInput(string ItemId) : IQueryInput;

public sealed class GetItemCountQuery(GetItemCountInput input)
    : AbstractQuery<GetItemCountInput, int>(input)
{
    protected override int OnDo(GetItemCountInput input)
    {
        var inventoryModel = this.GetModel<InventoryModel>();
        return inventoryModel.GetItemCount(input.ItemId);
    }
}
```

```csharp
var count = this.SendQuery(
    new GetItemCountQuery(new GetItemCountInput("potion")));
```

## 异步查询

上下文仍然保留旧异步查询执行入口：

- `SendQueryAsync(IAsyncQuery<TResult>)`

这主要面向兼容旧 `AsyncQueryExecutor` 路径。文档不再推荐围绕旧 `QueryBus` 设计新功能。

## 发送入口

旧查询的执行入口是：

- `SendQuery<TResult>(IQuery<TResult>)`
- `SendQueryAsync<TResult>(IAsyncQuery<TResult>)`

在 `IContextAware` 对象内部，通常直接使用 `GFramework.Core.Extensions` 里的扩展：

```csharp
using GFramework.Core.Extensions;
```

## 什么时候继续保留旧查询

- 你在维护现有 `Core.Query` 代码
- 当前代码已经建立在旧查询执行器之上
- 你只想修正局部行为，不想顺手迁移整条调用链

## 什么时候改用 CQRS 查询

如果你正在写新的读取路径，优先考虑：

- `GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse>`
- `AbstractQueryHandler<TQuery, TResponse>`
- `architecture.Context.SendQueryAsync(...)`

原因很简单：新查询路径和命令、通知、流式请求共享同一 dispatcher 与行为管道。

继续阅读：[cqrs](./cqrs.md)

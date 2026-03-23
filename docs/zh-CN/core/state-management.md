# State Management 包使用说明

## 概述

State Management 提供一个可选的集中式状态容器方案，用于补足 `BindableProperty<T>` 在复杂状态树场景下的能力。

当你的状态具有以下特征时，推荐使用 `Store<TState>`：

- 多个字段需要在一次业务操作中协同更新
- 多个模块或 UI 片段共享同一聚合状态
- 希望所有状态写入都经过统一的 action / reducer 入口
- 需要对整棵状态树做局部选择和按片段订阅

这套能力不会替代现有 Property 机制，而是与其并存：

- `BindableProperty<T>`：字段级响应式值
- `Store<TState>`：聚合状态容器
- `StateMachine`：流程状态切换

## 核心接口

### IReadonlyStore`<TState>`

只读状态容器接口，提供：

- `State`：读取当前状态快照
- `Subscribe()`：订阅状态变化
- `SubscribeWithInitValue()`：订阅并立即回放当前状态
- `UnSubscribe()`：取消订阅

### IStore`<TState>`

在只读能力上增加：

- `Dispatch<TAction>()`：统一分发 action

### IReducer`<TState, TAction>`

定义状态归约逻辑：

```csharp
public interface IReducer<TState, in TAction>
{
    TState Reduce(TState currentState, TAction action);
}
```

### IStateSelector`<TState, TSelected>`

从整棵状态树中投影局部视图，便于 UI 和 Controller 复用选择逻辑。

## Store`<TState>`

`Store<TState>` 是默认实现，支持：

- 初始状态快照
- reducer 注册
- middleware 分发管线
- 只在状态真正变化时通知订阅者
- 基础诊断信息（最近一次 action、最近一次分发记录、最近一次状态变化时间）

## 基本示例

```csharp
using GFramework.Core.StateManagement;

public sealed record PlayerState(int Health, string Name);
public sealed record DamageAction(int Amount);
public sealed record RenameAction(string Name);

var store = new Store<PlayerState>(new PlayerState(100, "Player"))
    .RegisterReducer<DamageAction>((state, action) =>
        state with { Health = Math.Max(0, state.Health - action.Amount) })
    .RegisterReducer<RenameAction>((state, action) =>
        state with { Name = action.Name });

store.SubscribeWithInitValue(state =>
{
    Console.WriteLine($"{state.Name}: {state.Health}");
});

store.Dispatch(new DamageAction(25));
store.Dispatch(new RenameAction("Knight"));
```

## 选择器和 Bindable 风格桥接

Store 可以通过扩展方法把聚合状态投影成局部只读绑定视图：

```csharp
using GFramework.Core.Extensions;

var healthSelection = store.Select(state => state.Health);

healthSelection.RegisterWithInitValue(health =>
{
    Console.WriteLine($"Current HP: {health}");
});
```

如果现有 UI 代码已经依赖 `IReadonlyBindableProperty<T>`，可以直接桥接：

```csharp
IReadonlyBindableProperty<int> healthProperty =
    store.ToBindableProperty(state => state.Health);
```

## 在 Model 中使用

推荐把 Store 作为 Model 的内部状态容器，由 Model 暴露领域友好的业务方法：

```csharp
public class PlayerStateModel : AbstractModel
{
    public Store<PlayerState> Store { get; } = new(new PlayerState(100, "Player"));

    protected override void OnInit()
    {
        Store.RegisterReducer<DamageAction>((state, action) =>
            state with { Health = Math.Max(0, state.Health - action.Amount) });
    }

    public void TakeDamage(int amount)
    {
        Store.Dispatch(new DamageAction(amount));
    }
}
```

这样可以保留 Model 的生命周期和领域边界，同时获得统一状态入口。

## 什么时候不用 Store

以下情况继续优先使用 `BindableProperty<T>`：

- 单一字段直接绑定 UI
- 状态规模很小，不需要聚合归约
- 没有跨模块共享状态树的需求
- 你只需要“值变化通知”，不需要“统一状态演进入口”

## 最佳实践

1. 优先把 `TState` 设计为不可变状态（如 `record`）
2. 让 reducer 保持纯函数风格，不在 reducer 内执行副作用
3. 使用 selector 暴露局部状态，而不是让 UI 自己解析整棵状态树
4. 需要日志或诊断时，优先通过 middleware 扩展，而不是把横切逻辑塞进 reducer

## 相关文档

- [`property`](./property) - 字段级响应式属性
- [`model`](./model) - Store 常见承载位置
- [`events`](./events) - 组件间事件通信
- [`state-machine-tutorial`](../tutorials/state-machine-tutorial) - 流程状态切换能力

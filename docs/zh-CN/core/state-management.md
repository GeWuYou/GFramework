---
title: State Management 包使用说明
description: 说明 GFramework.Core.StateManagement 的 Store、Reducer 与状态容器用法。
---

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
- `RunInBatch()`：在一个批处理中合并多次状态通知
- `Undo()` / `Redo()`：基于历史缓冲区回退或前进状态
- `TimeTravelTo()`：跳转到指定历史索引
- `ClearHistory()`：以当前状态重置历史锚点

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
- 可选历史缓冲区、撤销/重做和时间旅行
- 可选批处理通知折叠
- 可选多态 action 匹配（基类 / 接口）
- 只在状态真正变化时通知订阅者
- 基础诊断信息（最近一次 action、最近一次分发记录、最近一次状态变化时间、历史游标、批处理状态）

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

## 使用 StoreBuilder 组织配置

当一个 Store 需要在模块安装、测试工厂或 DI 装配阶段统一配置时，可以使用 `StoreBuilder<TState>`：

```csharp
var store = (Store<PlayerState>)Store<PlayerState>
    .CreateBuilder()
    .WithHistoryCapacity(32)
    .AddReducer<DamageAction>((state, action) =>
        state with { Health = Math.Max(0, state.Health - action.Amount) })
    .Build(new PlayerState(100, "Player"));
```

适合以下场景：

- 模块启动时集中注册 reducer 和 middleware
- 测试里快速组装不同配置的 Store
- 不希望把 Store 的装配细节散落在多个调用点

如果需要扩展新语义，`StoreBuilder<TState>` 还支持：

- `WithHistoryCapacity(int)`：开启撤销 / 重做 / 时间旅行缓冲区
- `WithActionMatching(StoreActionMatchingMode)`：切换 reducer 的 action 匹配策略

## 历史记录、撤销 / 重做与时间旅行

当状态需要调试回放、工具面板查看或编辑器内撤销/重做时，可以开启历史缓冲区：

```csharp
var store = new Store<PlayerState>(
    new PlayerState(100, "Player"),
    historyCapacity: 32);

store.Dispatch(new DamageAction(10));
store.Dispatch(new RenameAction("Knight"));

store.Undo();
store.Redo();
store.TimeTravelTo(0);
store.ClearHistory();
```

需要注意：

- `historyCapacity: 0` 表示关闭历史记录
- 历史只记录“状态真正变化”的 dispatch
- `Undo()` / `Redo()` / `TimeTravelTo()` 会更新当前状态并像普通状态变化一样通知订阅者
- 当你从历史中回退后再执行新的 `Dispatch()`，原来的 redo 分支会被裁掉

## 批处理通知折叠

如果一次业务操作会连续触发多个 action，但外部订阅者只需要看到最终状态，可以使用批处理：

```csharp
store.RunInBatch(() =>
{
    store.Dispatch(new DamageAction(10));
    store.Dispatch(new RenameAction("Knight"));
});
```

批处理语义如下：

- 批处理内部每次 dispatch 仍会立即更新 Store 状态
- 订阅通知会延迟到最外层批处理结束后再统一发送一次
- 嵌套批处理是允许的，只有最外层结束时才会发通知
- 状态变化桥接到 `EventBus` 时，也会复用这个折叠语义

## 多态 action 匹配

默认情况下，Store 只匹配与 action 运行时类型完全一致的 reducer，这样最稳定，也最容易推导。

如果你的 action 体系确实依赖基类或接口复用，可以显式开启多态匹配：

```csharp
var store = new Store<PlayerState>(
    new PlayerState(100, "Player"),
    actionMatchingMode: StoreActionMatchingMode.IncludeAssignableTypes);
```

启用后，reducer 的执行顺序保持确定性：

1. 精确类型 reducer
2. 最近的基类 reducer
3. 接口 reducer

只有在你明确需要这类复用关系时才建议启用；大多数业务状态仍建议继续使用默认的精确匹配模式。

## Store 到 EventBus 的兼容桥接

如果你在迁移旧模块时，现有逻辑仍然依赖 `EventBus`，可以临时把 Store 的 dispatch 和状态变化桥接过去：

```csharp
using GFramework.Core.Events;
using GFramework.Core.Extensions;

var eventBus = new EventBus();
var bridge = store.BridgeToEventBus(eventBus);
```

桥接后会发送两类事件：

- `StoreDispatchedEvent<TState>`：每次 dispatch 都会发送一次，即使状态没有变化
- `StoreStateChangedEvent<TState>`：只在状态真正变化时发送；批处理中只发送最终状态

不再需要兼容层时，调用 `bridge.UnRegister()` 即可拆除桥接。

## 运行时临时注册与注销

如果某个 reducer 或 middleware 只需要在一段生命周期内生效，例如调试探针、临时玩法规则、
或场景级模块扩展，可以直接使用 `Store<TState>` 提供的句柄式注册 API：

```csharp
public sealed class LoggingMiddleware<TState> : IStoreMiddleware<TState>
{
    public void Invoke(StoreDispatchContext<TState> context, Action next)
    {
        Console.WriteLine($"Dispatching: {context.ActionType.Name}");
        next();
    }
}

var store = new Store<PlayerState>(new PlayerState(100, "Player"));

var reducerHandle = store.RegisterReducerHandle<DamageAction>((state, action) =>
    state with { Health = Math.Max(0, state.Health - action.Amount) });

var middlewareHandle = store.RegisterMiddleware(new LoggingMiddleware<PlayerState>());

store.Dispatch(new DamageAction(10));

reducerHandle.UnRegister();
middlewareHandle.UnRegister();
```

这里有两个重要约束：

- `RegisterReducerHandle()` 和 `RegisterMiddleware()` 返回的是当前这一次注册的精确注销句柄
- 如果在某次 `Dispatch()` 已经开始后再调用 `UnRegister()`，当前这次 dispatch 仍会继续使用开始时抓取的快照，注销只影响后续新的
  dispatch

如果你只需要初始化阶段的链式配置，继续使用 `RegisterReducer()` 和 `UseMiddleware()` 即可；
如果你需要运行时清理，就使用上面的句柄式 API。

## 官方示例：角色面板状态

下面给出一个更贴近 GFramework 实战的完整示例，展示如何把 `Store<TState>` 放进 Model，
再通过 Command 修改状态，并在 Controller 中使用 selector 做 UI 绑定。

### 1. 定义状态和 action

```csharp
public sealed record PlayerPanelState(
    string Name,
    int Health,
    int MaxHealth,
    int Level);

public sealed record DamagePlayerAction(int Amount);
public sealed record HealPlayerAction(int Amount);
public sealed record RenamePlayerAction(string Name);
```

### 2. 在 Model 中承载 Store

```csharp
using GFramework.Core.Abstractions.Property;
using GFramework.Core.Model;
using GFramework.Core.Extensions;
using GFramework.Core.StateManagement;

public class PlayerPanelModel : AbstractModel
{
    public Store<PlayerPanelState> Store { get; } =
        new(new PlayerPanelState("Player", 100, 100, 1));

    // 使用带缓存的选择视图，避免属性 getter 每次访问都创建新的 StoreSelection 实例。
    public IReadonlyBindableProperty<int> Health =>
        Store.GetOrCreateBindableProperty("health", state => state.Health);

    public IReadonlyBindableProperty<string> Name =>
        Store.GetOrCreateBindableProperty("name", state => state.Name);

    public IReadonlyBindableProperty<float> HealthPercent =>
        Store.GetOrCreateBindableProperty("health_percent",
            state => (float)state.Health / state.MaxHealth);

    protected override void OnInit()
    {
        Store
            .RegisterReducer<DamagePlayerAction>((state, action) =>
                state with
                {
                    Health = Math.Max(0, state.Health - action.Amount)
                })
            .RegisterReducer<HealPlayerAction>((state, action) =>
                state with
                {
                    Health = Math.Min(state.MaxHealth, state.Health + action.Amount)
                })
            .RegisterReducer<RenamePlayerAction>((state, action) =>
                state with
                {
                    Name = action.Name
                });
    }
}
```

这个写法的关键点是：

- 状态结构集中定义在 `PlayerPanelState`
- 所有状态修改都经过 reducer
- 高频访问的局部状态通过缓存选择视图复用实例
- Controller 只消费局部只读视图，不直接修改 Store

### 3. 通过 Command 修改状态

```csharp
using GFramework.Core.Command;

public sealed class DamagePlayerCommand(int amount) : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = this.GetModel<PlayerPanelModel>();
        model.Store.Dispatch(new DamagePlayerAction(amount));
    }
}

public sealed class RenamePlayerCommand(string name) : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = this.GetModel<PlayerPanelModel>();
        model.Store.Dispatch(new RenamePlayerAction(name));
    }
}
```

这里仍然遵循 GFramework 现有分层：

- Controller 负责转发用户意图
- Command 负责执行业务操作
- Model 持有状态
- Store 负责统一归约状态变化

### 4. 在 Controller 中绑定局部状态

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Events;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class PlayerPanelController : IController
{
    private readonly IUnRegisterList _unRegisterList = new UnRegisterList();

    public void Initialize()
    {
        var model = this.GetModel<PlayerPanelModel>();

        model.Name
            .RegisterWithInitValue(name =>
            {
                Console.WriteLine($"Player Name: {name}");
            })
            .AddToUnregisterList(_unRegisterList);

        model.Health
            .RegisterWithInitValue(health =>
            {
                Console.WriteLine($"Health: {health}");
            })
            .AddToUnregisterList(_unRegisterList);

        model.HealthPercent
            .RegisterWithInitValue(percent =>
            {
                Console.WriteLine($"Health Percent: {percent:P0}");
            })
            .AddToUnregisterList(_unRegisterList);
    }

    public void OnDamageButtonClicked()
    {
        this.SendCommand(new DamagePlayerCommand(15));
    }

    public void OnRenameButtonClicked(string newName)
    {
        this.SendCommand(new RenamePlayerCommand(newName));
    }
}
```

### 5. 什么时候这个示例比 BindableProperty 更合适

如果你只需要：

- `Health`
- `Name`
- `Level`

分别独立通知，那么多个 `BindableProperty<T>` 就足够了。

如果你很快会遇到以下问题，这个 Store 方案会更稳：

- 一次操作要同时修改多个字段
- 同一个业务操作要在多个界面复用
- 希望把“状态结构”和“状态变化规则”集中在一起
- 需要 middleware、调试记录、撤销/重做或时间旅行能力

### 6. 推荐的落地方式

在实际项目里，建议按这个顺序引入：

1. 先把复杂聚合状态封装到某个 Model 内部
2. 再把修改入口逐步迁移到 Command
3. 最后在 Controller 层使用 selector 或 `ToBindableProperty()` 做局部绑定

这样不会破坏现有 `BindableProperty<T>` 的轻量工作流，也能让复杂状态逐步收敛到统一入口。

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
5. 默认优先使用精确类型 reducer 匹配；只有确有继承层次复用需求时再启用多态匹配
6. `EventBus` 桥接只建议作为迁移过渡层，新模块应优先直接依赖 Store

## 相关文档

- [`property`](./property.md) - 字段级响应式属性
- [`model`](./model.md) - Store 常见承载位置
- [`events`](./events.md) - 组件间事件通信
- [`state-machine-tutorial`](../tutorials/state-machine-tutorial.md) - 流程状态切换能力

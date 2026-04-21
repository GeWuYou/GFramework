using System.Collections;
using System.Reflection;

namespace GFramework.Core.Tests.StateManagement;

/// <summary>
///     Store 状态管理能力的单元测试。
///     这些测试覆盖集中式状态容器的核心职责：状态归约、订阅通知、选择器桥接和诊断行为。
/// </summary>
[TestFixture]
public class StoreTests
{
    /// <summary>
    ///     测试 Store 在创建后能够暴露初始状态。
    /// </summary>
    [Test]
    public void State_Should_Return_Initial_State()
    {
        var store = CreateStore(new CounterState(1, "Player"));

        Assert.That(store.State.Count, Is.EqualTo(1));
        Assert.That(store.State.Name, Is.EqualTo("Player"));
    }

    /// <summary>
    ///     测试 Dispatch 能够执行 reducer 并向订阅者广播新状态。
    /// </summary>
    [Test]
    public void Dispatch_Should_Update_State_And_Notify_Subscribers()
    {
        var store = CreateStore();
        var receivedStates = new List<CounterState>();

        store.Subscribe(receivedStates.Add);

        store.Dispatch(new IncrementAction(2));

        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(receivedStates.Count, Is.EqualTo(1));
        Assert.That(receivedStates[0].Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     测试当 reducer 返回逻辑相等状态时不会触发通知。
    /// </summary>
    [Test]
    public void Dispatch_Should_Not_Notify_When_State_Does_Not_Change()
    {
        var store = CreateStore();
        var notifyCount = 0;

        store.Subscribe(_ => notifyCount++);

        store.Dispatch(new RenameAction("Player"));

        Assert.That(store.State.Name, Is.EqualTo("Player"));
        Assert.That(notifyCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试同一 action 类型的多个 reducer 会按注册顺序执行。
    /// </summary>
    [Test]
    public void Dispatch_Should_Run_Multiple_Reducers_In_Registration_Order()
    {
        var store = CreateStore();
        store.RegisterReducer<IncrementAction>((state, action) =>
            state with { Count = state.Count + action.Amount * 10 });

        store.Dispatch(new IncrementAction(1));

        Assert.That(store.State.Count, Is.EqualTo(11));
    }

    /// <summary>
    ///     测试 SubscribeWithInitValue 会立即回放当前状态并继续接收后续变化。
    /// </summary>
    [Test]
    public void SubscribeWithInitValue_Should_Replay_Current_State_And_Future_Changes()
    {
        var store = CreateStore(new CounterState(5, "Player"));
        var receivedCounts = new List<int>();

        store.SubscribeWithInitValue(state => receivedCounts.Add(state.Count));
        store.Dispatch(new IncrementAction(3));

        Assert.That(receivedCounts, Is.EqualTo(new[] { 5, 8 }));
    }

    /// <summary>
    ///     测试 Store 的 SubscribeWithInitValue 在初始化回放期间不会漏掉后续状态变化。
    /// </summary>
    [Test]
    public void SubscribeWithInitValue_Should_Not_Miss_Changes_During_Init_Callback()
    {
        var store = CreateStore();
        var receivedCounts = new List<int>();

        store.SubscribeWithInitValue(state =>
        {
            receivedCounts.Add(state.Count);
            if (receivedCounts.Count == 1)
            {
                store.Dispatch(new IncrementAction(1));
            }
        });

        Assert.That(receivedCounts, Is.EqualTo(new[] { 0, 1 }));
    }

    /// <summary>
    ///     测试注销订阅后不会再收到后续通知。
    /// </summary>
    [Test]
    public void UnRegister_Handle_Should_Stop_Future_Notifications()
    {
        var store = CreateStore();
        var notifyCount = 0;

        var unRegister = store.Subscribe(_ => notifyCount++);
        store.Dispatch(new IncrementAction(1));
        unRegister.UnRegister();
        store.Dispatch(new IncrementAction(1));

        Assert.That(notifyCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试选择器仅在所选状态片段变化时触发通知。
    /// </summary>
    [Test]
    public void Select_Should_Only_Notify_When_Selected_Slice_Changes()
    {
        var store = CreateStore();
        var selectedCounts = new List<int>();
        var selection = store.Select(state => state.Count);

        selection.Register(selectedCounts.Add);

        store.Dispatch(new RenameAction("Renamed"));
        store.Dispatch(new IncrementAction(2));

        Assert.That(selectedCounts, Is.EqualTo(new[] { 2 }));
    }

    /// <summary>
    ///     测试选择器支持自定义比较器，从而抑制无意义的局部状态通知。
    /// </summary>
    [Test]
    public void Select_Should_Respect_Custom_Selected_Value_Comparer()
    {
        var store = CreateStore();
        var selectedCounts = new List<int>();
        var selection = store.Select(
            state => state.Count,
            new TensBucketEqualityComparer());

        selection.Register(selectedCounts.Add);

        store.Dispatch(new IncrementAction(5));
        store.Dispatch(new IncrementAction(6));

        Assert.That(selectedCounts, Is.EqualTo(new[] { 11 }));
    }

    /// <summary>
    ///     测试 StoreSelection 的 RegisterWithInitValue 在初始化回放期间不会漏掉后续局部状态变化。
    /// </summary>
    [Test]
    public void Selection_RegisterWithInitValue_Should_Not_Miss_Changes_During_Init_Callback()
    {
        var store = CreateStore();
        var selection = store.Select(state => state.Count);
        var receivedCounts = new List<int>();

        selection.RegisterWithInitValue(value =>
        {
            receivedCounts.Add(value);
            if (receivedCounts.Count == 1)
            {
                store.Dispatch(new IncrementAction(1));
            }
        });

        Assert.That(receivedCounts, Is.EqualTo(new[] { 0, 1 }));
    }

    /// <summary>
    ///     测试 ToBindableProperty 可桥接到现有 BindableProperty 风格用法，并与旧属性系统共存。
    /// </summary>
    [Test]
    public void ToBindableProperty_Should_Work_With_Existing_BindableProperty_Pattern()
    {
        var store = CreateStore();
        var mirror = new BindableProperty<int>(0);
        IReadonlyBindableProperty<int> bindableProperty = store.ToBindableProperty(state => state.Count);

        bindableProperty.Register(value => mirror.Value = value);
        store.Dispatch(new IncrementAction(3));

        Assert.That(mirror.Value, Is.EqualTo(3));
    }

    /// <summary>
    ///     测试 IStateSelector 接口重载能够复用显式选择逻辑。
    /// </summary>
    [Test]
    public void Select_With_IStateSelector_Should_Project_Selected_Value()
    {
        var store = CreateStore();
        var selection = store.Select(new CounterNameSelector());

        Assert.That(selection.Value, Is.EqualTo("Player"));
    }

    /// <summary>
    ///     测试 Store 在中间件内部发生同一实例的嵌套分发时会抛出异常。
    /// </summary>
    [Test]
    public void Dispatch_Should_Throw_When_Nested_Dispatch_Happens_On_Same_Store()
    {
        var store = CreateStore();
        store.UseMiddleware(new NestedDispatchMiddleware(store));

        Assert.That(
            () => store.Dispatch(new IncrementAction(1)),
            Throws.InvalidOperationException.With.Message.Contain("Nested dispatch"));
    }

    /// <summary>
    ///     测试中间件链执行顺序和 Store 诊断信息更新。
    /// </summary>
    [Test]
    public void Dispatch_Should_Run_Middlewares_In_Order_And_Update_Diagnostics()
    {
        var store = CreateStore();
        var logs = new List<string>();

        store.UseMiddleware(new RecordingMiddleware(logs, "first"));
        store.UseMiddleware(new RecordingMiddleware(logs, "second"));

        store.Dispatch(new IncrementAction(2));

        Assert.That(logs, Is.EqualTo(new[]
        {
            "first:before",
            "second:before",
            "second:after",
            "first:after"
        }));

        Assert.That(store.LastActionType, Is.EqualTo(typeof(IncrementAction)));
        Assert.That(store.LastStateChangedAt, Is.Not.Null);
        Assert.That(store.LastDispatchRecord, Is.Not.Null);
        Assert.That(store.LastDispatchRecord!.HasStateChanged, Is.True);
        Assert.That(store.LastDispatchRecord.NextState.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     测试 reducer 句柄注销后，后续同类型 action 不会再命中该 reducer。
    /// </summary>
    [Test]
    public void RegisterReducerHandle_UnRegister_Should_Stop_Future_Reductions()
    {
        var store = new Store<CounterState>(new CounterState(0, "Player"));
        var reducerHandle = store.RegisterReducerHandle<IncrementAction>((state, action) =>
            state with { Count = state.Count + action.Amount });

        store.Dispatch(new IncrementAction(2));
        reducerHandle.UnRegister();
        store.Dispatch(new IncrementAction(2));

        Assert.That(store.State.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     测试 middleware 句柄注销后，后续 dispatch 不会再经过该中间件。
    /// </summary>
    [Test]
    public void RegisterMiddleware_UnRegister_Should_Stop_Future_Pipeline_Execution()
    {
        var store = CreateStore();
        var logs = new List<string>();
        var middlewareHandle = store.RegisterMiddleware(new RecordingMiddleware(logs, "dynamic"));

        store.Dispatch(new IncrementAction(1));
        middlewareHandle.UnRegister();
        store.Dispatch(new IncrementAction(1));

        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(logs, Is.EqualTo(new[] { "dynamic:before", "dynamic:after" }));
    }

    /// <summary>
    ///     测试移除同一 action 类型中的某个 reducer 后，其余 reducer 仍保持原有注册顺序。
    /// </summary>
    [Test]
    public void RegisterReducerHandle_UnRegister_Should_Preserve_Remaining_Order()
    {
        var executionOrder = new List<string>();
        var store = new Store<CounterState>(new CounterState(0, "Player"));

        store.RegisterReducerHandle<IncrementAction>((state, action) =>
        {
            executionOrder.Add("first");
            return state with { Count = state.Count + action.Amount };
        });

        var middleReducer = store.RegisterReducerHandle<IncrementAction>((state, action) =>
        {
            executionOrder.Add("middle");
            return state with { Count = state.Count + action.Amount * 10 };
        });

        store.RegisterReducerHandle<IncrementAction>((state, action) =>
        {
            executionOrder.Add("last");
            return state with { Count = state.Count + action.Amount * 100 };
        });

        middleReducer.UnRegister();
        store.Dispatch(new IncrementAction(1));

        Assert.That(executionOrder, Is.EqualTo(new[] { "first", "last" }));
        Assert.That(store.State.Count, Is.EqualTo(101));
    }

    /// <summary>
    ///     测试注册句柄的注销操作是幂等的，多次调用不会抛异常或影响其他注册项。
    /// </summary>
    [Test]
    public void RegisterHandles_UnRegister_Should_Be_Idempotent()
    {
        var logs = new List<string>();
        var store = new Store<CounterState>(new CounterState(0, "Player"));
        var reducerHandle = store.RegisterReducerHandle<IncrementAction>((state, action) =>
            state with { Count = state.Count + action.Amount });
        var middlewareHandle = store.RegisterMiddleware(new RecordingMiddleware(logs, "dynamic"));

        Assert.That(() =>
        {
            reducerHandle.UnRegister();
            reducerHandle.UnRegister();
            middlewareHandle.UnRegister();
            middlewareHandle.UnRegister();
        }, Throws.Nothing);

        store.Dispatch(new IncrementAction(1));

        Assert.That(store.State.Count, Is.EqualTo(0));
        Assert.That(logs, Is.Empty);
    }

    /// <summary>
    ///     测试 dispatch 进行中注销 reducer 和 middleware 时，
    ///     当前 dispatch 仍使用开始时的快照，而后续 dispatch 会看到注销结果。
    /// </summary>
    [Test]
    public void UnRegister_During_Dispatch_Should_Affect_Next_Dispatch_But_Not_Current_One()
    {
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var logs = new List<string>();
        var store = new Store<CounterState>(new CounterState(0, "Player"));
        var reducerHandle = store.RegisterReducerHandle<IncrementAction>((state, action) =>
            state with { Count = state.Count + action.Amount });
        var blockingHandle = store.RegisterMiddleware(new BlockingMiddleware(entered, release));
        var recordingHandle = store.RegisterMiddleware(new RecordingMiddleware(logs, "dynamic"));

        var dispatchTask = Task.Run(() => store.Dispatch(new IncrementAction(1)));

        Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True, "middleware 未按预期进入阻塞阶段");

        reducerHandle.UnRegister();
        blockingHandle.UnRegister();
        recordingHandle.UnRegister();
        release.Set();

        Assert.That(dispatchTask.Wait(TimeSpan.FromSeconds(2)), Is.True, "dispatch 未在释放 middleware 后完成");
        Assert.That(store.State.Count, Is.EqualTo(1), "当前 dispatch 应继续使用启动时抓取的 reducer 快照");
        Assert.That(logs, Is.EqualTo(new[] { "dynamic:before", "dynamic:after" }));

        store.Dispatch(new IncrementAction(1));

        Assert.That(store.State.Count, Is.EqualTo(1), "后续 dispatch 应看到 reducer 已被注销");
        Assert.That(logs, Is.EqualTo(new[] { "dynamic:before", "dynamic:after" }));
    }

    /// <summary>
    ///     测试当 dispatch 作用域在快照阶段抛出异常时，Store 不会残留“正在分发”标记而锁死后续调用。
    /// </summary>
    [Test]
    public void Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws()
    {
        var store = new Store<CounterState>(
            new CounterState(0, "Player"),
            actionMatchingMode: StoreActionMatchingMode.IncludeAssignableTypes);

        store.RegisterReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount });

        var reducers = GetReducersDictionary(store);
        var throwingActionType = new ThrowingAssignableType(typeof(IncrementAction), "simulated reducer snapshot failure");
        reducers.Add(throwingActionType, CreateEmptyReducerRegistrationList(reducers));

        Assert.That(
            () => store.Dispatch(new IncrementAction(1)),
            Throws.InvalidOperationException.With.Message.EqualTo("simulated reducer snapshot failure"));

        reducers.Remove(throwingActionType);

        Assert.That(() => store.Dispatch(new IncrementAction(1)), Throws.Nothing);
        Assert.That(store.State.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试未命中的 action 仍会记录诊断信息，但不会改变状态。
    /// </summary>
    [Test]
    public void Dispatch_Without_Matching_Reducer_Should_Update_Record_Without_Changing_State()
    {
        var store = CreateStore();

        store.Dispatch(new NoopAction());

        Assert.That(store.State.Count, Is.EqualTo(0));
        Assert.That(store.LastActionType, Is.EqualTo(typeof(NoopAction)));
        Assert.That(store.LastDispatchRecord, Is.Not.Null);
        Assert.That(store.LastDispatchRecord!.HasStateChanged, Is.False);
        Assert.That(store.LastStateChangedAt, Is.Null);
    }

    /// <summary>
    ///     测试 Store 能够复用同一个缓存选择视图实例。
    /// </summary>
    [Test]
    public void GetOrCreateSelection_Should_Return_Cached_Instance_For_Same_Key()
    {
        var store = CreateStore();

        var first = store.GetOrCreateSelection("count", state => state.Count);
        var second = store.GetOrCreateSelection("count", state => state.Count);

        Assert.That(second, Is.SameAs(first));
    }

    /// <summary>
    ///     测试 StoreBuilder 能够应用 reducer、中间件和状态比较器配置。
    /// </summary>
    [Test]
    public void StoreBuilder_Should_Apply_Configured_Reducers_Middlewares_And_Comparer()
    {
        var logs = new List<string>();
        var store = (Store<CounterState>)Store<CounterState>
            .CreateBuilder()
            .WithComparer(new CounterStateNameInsensitiveComparer())
            .AddReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount })
            .AddReducer<RenameAction>((state, action) => state with { Name = action.Name })
            .UseMiddleware(new RecordingMiddleware(logs, "builder"))
            .Build(new CounterState(0, "Player"));

        var notifyCount = 0;
        store.Subscribe(_ => notifyCount++);

        store.Dispatch(new RenameAction("player"));
        store.Dispatch(new IncrementAction(2));

        Assert.That(notifyCount, Is.EqualTo(1));
        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(logs, Is.EqualTo(new[] { "builder:before", "builder:after", "builder:before", "builder:after" }));
    }

    /// <summary>
    ///     测试批处理会折叠多次状态变化通知，只在最外层结束时发布最终状态。
    /// </summary>
    [Test]
    public void RunInBatch_Should_Collapse_Notifications_To_Final_State()
    {
        var store = CreateStore();
        var receivedCounts = new List<int>();

        store.Subscribe(state => receivedCounts.Add(state.Count));

        store.RunInBatch(() =>
        {
            Assert.That(store.IsBatching, Is.True);

            store.Dispatch(new IncrementAction(1));
            store.Dispatch(new IncrementAction(2));
        });

        Assert.That(store.IsBatching, Is.False);
        Assert.That(store.State.Count, Is.EqualTo(3));
        Assert.That(receivedCounts, Is.EqualTo(new[] { 3 }));
    }

    /// <summary>
    ///     测试嵌套批处理只会在最外层结束时发出一次通知。
    /// </summary>
    [Test]
    public void RunInBatch_Should_Support_Nested_Batches()
    {
        var store = CreateStore();
        var receivedCounts = new List<int>();

        store.Subscribe(state => receivedCounts.Add(state.Count));

        store.RunInBatch(() =>
        {
            store.Dispatch(new IncrementAction(1));

            store.RunInBatch(() =>
            {
                Assert.That(store.IsBatching, Is.True);
                store.Dispatch(new IncrementAction(1));
            });

            store.Dispatch(new IncrementAction(1));
        });

        Assert.That(store.State.Count, Is.EqualTo(3));
        Assert.That(receivedCounts, Is.EqualTo(new[] { 3 }));
    }

    /// <summary>
    ///     测试启用历史记录后支持撤销、重做、时间旅行和 redo 分支裁剪。
    /// </summary>
    [Test]
    public void History_Should_Support_Undo_Redo_Time_Travel_And_Branch_Reset()
    {
        var store = new Store<CounterState>(new CounterState(0, "Player"), historyCapacity: 8);
        store.RegisterReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount });

        store.Dispatch(new IncrementAction(1));
        store.Dispatch(new IncrementAction(1));
        store.Dispatch(new IncrementAction(1));

        Assert.That(store.HistoryCount, Is.EqualTo(4));
        Assert.That(store.HistoryIndex, Is.EqualTo(3));
        Assert.That(store.CanUndo, Is.True);
        Assert.That(store.CanRedo, Is.False);

        store.Undo();
        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(store.HistoryIndex, Is.EqualTo(2));
        Assert.That(store.CanRedo, Is.True);

        store.Undo();
        Assert.That(store.State.Count, Is.EqualTo(1));
        Assert.That(store.HistoryIndex, Is.EqualTo(1));

        store.Redo();
        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(store.HistoryIndex, Is.EqualTo(2));

        store.TimeTravelTo(0);
        Assert.That(store.State.Count, Is.EqualTo(0));
        Assert.That(store.HistoryIndex, Is.EqualTo(0));

        store.TimeTravelTo(2);
        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(store.HistoryIndex, Is.EqualTo(2));

        store.Dispatch(new IncrementAction(10));

        Assert.That(store.State.Count, Is.EqualTo(12));
        Assert.That(store.CanRedo, Is.False, "新 dispatch 应清除 redo 分支");
        Assert.That(store.GetHistoryEntriesSnapshot().Select(entry => entry.State.Count),
            Is.EqualTo(new[] { 0, 1, 2, 12 }));
    }

    /// <summary>
    ///     测试 ClearHistory 会以当前状态重置历史锚点，而不会修改当前状态。
    /// </summary>
    [Test]
    public void ClearHistory_Should_Reset_To_Current_State_Anchor()
    {
        var store = new Store<CounterState>(new CounterState(0, "Player"), historyCapacity: 4);
        store.RegisterReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount });

        store.Dispatch(new IncrementAction(1));
        store.Dispatch(new IncrementAction(1));
        store.ClearHistory();

        Assert.That(store.State.Count, Is.EqualTo(2));
        Assert.That(store.HistoryCount, Is.EqualTo(1));
        Assert.That(store.HistoryIndex, Is.EqualTo(0));
        Assert.That(store.CanUndo, Is.False);
        Assert.That(store.GetHistoryEntriesSnapshot()[0].State.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     测试默认 action 匹配策略仍然只命中精确类型 reducer。
    /// </summary>
    [Test]
    public void Dispatch_Should_Remain_Exact_Type_Only_By_Default()
    {
        var store = new Store<CounterState>(new CounterState(0, "Player"));
        store.RegisterReducer<IncrementActionBase>((state, action) =>
            state with { Count = state.Count + action.Amount * 10 });
        store.RegisterReducer<IIncrementActionMarker>((state, action) =>
            state with { Count = state.Count + action.Amount * 100 });

        store.Dispatch(new DerivedIncrementAction(1));

        Assert.That(store.State.Count, Is.EqualTo(0));
        Assert.That(store.ActionMatchingMode, Is.EqualTo(StoreActionMatchingMode.ExactTypeOnly));
    }

    /// <summary>
    ///     测试启用多态匹配后，Store 会按“精确类型 -> 基类 -> 接口”的稳定顺序执行 reducer。
    /// </summary>
    [Test]
    public void Dispatch_Should_Use_Polymorphic_Action_Matching_In_Deterministic_Order()
    {
        var executionOrder = new List<string>();
        var store = new Store<CounterState>(
            new CounterState(0, "Player"),
            actionMatchingMode: StoreActionMatchingMode.IncludeAssignableTypes);

        store.RegisterReducer<IncrementActionBase>((state, action) =>
        {
            executionOrder.Add("base");
            return state with { Count = state.Count + action.Amount * 10 };
        });

        store.RegisterReducer<IIncrementActionMarker>((state, action) =>
        {
            executionOrder.Add("interface");
            return state with { Count = state.Count + action.Amount * 100 };
        });

        store.RegisterReducer<DerivedIncrementAction>((state, action) =>
        {
            executionOrder.Add("exact");
            return state with { Count = state.Count + action.Amount };
        });

        store.Dispatch(new DerivedIncrementAction(1));

        Assert.That(executionOrder, Is.EqualTo(new[] { "exact", "base", "interface" }));
        Assert.That(store.State.Count, Is.EqualTo(111));
    }

    /// <summary>
    ///     测试 StoreBuilder 能够应用历史容量和 action 匹配策略配置。
    /// </summary>
    [Test]
    public void StoreBuilder_Should_Apply_History_And_Action_Matching_Configuration()
    {
        var store = (Store<CounterState>)Store<CounterState>
            .CreateBuilder()
            .WithHistoryCapacity(6)
            .WithActionMatching(StoreActionMatchingMode.IncludeAssignableTypes)
            .AddReducer<IncrementActionBase>((state, action) => state with { Count = state.Count + action.Amount })
            .Build(new CounterState(0, "Player"));

        store.Dispatch(new DerivedIncrementAction(2));

        Assert.That(store.ActionMatchingMode, Is.EqualTo(StoreActionMatchingMode.IncludeAssignableTypes));
        Assert.That(store.HistoryCapacity, Is.EqualTo(6));
        Assert.That(store.HistoryCount, Is.EqualTo(2));
        Assert.That(store.State.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     测试长时间运行的 middleware 不会长时间占用状态锁，
    ///     使读取状态和新增订阅仍能在 dispatch 进行期间完成。
    /// </summary>
    [Test]
    public void Dispatch_Should_Not_Block_State_Read_Or_Subscribe_While_Middleware_Is_Running()
    {
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var store = CreateStore();
        store.UseMiddleware(new BlockingMiddleware(entered, release));

        var dispatchTask = Task.Run(() => store.Dispatch(new IncrementAction(1)));

        Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True, "middleware 未按预期进入阻塞阶段");

        var stateReadTask = Task.Run(() => store.State.Count);
        Assert.That(stateReadTask.Wait(TimeSpan.FromMilliseconds(200)), Is.True, "State 读取被 dispatch 长时间阻塞");
        Assert.That(stateReadTask.Result, Is.EqualTo(0), "middleware 执行期间应仍能读取到提交前的状态快照");

        var subscribeTask = Task.Run(() =>
        {
            var unRegister = store.Subscribe(_ => { });
            unRegister.UnRegister();
        });
        Assert.That(subscribeTask.Wait(TimeSpan.FromMilliseconds(200)), Is.True, "Subscribe 被 dispatch 长时间阻塞");

        release.Set();

        Assert.That(dispatchTask.Wait(TimeSpan.FromSeconds(2)), Is.True, "dispatch 未在释放 middleware 后完成");
        Assert.That(store.State.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     创建一个带有基础 reducer 的测试 Store。
    /// </summary>
    /// <param name="initialState">可选初始状态。</param>
    /// <returns>已配置基础 reducer 的 Store 实例。</returns>
    private static Store<CounterState> CreateStore(CounterState? initialState = null)
    {
        var store = new Store<CounterState>(initialState ?? new CounterState(0, "Player"));
        store.RegisterReducer<IncrementAction>((state, action) => state with { Count = state.Count + action.Amount });
        store.RegisterReducer<RenameAction>((state, action) => state with { Name = action.Name });
        return store;
    }

    /// <summary>
    ///     读取 Store 内部 reducer 字典，以便在异常安全回归测试中注入受控的异常源。
    /// </summary>
    /// <param name="store">要读取的 Store 实例。</param>
    /// <returns>Store 当前持有的 reducer 字典引用。</returns>
    private static IDictionary GetReducersDictionary(Store<CounterState> store)
    {
        var reducersField = typeof(Store<CounterState>).GetField("_reducers", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate Store reducer dictionary field.");

        return (IDictionary)(reducersField.GetValue(store)
            ?? throw new InvalidOperationException("Store reducer dictionary should not be null."));
    }

    /// <summary>
    ///     创建与 Store 私有 reducer 注册列表兼容的空列表实例。
    /// </summary>
    /// <param name="reducers">现有 reducer 字典，用于推断私有列表元素类型。</param>
    /// <returns>可写入私有 reducer 字典的空列表。</returns>
    private static object CreateEmptyReducerRegistrationList(IDictionary reducers)
    {
        var valueType = reducers.GetType().GenericTypeArguments[1];
        return Activator.CreateInstance(valueType)
            ?? throw new InvalidOperationException("Unable to create an empty reducer registration list.");
    }

    /// <summary>
    ///     用于测试的计数器状态。
    ///     使用 record 保持逻辑不可变语义，便于 Store 基于状态快照进行比较和断言。
    /// </summary>
    /// <param name="Count">当前计数值。</param>
    /// <param name="Name">当前名称。</param>
    private sealed record CounterState(int Count, string Name);

    /// <summary>
    ///     表示增加计数的 action。
    /// </summary>
    /// <param name="Amount">要增加的数量。</param>
    private sealed record IncrementAction(int Amount);

    /// <summary>
    ///     表示修改名称的 action。
    /// </summary>
    /// <param name="Name">新的名称。</param>
    private sealed record RenameAction(string Name);

    /// <summary>
    ///     表示没有匹配 reducer 的 action，用于验证无变更分发路径。
    /// </summary>
    private sealed record NoopAction;

    /// <summary>
    ///     表示参与多态匹配测试的 action 标记接口。
    /// </summary>
    private interface IIncrementActionMarker
    {
        /// <summary>
        ///     获取增量值。
        /// </summary>
        int Amount { get; }
    }

    /// <summary>
    ///     表示多态匹配测试中的基类 action。
    /// </summary>
    /// <param name="Amount">要增加的数量。</param>
    private abstract record IncrementActionBase(int Amount);

    /// <summary>
    ///     表示多态匹配测试中的派生 action。
    /// </summary>
    /// <param name="Amount">要增加的数量。</param>
    private sealed record DerivedIncrementAction(int Amount)
        : IncrementActionBase(Amount), IIncrementActionMarker;

    /// <summary>
    ///     显式选择器实现，用于验证 IStateSelector 重载。
    /// </summary>
    private sealed class CounterNameSelector : IStateSelector<CounterState, string>
    {
        /// <summary>
        ///     从状态中选择名称字段。
        /// </summary>
        /// <param name="state">完整状态。</param>
        /// <returns>名称字段。</returns>
        public string Select(CounterState state)
        {
            return state.Name;
        }
    }

    /// <summary>
    ///     将计数值按十位分桶比较的测试比较器。
    ///     该比较器用于验证选择器只在局部状态“语义变化”时才触发通知。
    /// </summary>
    private sealed class TensBucketEqualityComparer : IEqualityComparer<int>
    {
        /// <summary>
        ///     判断两个值是否落在同一个十位分桶中。
        /// </summary>
        /// <param name="x">左侧值。</param>
        /// <param name="y">右侧值。</param>
        /// <returns>若位于同一分桶则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public bool Equals(int x, int y)
        {
            return x / 10 == y / 10;
        }

        /// <summary>
        ///     返回基于十位分桶的哈希码。
        /// </summary>
        /// <param name="obj">目标值。</param>
        /// <returns>分桶哈希码。</returns>
        public int GetHashCode(int obj)
        {
            return obj / 10;
        }
    }

    /// <summary>
    ///     用于测试 StoreBuilder 自定义状态比较器的比较器实现。
    ///     该比较器忽略名称字段的大小写差异，并保持计数字段严格比较。
    /// </summary>
    private sealed class CounterStateNameInsensitiveComparer : IEqualityComparer<CounterState>
    {
        /// <summary>
        ///     判断两个状态是否在业务语义上相等。
        /// </summary>
        /// <param name="x">左侧状态。</param>
        /// <param name="y">右侧状态。</param>
        /// <returns>若两个状态在计数相同且名称仅大小写不同，则返回 <see langword="true"/>。</returns>
        public bool Equals(CounterState? x, CounterState? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Count == y.Count &&
                   string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     返回与业务语义一致的哈希码。
        /// </summary>
        /// <param name="obj">目标状态。</param>
        /// <returns>忽略名称大小写后的哈希码。</returns>
        public int GetHashCode(CounterState obj)
        {
            return HashCode.Combine(obj.Count, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
        }
    }

    /// <summary>
    ///     记录中间件调用顺序的测试中间件。
    /// </summary>
    private sealed class RecordingMiddleware(List<string> logs, string name) : IStoreMiddleware<CounterState>
    {
        /// <summary>
        ///     记录当前中间件在分发前后的调用顺序。
        /// </summary>
        /// <param name="context">当前分发上下文。</param>
        /// <param name="next">后续处理节点。</param>
        public void Invoke(StoreDispatchContext<CounterState> context, Action next)
        {
            logs.Add($"{name}:before");
            next();
            logs.Add($"{name}:after");
        }
    }

    /// <summary>
    ///     用于验证 dispatch 管线在 middleware 执行期间不会占用状态锁的测试中间件。
    /// </summary>
    private sealed class BlockingMiddleware(ManualResetEventSlim entered, ManualResetEventSlim release)
        : IStoreMiddleware<CounterState>
    {
        /// <summary>
        ///     通知测试线程 middleware 已进入阻塞点，并等待释放信号后继续执行。
        /// </summary>
        /// <param name="context">当前分发上下文。</param>
        /// <param name="next">后续处理节点。</param>
        public void Invoke(StoreDispatchContext<CounterState> context, Action next)
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(2));
            next();
        }
    }

    /// <summary>
    ///     在中间件阶段尝试二次分发的测试中间件，用于验证重入保护。
    /// </summary>
    private sealed class NestedDispatchMiddleware(Store<CounterState> store) : IStoreMiddleware<CounterState>
    {
        /// <summary>
        ///     标记是否已经触发过一次嵌套分发，避免因测试实现本身导致无限递归。
        /// </summary>
        private bool _hasTriggered;

        /// <summary>
        ///     在第一次进入中间件时执行嵌套分发。
        /// </summary>
        /// <param name="context">当前分发上下文。</param>
        /// <param name="next">后续处理节点。</param>
        public void Invoke(StoreDispatchContext<CounterState> context, Action next)
        {
            if (!_hasTriggered)
            {
                _hasTriggered = true;
                store.Dispatch(new IncrementAction(1));
            }

            next();
        }
    }

    /// <summary>
    ///     用于在回归测试中稳定模拟 <see cref="Type.IsAssignableFrom(Type)"/> 失败的代理类型。
    /// </summary>
    private sealed class ThrowingAssignableType(Type delegatingType, string message) : TypeDelegator(delegatingType)
    {
        /// <summary>
        ///     在 Store 创建 reducer 快照时抛出受控异常，验证 dispatch 作用域能够正确回滚。
        /// </summary>
        /// <param name="typeInfo">待比较的 action 运行时类型。</param>
        /// <returns>此实现不会正常返回。</returns>
        public override bool IsAssignableFrom(Type? typeInfo)
        {
            throw new InvalidOperationException(message);
        }
    }
}

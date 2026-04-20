using System.Reflection;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Extensions;

namespace GFramework.Godot.Coroutine;

/// <summary>
///     Godot 协程管理器，负责在不同的引擎更新阶段驱动 Core 协程调度器。
/// </summary>
/// <remarks>
///     该类型为 Godot 层协程功能的统一入口。
///     它不仅提供静态运行 API，也负责把 Godot 的 Process、Physics 与 Deferred 生命周期映射为
///     <see cref="CoroutineExecutionStage" />，以保证阶段型等待指令的语义真实有效。
/// </remarks>
public partial class Timing : Node
{
    private const string NotInitializedMessage = "Timing not yet initialized (_Ready not executed)";

    private static readonly Timing?[] ActiveInstances = new Timing?[16];
    private static Timing? _instance;
    private Dictionary<CoroutineHandle, OwnedCoroutineRegistration> _ownedCoroutineRegistrations = new();
    private Dictionary<ulong, HashSet<CoroutineHandle>> _ownedCoroutinesByNode = new();
    private GodotTimeSource? _deferredRealtimeTimeSource;
    private CoroutineScheduler? _deferredScheduler;
    private GodotTimeSource? _deferredTimeSource;
    private ushort _frameCounter;
    private byte _instanceId = 1;
    private GodotTimeSource? _physicsRealtimeTimeSource;
    private CoroutineScheduler? _physicsScheduler;
    private GodotTimeSource? _physicsTimeSource;
    private GodotTimeSource? _processIgnorePauseRealtimeTimeSource;
    private CoroutineScheduler? _processIgnorePauseScheduler;
    private GodotTimeSource? _processIgnorePauseTimeSource;
    private GodotTimeSource? _processRealtimeTimeSource;
    private CoroutineScheduler? _processScheduler;
    private GodotTimeSource? _processTimeSource;

    /// <summary>
    ///     获取 Process 调度器。
    /// </summary>
    private CoroutineScheduler ProcessScheduler =>
        _processScheduler ?? throw new InvalidOperationException(NotInitializedMessage);

    /// <summary>
    ///     获取忽略暂停的 Process 调度器。
    /// </summary>
    private CoroutineScheduler ProcessIgnorePauseScheduler =>
        _processIgnorePauseScheduler ?? throw new InvalidOperationException(NotInitializedMessage);

    /// <summary>
    ///     获取 Physics 调度器。
    /// </summary>
    private CoroutineScheduler PhysicsScheduler =>
        _physicsScheduler ?? throw new InvalidOperationException(NotInitializedMessage);

    /// <summary>
    ///     获取 Deferred 调度器。
    /// </summary>
    private CoroutineScheduler DeferredScheduler =>
        _deferredScheduler ?? throw new InvalidOperationException(NotInitializedMessage);

    #region 单例

    /// <summary>
    ///     获取 Timing 单例实例。
    ///     如果实例不存在，则会自动创建并挂载到场景树根节点。
    /// </summary>
    public static Timing Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            var tree = (SceneTree)Engine.GetMainLoop();
            _instance = tree.Root.GetNodeOrNull<Timing>(nameof(Timing));
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new Timing
            {
                Name = nameof(Timing)
            };
            tree.Root.WaitUntilReady(() => tree.Root.AddChild(_instance));
            return _instance;
        }
    }

    #endregion

    private sealed class OwnedCoroutineRegistration
    {
        /// <summary>
        ///     创建一个节点归属协程注册记录。
        /// </summary>
        /// <param name="owner">归属节点。</param>
        /// <param name="ownerId">归属节点的 Godot 实例 ID。</param>
        /// <param name="handle">被管理的协程句柄。</param>
        /// <param name="killCallback">节点退出场景树时触发的终止逻辑。</param>
        public OwnedCoroutineRegistration(Node owner, ulong ownerId, CoroutineHandle handle,
            Action<CoroutineHandle> killCallback)
        {
            Handle = handle;
            Owner = new WeakReference<Node>(owner);
            OwnerId = ownerId;
            OnOwnerTreeExiting = () => killCallback(handle);
        }

        /// <summary>
        ///     获取协程句柄。
        /// </summary>
        public CoroutineHandle Handle { get; }

        /// <summary>
        ///     获取节点弱引用。
        /// </summary>
        public WeakReference<Node> Owner { get; }

        /// <summary>
        ///     获取归属节点 ID。
        /// </summary>
        public ulong OwnerId { get; }

        /// <summary>
        ///     获取节点退出场景树时使用的清理回调。
        /// </summary>
        public Action OnOwnerTreeExiting { get; }
    }

    #region Debug 信息

    /// <summary>
    ///     获取 Process 段活跃协程数量。
    /// </summary>
    public int ProcessCoroutines => _processScheduler?.ActiveCoroutineCount ?? 0;

    /// <summary>
    ///     获取忽略暂停的 Process 段活跃协程数量。
    /// </summary>
    public int ProcessIgnorePauseCoroutines => _processIgnorePauseScheduler?.ActiveCoroutineCount ?? 0;

    /// <summary>
    ///     获取 Physics 段活跃协程数量。
    /// </summary>
    public int PhysicsCoroutines => _physicsScheduler?.ActiveCoroutineCount ?? 0;

    /// <summary>
    ///     获取 Deferred 段活跃协程数量。
    /// </summary>
    public int DeferredCoroutines => _deferredScheduler?.ActiveCoroutineCount ?? 0;

    #endregion

    #region 生命周期

    /// <summary>
    ///     节点就绪时初始化所有调度器与生命周期桥接。
    /// </summary>
    public override void _Ready()
    {
        ProcessPriority = -1;
        ProcessMode = ProcessModeEnum.Always;

        RegisterInstance();
        TrySetPhysicsPriority(-1);
        InitializeSchedulers();
    }

    /// <summary>
    ///     节点退出场景树时清理实例与归属关系。
    /// </summary>
    public override void _ExitTree()
    {
        DetachAllOwnedRegistrations();
        ClearOnInstance();

        if (_instanceId < ActiveInstances.Length)
        {
            ActiveInstances[_instanceId] = null;
        }

        CleanupInstanceIfNecessary(this);
    }

    /// <summary>
    ///     仅在当前实例仍持有共享单例引用时清理它，避免多宿主场景误清其他实例。
    /// </summary>
    /// <param name="instance">正在退出生命周期的实例。</param>
    private static void CleanupInstanceIfNecessary(Timing instance)
    {
        if (ReferenceEquals(_instance, instance))
        {
            _instance = null;
        }
    }

    /// <summary>
    ///     Godot 每帧更新逻辑。
    /// </summary>
    /// <param name="delta">本帧 Process 增量。</param>
    public override void _Process(double delta)
    {
        var paused = GetTree().Paused;

        if (!paused)
        {
            _processScheduler?.Update();
        }

        _processIgnorePauseScheduler?.Update();
        _frameCounter++;
        CallDeferred(nameof(ProcessDeferred));
    }

    /// <summary>
    ///     Godot 物理帧更新逻辑。
    /// </summary>
    /// <param name="delta">本帧 Physics 增量。</param>
    public override void _PhysicsProcess(double delta)
    {
        _physicsScheduler?.Update();
    }

    /// <summary>
    ///     当前帧尾的延迟更新逻辑。
    /// </summary>
    private void ProcessDeferred()
    {
        if (GetTree().Paused)
        {
            return;
        }

        _deferredScheduler?.Update();
    }

    #endregion

    #region 初始化

    /// <summary>
    ///     预热 Timing 单例，以便在业务逻辑首次使用前完成挂载。
    /// </summary>
    public static void Prewarm()
    {
        _ = Instance;
    }

    /// <summary>
    ///     初始化所有调度器和时间源。
    /// </summary>
    private void InitializeSchedulers()
    {
        _processTimeSource = new GodotTimeSource(GetProcessDeltaTime);
        _processRealtimeTimeSource = GodotTimeSource.CreateRealtime();
        _processIgnorePauseTimeSource = new GodotTimeSource(GetProcessDeltaTime);
        _processIgnorePauseRealtimeTimeSource = GodotTimeSource.CreateRealtime();
        _physicsTimeSource = new GodotTimeSource(GetPhysicsProcessDeltaTime);
        _physicsRealtimeTimeSource = GodotTimeSource.CreateRealtime();
        _deferredTimeSource = new GodotTimeSource(GetProcessDeltaTime);
        _deferredRealtimeTimeSource = GodotTimeSource.CreateRealtime();

        _processScheduler = new CoroutineScheduler(
            _processTimeSource,
            _instanceId,
            256,
            false,
            _processRealtimeTimeSource,
            CoroutineExecutionStage.Update);

        _processIgnorePauseScheduler = new CoroutineScheduler(
            _processIgnorePauseTimeSource,
            _instanceId,
            256,
            false,
            _processIgnorePauseRealtimeTimeSource,
            CoroutineExecutionStage.Update);

        _physicsScheduler = new CoroutineScheduler(
            _physicsTimeSource,
            _instanceId,
            128,
            false,
            _physicsRealtimeTimeSource,
            CoroutineExecutionStage.FixedUpdate);

        _deferredScheduler = new CoroutineScheduler(
            _deferredTimeSource,
            _instanceId,
            64,
            false,
            _deferredRealtimeTimeSource,
            CoroutineExecutionStage.EndOfFrame);

        AttachSchedulerLifecycleHandlers(ProcessScheduler);
        AttachSchedulerLifecycleHandlers(ProcessIgnorePauseScheduler);
        AttachSchedulerLifecycleHandlers(PhysicsScheduler);
        AttachSchedulerLifecycleHandlers(DeferredScheduler);
    }

    /// <summary>
    ///     把调度器的完成通知接入 Timing 的节点归属清理逻辑。
    /// </summary>
    /// <param name="scheduler">待桥接的调度器。</param>
    private void AttachSchedulerLifecycleHandlers(CoroutineScheduler scheduler)
    {
        scheduler.OnCoroutineFinished += HandleCoroutineFinished;
    }

    /// <summary>
    ///     注册当前 Timing 实例到实例槽位表中。
    /// </summary>
    private void RegisterInstance()
    {
        if (ActiveInstances[_instanceId] == null)
        {
            ActiveInstances[_instanceId] = this;
            return;
        }

        for (byte i = 1; i < ActiveInstances.Length; i++)
        {
            if (ActiveInstances[i] == null)
            {
                _instanceId = i;
                ActiveInstances[i] = this;
                return;
            }
        }

        throw new OverflowException("最多只能存在 15 个 Timing 实例");
    }

    /// <summary>
    ///     通过反射设置 Physics 处理优先级，兼容不同 Godot 版本的 API 表面。
    /// </summary>
    /// <param name="priority">要设置的优先级。</param>
    private void TrySetPhysicsPriority(int priority)
    {
        try
        {
            typeof(Node)
                .GetProperty(
                    "ProcessPhysicsPriority",
                    BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(this, priority);
        }
        catch
        {
            // ignored
        }
    }

    #endregion

    #region 协程启动 API

    /// <summary>
    ///     运行受场景暂停影响的游戏级协程。
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="tag">协程标签。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle RunGameCoroutine(IEnumerator<IYieldInstruction> coroutine, string? tag = null)
    {
        return RunCoroutine(coroutine, Segment.Process, tag);
    }

    /// <summary>
    ///     运行忽略场景暂停的 UI 级协程。
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="tag">协程标签。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle RunUiCoroutine(IEnumerator<IYieldInstruction> coroutine, string? tag = null)
    {
        return RunCoroutine(coroutine, Segment.ProcessIgnorePause, tag);
    }

    /// <summary>
    ///     在指定段运行协程。
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="segment">协程执行段。</param>
    /// <param name="tag">协程标签。</param>
    /// <param name="cancellationToken">可选取消令牌。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle RunCoroutine(
        IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Instance.RunCoroutineOnInstance(coroutine, segment, tag, cancellationToken);
    }

    /// <summary>
    ///     运行一个显式归属于指定节点的协程。
    /// </summary>
    /// <param name="owner">拥有该协程生命周期的节点。</param>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="segment">协程执行段。</param>
    /// <param name="tag">协程标签。</param>
    /// <param name="cancellationToken">可选取消令牌。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle RunOwnedCoroutine(
        Node owner,
        IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Instance.RunOwnedCoroutineOnInstance(owner, coroutine, segment, tag, cancellationToken);
    }

    /// <summary>
    ///     在当前实例上运行协程。
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="segment">协程执行段。</param>
    /// <param name="tag">协程标签。</param>
    /// <param name="cancellationToken">可选取消令牌。</param>
    /// <returns>新创建的协程句柄。</returns>
    public CoroutineHandle RunCoroutineOnInstance(
        IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        if (coroutine == null)
        {
            return default;
        }

        return GetScheduler(segment).Run(coroutine, tag, group: null, cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     在当前实例上运行归属于指定节点的协程。
    /// </summary>
    /// <param name="owner">拥有该协程的节点。</param>
    /// <param name="coroutine">要运行的协程枚举器。</param>
    /// <param name="segment">协程执行段。</param>
    /// <param name="tag">协程标签。</param>
    /// <param name="cancellationToken">可选取消令牌。</param>
    /// <returns>新创建的协程句柄。</returns>
    public CoroutineHandle RunOwnedCoroutineOnInstance(
        Node? owner,
        IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        if (owner == null || coroutine == null || !IsNodeAlive(owner))
        {
            return default;
        }

        var handle = RunCoroutineOnInstance(
            coroutine.CancelWith(owner),
            segment,
            tag,
            cancellationToken);

        if (!handle.IsValid)
        {
            return handle;
        }

        if (!GetScheduler(segment).IsCoroutineAlive(handle))
        {
            return handle;
        }

        RegisterOwnedCoroutine(owner, handle);
        return handle;
    }

    #endregion

    #region 协程控制 API

    /// <summary>
    ///     暂停指定的协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功暂停则返回 <see langword="true" />。</returns>
    public static bool PauseCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.PauseOnInstance(handle) == true;
    }

    /// <summary>
    ///     恢复指定的协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功恢复则返回 <see langword="true" />。</returns>
    public static bool ResumeCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.ResumeOnInstance(handle) == true;
    }

    /// <summary>
    ///     终止指定的协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功终止则返回 <see langword="true" />。</returns>
    public static bool KillCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.KillOnInstance(handle) == true;
    }

    /// <summary>
    ///     终止某个节点归属的所有协程。
    /// </summary>
    /// <param name="owner">协程归属节点。</param>
    /// <returns>被终止的协程数量。</returns>
    public static int KillCoroutines(Node owner)
    {
        var count = 0;
        foreach (var timing in EnumerateActiveInstances())
        {
            count += timing.KillOwnedCoroutinesOnInstance(owner);
        }

        return count;
    }

    /// <summary>
    ///     终止所有具有指定标签的协程。
    /// </summary>
    /// <param name="tag">协程标签。</param>
    /// <returns>被终止的协程数量。</returns>
    public static int KillCoroutines(string tag)
    {
        return Instance.KillByTagOnInstance(tag);
    }

    /// <summary>
    ///     终止所有协程。
    /// </summary>
    /// <returns>被终止的协程总数。</returns>
    public static int KillAllCoroutines()
    {
        return Instance.ClearOnInstance();
    }

    /// <summary>
    ///     根据协程句柄查询其当前快照。
    /// </summary>
    /// <param name="handle">要查询的协程句柄。</param>
    /// <param name="snapshot">查询成功时返回快照。</param>
    /// <returns>如果找到活跃协程则返回 <see langword="true" />。</returns>
    public static bool TryGetCoroutineSnapshot(CoroutineHandle handle, out CoroutineSnapshot snapshot)
    {
        var instance = GetInstance(handle.Key);
        if (instance == null)
        {
            snapshot = default;
            return false;
        }

        return instance.TryGetSnapshotOnInstance(handle, out snapshot);
    }

    /// <summary>
    ///     获取某个节点当前归属的活跃协程数量。
    /// </summary>
    /// <param name="owner">要查询的节点。</param>
    /// <returns>该节点当前归属的活跃协程数量。</returns>
    public static int GetOwnedCoroutineCount(Node owner)
    {
        var count = 0;
        foreach (var timing in EnumerateActiveInstances())
        {
            count += timing.GetOwnedCoroutineCountOnInstance(owner);
        }

        return count;
    }

    /// <summary>
    ///     在当前实例上暂停协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功暂停则返回 <see langword="true" />。</returns>
    private bool PauseOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Pause(handle)
               || ProcessIgnorePauseScheduler.Pause(handle)
               || PhysicsScheduler.Pause(handle)
               || DeferredScheduler.Pause(handle);
    }

    /// <summary>
    ///     在当前实例上恢复协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功恢复则返回 <see langword="true" />。</returns>
    private bool ResumeOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Resume(handle)
               || ProcessIgnorePauseScheduler.Resume(handle)
               || PhysicsScheduler.Resume(handle)
               || DeferredScheduler.Resume(handle);
    }

    /// <summary>
    ///     在当前实例上终止协程。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <returns>如果成功终止则返回 <see langword="true" />。</returns>
    private bool KillOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Kill(handle)
               || ProcessIgnorePauseScheduler.Kill(handle)
               || PhysicsScheduler.Kill(handle)
               || DeferredScheduler.Kill(handle);
    }

    /// <summary>
    ///     在当前实例上根据标签终止协程。
    /// </summary>
    /// <param name="tag">协程标签。</param>
    /// <returns>被终止的协程数量。</returns>
    private int KillByTagOnInstance(string tag)
    {
        var count = 0;
        count += ProcessScheduler.KillByTag(tag);
        count += ProcessIgnorePauseScheduler.KillByTag(tag);
        count += PhysicsScheduler.KillByTag(tag);
        count += DeferredScheduler.KillByTag(tag);
        return count;
    }

    /// <summary>
    ///     在当前实例上终止某个节点归属的所有协程。
    /// </summary>
    /// <param name="owner">协程归属节点。</param>
    /// <returns>被终止的协程数量。</returns>
    private int KillOwnedCoroutinesOnInstance(Node owner)
    {
        var ownerId = owner.GetInstanceId();
        if (!_ownedCoroutinesByNode.TryGetValue(ownerId, out var handles))
        {
            return 0;
        }

        var count = 0;
        foreach (var handle in handles.ToArray())
        {
            if (KillOnInstance(handle))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    ///     获取某个节点当前归属的活跃协程数量。
    /// </summary>
    /// <param name="owner">要查询的节点。</param>
    /// <returns>活跃归属协程数量。</returns>
    private int GetOwnedCoroutineCountOnInstance(Node owner)
    {
        var ownerId = owner.GetInstanceId();
        return _ownedCoroutinesByNode.TryGetValue(ownerId, out var handles) ? handles.Count : 0;
    }

    /// <summary>
    ///     清空当前实例上的所有协程。
    /// </summary>
    /// <returns>被清除的协程总数。</returns>
    private int ClearOnInstance()
    {
        var count = 0;
        count += ProcessScheduler.Clear();
        count += ProcessIgnorePauseScheduler.Clear();
        count += PhysicsScheduler.Clear();
        count += DeferredScheduler.Clear();
        return count;
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     根据 ID 获取 Timing 实例。
    /// </summary>
    /// <param name="id">实例 ID。</param>
    /// <returns>对应的 Timing 实例；如果不存在则返回 <see langword="null" />。</returns>
    public static Timing? GetInstance(byte id)
    {
        return id < ActiveInstances.Length ? ActiveInstances[id] : null;
    }

    /// <summary>
    ///     枚举所有当前已注册的 Timing 实例。
    /// </summary>
    /// <returns>活跃 Timing 实例序列。</returns>
    private static IEnumerable<Timing> EnumerateActiveInstances()
    {
        return ActiveInstances.Where(static timing => timing is not null).Select(static timing => timing!);
    }

    /// <summary>
    ///     检查节点是否处于有效状态。
    /// </summary>
    /// <param name="node">要检查的节点。</param>
    /// <returns>如果节点存在、实例有效、未进入删除队列且仍在场景树中，则返回 <see langword="true" />。</returns>
    public static bool IsNodeAlive(Node? node)
    {
        return node != null
               && IsInstanceValid(node)
               && !node.IsQueuedForDeletion()
               && node.IsInsideTree();
    }

    /// <summary>
    ///     根据分段选择具体调度器。
    /// </summary>
    /// <param name="segment">目标执行段。</param>
    /// <returns>与分段对应的协程调度器。</returns>
    private CoroutineScheduler GetScheduler(Segment segment)
    {
        return segment switch
        {
            Segment.Process => ProcessScheduler,
            Segment.ProcessIgnorePause => ProcessIgnorePauseScheduler,
            Segment.PhysicsProcess => PhysicsScheduler,
            Segment.DeferredProcess => DeferredScheduler,
            _ => throw new ArgumentOutOfRangeException(nameof(segment), segment, "Unsupported coroutine segment.")
        };
    }

    /// <summary>
    ///     在当前实例上查询指定句柄的快照。
    /// </summary>
    /// <param name="handle">协程句柄。</param>
    /// <param name="snapshot">查询成功时返回快照。</param>
    /// <returns>如果找到活跃协程则返回 <see langword="true" />。</returns>
    private bool TryGetSnapshotOnInstance(CoroutineHandle handle, out CoroutineSnapshot snapshot)
    {
        return ProcessScheduler.TryGetSnapshot(handle, out snapshot)
               || ProcessIgnorePauseScheduler.TryGetSnapshot(handle, out snapshot)
               || PhysicsScheduler.TryGetSnapshot(handle, out snapshot)
               || DeferredScheduler.TryGetSnapshot(handle, out snapshot);
    }

    /// <summary>
    ///     注册节点归属协程，并在节点退树时强制终止该协程。
    /// </summary>
    /// <param name="owner">协程归属节点。</param>
    /// <param name="handle">要登记的协程句柄。</param>
    private void RegisterOwnedCoroutine(Node owner, CoroutineHandle handle)
    {
        var ownerId = owner.GetInstanceId();
        var registration =
            new OwnedCoroutineRegistration(owner, ownerId, handle, ownedHandle => _ = KillOnInstance(ownedHandle));

        _ownedCoroutineRegistrations[handle] = registration;
        if (!_ownedCoroutinesByNode.TryGetValue(ownerId, out var handles))
        {
            handles = new HashSet<CoroutineHandle>();
            _ownedCoroutinesByNode[ownerId] = handles;
        }

        handles.Add(handle);
        owner.TreeExiting += registration.OnOwnerTreeExiting;
    }

    /// <summary>
    ///     在协程结束时解除节点归属回调并清理索引。
    /// </summary>
    /// <param name="handle">已结束的协程句柄。</param>
    /// <param name="status">协程最终状态。</param>
    /// <param name="exception">若失败则为异常对象。</param>
    private void HandleCoroutineFinished(
        CoroutineHandle handle,
        CoroutineCompletionStatus status,
        Exception? exception)
    {
        CleanupOwnedCoroutineRegistration(handle);
    }

    /// <summary>
    ///     清理单个协程对应的节点归属注册。
    /// </summary>
    /// <param name="handle">要清理的协程句柄。</param>
    private void CleanupOwnedCoroutineRegistration(CoroutineHandle handle)
    {
        if (!_ownedCoroutineRegistrations.TryGetValue(handle, out var registration))
        {
            return;
        }

        if (registration.Owner.TryGetTarget(out var owner) && IsInstanceValid(owner))
        {
            owner.TreeExiting -= registration.OnOwnerTreeExiting;
        }

        if (_ownedCoroutinesByNode.TryGetValue(registration.OwnerId, out var handles))
        {
            handles.Remove(handle);
            if (handles.Count == 0)
            {
                _ownedCoroutinesByNode.Remove(registration.OwnerId);
            }
        }

        _ownedCoroutineRegistrations.Remove(handle);
    }

    /// <summary>
    ///     清理所有已登记的节点归属回调。
    /// </summary>
    private void DetachAllOwnedRegistrations()
    {
        foreach (var handle in _ownedCoroutineRegistrations.Keys.ToArray())
        {
            CleanupOwnedCoroutineRegistration(handle);
        }
    }

    #endregion

    #region 延迟调用

    /// <summary>
    ///     延迟执行指定动作。
    /// </summary>
    /// <param name="delay">延迟时间，单位秒。</param>
    /// <param name="action">到期时执行的动作。</param>
    /// <param name="segment">执行所在的协程段。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle CallDelayed(double delay, Action? action, Segment segment = Segment.Process)
    {
        if (action == null)
        {
            return default;
        }

        return RunCoroutine(DelayedCallCoroutine(delay, action), segment);
    }

    /// <summary>
    ///     延迟执行指定动作，并在节点失活时自动放弃执行。
    /// </summary>
    /// <param name="delay">延迟时间，单位秒。</param>
    /// <param name="action">到期时执行的动作。</param>
    /// <param name="cancelWith">用于控制生命周期的节点。</param>
    /// <param name="segment">执行所在的协程段。</param>
    /// <returns>新创建的协程句柄。</returns>
    public static CoroutineHandle CallDelayed(
        double delay,
        Action? action,
        Node cancelWith,
        Segment segment = Segment.Process)
    {
        if (action == null)
        {
            return default;
        }

        return RunOwnedCoroutine(cancelWith, DelayedCallWithCancelCoroutine(delay, action, cancelWith), segment);
    }

    /// <summary>
    ///     延迟调用协程实现。
    /// </summary>
    /// <param name="delay">延迟时间。</param>
    /// <param name="action">要执行的动作。</param>
    /// <returns>可直接交给调度器运行的协程枚举器。</returns>
    private static IEnumerator<IYieldInstruction> DelayedCallCoroutine(double delay, Action action)
    {
        yield return new Delay(delay);
        action();
    }

    /// <summary>
    ///     带节点生命周期判断的延迟调用协程实现。
    /// </summary>
    /// <param name="delay">延迟时间。</param>
    /// <param name="action">要执行的动作。</param>
    /// <param name="cancelWith">生命周期检查节点。</param>
    /// <returns>可直接交给调度器运行的协程枚举器。</returns>
    private static IEnumerator<IYieldInstruction> DelayedCallWithCancelCoroutine(double delay, Action action,
        Node cancelWith)
    {
        yield return new Delay(delay);

        if (IsNodeAlive(cancelWith))
        {
            action();
        }
    }

    #endregion
}

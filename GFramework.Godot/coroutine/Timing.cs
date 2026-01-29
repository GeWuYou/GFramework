using System.Reflection;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.instructions;
using Godot;

namespace GFramework.Godot.coroutine;

/// <summary>
///     Godot协程管理器，提供基于不同更新循环的协程调度功能
///     支持Process、PhysicsProcess和DeferredProcess三种执行段的协程管理
/// </summary>
public partial class Timing : Node
{
    private static Timing? _instance;
    private static readonly Timing?[] ActiveInstances = new Timing?[16];
    private CoroutineScheduler? _deferredScheduler;
    private GodotTimeSource? _deferredTimeSource;
    private ushort _frameCounter;

    private byte _instanceId = 1;
    private CoroutineScheduler? _physicsScheduler;
    private GodotTimeSource? _physicsTimeSource;

    private CoroutineScheduler? _processScheduler;

    private GodotTimeSource? _processTimeSource;

    /// <summary>
    ///     获取Process调度器，如果未初始化则抛出异常
    /// </summary>
    private CoroutineScheduler ProcessScheduler =>
        _processScheduler ?? throw new InvalidOperationException(
            "Timing not yet initialized (_Ready not executed)");

    /// <summary>
    ///     获取Physics调度器，如果未初始化则抛出异常
    /// </summary>
    private CoroutineScheduler PhysicsScheduler =>
        _physicsScheduler ?? throw new InvalidOperationException(
            "Timing not yet initialized (_Ready not executed)");

    /// <summary>
    ///     获取Deferred调度器，如果未初始化则抛出异常
    /// </summary>
    private CoroutineScheduler DeferredScheduler =>
        _deferredScheduler ?? throw new InvalidOperationException(
            "Timing not yet initialized (_Ready not executed)");

    #region 单例

    /// <summary>
    ///     获取Timing单例实例
    ///     如果实例不存在则自动创建并添加到场景树根节点
    /// </summary>
    public static Timing Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            var tree = (SceneTree)Engine.GetMainLoop();
            _instance = tree.Root.GetNodeOrNull<Timing>(nameof(Timing));

            if (_instance == null)
            {
                _instance = new Timing
                {
                    Name = nameof(Timing)
                };
                tree.Root.AddChild(_instance);
            }

            return _instance;
        }
    }

    #endregion

    #region Debug 信息

    /// <summary>
    ///     获取Process段活跃协程数量
    /// </summary>
    public int ProcessCoroutines => _processScheduler?.ActiveCoroutineCount ?? 0;

    /// <summary>
    ///     获取Physics段活跃协程数量
    /// </summary>
    public int PhysicsCoroutines => _physicsScheduler?.ActiveCoroutineCount ?? 0;

    /// <summary>
    ///     获取Deferred段活跃协程数量
    /// </summary>
    public int DeferredCoroutines => _deferredScheduler?.ActiveCoroutineCount ?? 0;

    #endregion

    #region 生命周期

    /// <summary>
    ///     节点就绪时的初始化方法
    ///     设置处理优先级，初始化调度器，并注册实例
    /// </summary>
    public override void _Ready()
    {
        ProcessPriority = -1;

        TrySetPhysicsPriority(-1);

        InitializeSchedulers();
        RegisterInstance();
    }

    /// <summary>
    ///     节点退出场景树时的清理方法
    ///     从活动实例数组中移除当前实例并清理必要资源
    /// </summary>
    public override void _ExitTree()
    {
        if (_instanceId < ActiveInstances.Length)
            ActiveInstances[_instanceId] = null;

        CleanupInstanceIfNecessary();
    }

    /// <summary>
    ///     清理实例引用
    /// </summary>
    private static void CleanupInstanceIfNecessary()
    {
        _instance = null;
    }

    /// <summary>
    ///     每帧处理逻辑
    ///     更新Process调度器，增加帧计数器，并安排延迟处理
    /// </summary>
    /// <param name="delta">时间增量</param>
    public override void _Process(double delta)
    {
        _processScheduler?.Update();
        _frameCounter++;

        CallDeferred(nameof(ProcessDeferred));
    }

    /// <summary>
    ///     物理处理逻辑
    ///     更新Physics调度器
    /// </summary>
    /// <param name="delta">物理时间增量</param>
    public override void _PhysicsProcess(double delta)
    {
        _physicsScheduler?.Update();
    }

    /// <summary>
    ///     延迟处理逻辑
    ///     更新Deferred调度器
    /// </summary>
    private void ProcessDeferred()
    {
        _deferredScheduler?.Update();
    }

    #endregion

    #region 初始化

    /// <summary>
    ///     初始化所有调度器和时间源
    ///     创建Process、Physics和Deferred三个调度器实例
    /// </summary>
    private void InitializeSchedulers()
    {
        _processTimeSource = new GodotTimeSource(GetProcessDeltaTime);
        _physicsTimeSource = new GodotTimeSource(GetPhysicsProcessDeltaTime);
        _deferredTimeSource = new GodotTimeSource(GetProcessDeltaTime);

        _processScheduler = new CoroutineScheduler(
            _processTimeSource,
            _instanceId
        );

        _physicsScheduler = new CoroutineScheduler(
            _physicsTimeSource,
            _instanceId,
            128
        );

        _deferredScheduler = new CoroutineScheduler(
            _deferredTimeSource,
            _instanceId,
            64
        );
    }

    /// <summary>
    ///     注册当前实例到活动实例数组中
    ///     如果当前ID已被占用则寻找可用ID
    /// </summary>
    private void RegisterInstance()
    {
        if (ActiveInstances[_instanceId] == null)
        {
            ActiveInstances[_instanceId] = this;
            return;
        }

        for (byte i = 1; i < ActiveInstances.Length; i++)
            if (ActiveInstances[i] == null)
            {
                _instanceId = i;
                ActiveInstances[i] = this;
                return;
            }

        throw new OverflowException("最多只能存在 15 个 Timing 实例");
    }

    /// <summary>
    ///     尝试设置物理处理优先级
    ///     使用反射方式设置ProcessPhysicsPriority属性
    /// </summary>
    /// <param name="priority">物理处理优先级</param>
    private static void TrySetPhysicsPriority(int priority)
    {
        try
        {
            typeof(Node)
                .GetProperty(
                    "ProcessPhysicsPriority",
                    BindingFlags.Instance |
                    BindingFlags.Public)
                ?.SetValue(Instance, priority);
        }
        catch
        {
            // ignored
        }
    }

    #endregion

    #region 协程启动 API

    /// <summary>
    ///     在指定段运行协程
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器</param>
    /// <param name="segment">协程执行段（Process/PhysicsProcess/DeferredProcess）</param>
    /// <param name="tag">协程标签，用于批量操作</param>
    /// <returns>协程句柄</returns>
    public static CoroutineHandle RunCoroutine(
        IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null)
    {
        return Instance.RunCoroutineOnInstance(coroutine, segment, tag);
    }

    /// <summary>
    ///     在当前实例上运行协程
    ///     根据指定的段选择对应的调度器运行协程
    /// </summary>
    /// <param name="coroutine">要运行的协程枚举器</param>
    /// <param name="segment">协程执行段</param>
    /// <param name="tag">协程标签</param>
    /// <returns>协程句柄</returns>
    public CoroutineHandle RunCoroutineOnInstance(
        IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process,
        string? tag = null)
    {
        if (coroutine == null)
            return default;

        return segment switch
        {
            Segment.Process => ProcessScheduler.Run(coroutine, tag),
            Segment.PhysicsProcess => PhysicsScheduler.Run(coroutine, tag),
            Segment.DeferredProcess => DeferredScheduler.Run(coroutine, tag),
            _ => default
        };
    }

    #endregion

    #region 协程控制 API

    /// <summary>
    ///     暂停指定的协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功暂停</returns>
    public static bool PauseCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.PauseOnInstance(handle) ?? false;
    }

    /// <summary>
    ///     恢复指定的协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功恢复</returns>
    public static bool ResumeCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.ResumeOnInstance(handle) ?? false;
    }

    /// <summary>
    ///     终止指定的协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功终止</returns>
    public static bool KillCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.KillOnInstance(handle) ?? false;
    }

    /// <summary>
    ///     终止所有具有指定标签的协程
    /// </summary>
    /// <param name="tag">协程标签</param>
    /// <returns>被终止的协程数量</returns>
    public static int KillCoroutines(string tag)
    {
        return Instance.KillByTagOnInstance(tag);
    }

    /// <summary>
    ///     终止所有协程
    /// </summary>
    /// <returns>被终止的协程总数</returns>
    public static int KillAllCoroutines()
    {
        return Instance.ClearOnInstance();
    }

    /// <summary>
    ///     在当前实例上暂停协程
    ///     尝试在所有调度器中查找并暂停指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功暂停</returns>
    private bool PauseOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Pause(handle)
               || PhysicsScheduler.Pause(handle)
               || DeferredScheduler.Pause(handle);
    }

    /// <summary>
    ///     在当前实例上恢复协程
    ///     尝试在所有调度器中查找并恢复指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功恢复</returns>
    private bool ResumeOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Resume(handle)
               || PhysicsScheduler.Resume(handle)
               || DeferredScheduler.Resume(handle);
    }

    /// <summary>
    ///     在当前实例上终止协程
    ///     尝试在所有调度器中查找并终止指定协程
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>是否成功终止</returns>
    private bool KillOnInstance(CoroutineHandle handle)
    {
        return ProcessScheduler.Kill(handle)
               || PhysicsScheduler.Kill(handle)
               || DeferredScheduler.Kill(handle);
    }

    /// <summary>
    ///     在当前实例上根据标签终止协程
    ///     在所有调度器中查找并终止具有指定标签的协程
    /// </summary>
    /// <param name="tag">协程标签</param>
    /// <returns>被终止的协程数量</returns>
    private int KillByTagOnInstance(string tag)
    {
        var count = 0;
        count += ProcessScheduler.KillByTag(tag);
        count += PhysicsScheduler.KillByTag(tag);
        count += DeferredScheduler.KillByTag(tag);
        return count;
    }

    /// <summary>
    ///     清空当前实例上的所有协程
    ///     从所有调度器中清除协程
    /// </summary>
    /// <returns>被清除的协程总数</returns>
    private int ClearOnInstance()
    {
        var count = 0;
        count += ProcessScheduler.Clear();
        count += PhysicsScheduler.Clear();
        count += DeferredScheduler.Clear();
        return count;
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     根据ID获取Timing实例
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <returns>对应的Timing实例或null</returns>
    public static Timing? GetInstance(byte id)
    {
        return id < ActiveInstances.Length ? ActiveInstances[id] : null;
    }


    /// <summary>
    ///     检查节点是否处于有效状态
    /// </summary>
    /// <param name="node">要检查的节点</param>
    /// <returns>如果节点存在且有效则返回true，否则返回false</returns>
    public static bool IsNodeAlive(Node? node)
    {
        // 验证节点是否存在、实例是否有效、未被标记为删除且在场景树中
        return node != null
               && IsInstanceValid(node)
               && !node.IsQueuedForDeletion()
               && node.IsInsideTree();
    }

    #endregion

    #region 延迟调用

    /// <summary>
    ///     延迟调用指定动作
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="segment">执行段</param>
    /// <returns>协程句柄</returns>
    public static CoroutineHandle CallDelayed(
        double delay,
        Action? action,
        Segment segment = Segment.Process)
    {
        if (action == null)
            return default;

        return RunCoroutine(DelayedCallCoroutine(delay, action), segment);
    }

    /// <summary>
    ///     延迟调用指定动作，支持取消条件
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="cancelWith">取消条件节点</param>
    /// <param name="segment">执行段</param>
    /// <returns>协程句柄</returns>
    public static CoroutineHandle CallDelayed(
        double delay,
        Action? action,
        Node cancelWith,
        Segment segment = Segment.Process)
    {
        if (action == null)
            return default;

        return RunCoroutine(
            DelayedCallWithCancelCoroutine(delay, action, cancelWith),
            segment);
    }

    /// <summary>
    ///     延迟调用协程实现
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <param name="action">要执行的动作</param>
    /// <returns>协程枚举器</returns>
    private static IEnumerator<IYieldInstruction> DelayedCallCoroutine(
        double delay,
        Action action)
    {
        yield return new Delay(delay);
        action();
    }

    /// <summary>
    ///     带取消条件的延迟调用协程实现
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="cancelWith">取消条件节点</param>
    /// <returns>协程枚举器</returns>
    private static IEnumerator<IYieldInstruction> DelayedCallWithCancelCoroutine(
        double delay,
        Action action,
        Node cancelWith)
    {
        yield return new Delay(delay);

        if (IsNodeAlive(cancelWith))
            action();
    }

    #endregion
}
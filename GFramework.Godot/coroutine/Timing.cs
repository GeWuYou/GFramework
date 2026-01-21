using System.Reflection;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using Godot;

namespace GFramework.Godot.coroutine;

public partial class Timing : Node
{
    private static Timing? _instance;
    private static readonly Timing?[] ActiveInstances = new Timing?[16];
    private CoroutineScheduler _deferredScheduler;
    private GodotTimeSource? _deferredTimeSource;
    private ushort _frameCounter;

    private byte _instanceId = 1;
    private CoroutineScheduler _physicsScheduler;
    private GodotTimeSource? _physicsTimeSource;

    private CoroutineScheduler _processScheduler;

    private GodotTimeSource? _processTimeSource;

    #region 单例

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

    public int ProcessCoroutines => _processScheduler?.ActiveCoroutineCount ?? 0;

    public int PhysicsCoroutines => _physicsScheduler?.ActiveCoroutineCount ?? 0;

    public int DeferredCoroutines => _deferredScheduler?.ActiveCoroutineCount ?? 0;

    #endregion

    #region 生命周期

    public override void _Ready()
    {
        ProcessPriority = -1;

        TrySetPhysicsPriority(-1);

        InitializeSchedulers();
        RegisterInstance();
    }

    public override void _ExitTree()
    {
        if (_instanceId < ActiveInstances.Length)
            ActiveInstances[_instanceId] = null;

        CleanupInstanceIfNecessary();
    }

    private static void CleanupInstanceIfNecessary()
    {
        _instance = null;
    }

    public override void _Process(double delta)
    {
        _processScheduler?.Update();
        _frameCounter++;

        CallDeferred(nameof(ProcessDeferred));
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsScheduler?.Update();
    }

    private void ProcessDeferred()
    {
        _deferredScheduler?.Update();
    }

    #endregion

    #region 初始化

    private void InitializeSchedulers()
    {
        _processTimeSource = new GodotTimeSource(GetProcessDeltaTime);
        _physicsTimeSource = new GodotTimeSource(GetPhysicsProcessDeltaTime);
        _deferredTimeSource = new GodotTimeSource(GetProcessDeltaTime);

        _processScheduler = new CoroutineScheduler(
            _processTimeSource,
            _instanceId,
            initialCapacity: 256
        );

        _physicsScheduler = new CoroutineScheduler(
            _physicsTimeSource,
            _instanceId,
            initialCapacity: 128
        );

        _deferredScheduler = new CoroutineScheduler(
            _deferredTimeSource,
            _instanceId,
            initialCapacity: 64
        );
    }

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

    public static CoroutineHandle RunCoroutine(
        IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null)
    {
        return Instance.RunCoroutineOnInstance(coroutine, segment, tag);
    }

    public CoroutineHandle RunCoroutineOnInstance(
        IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process,
        string? tag = null)
    {
        if (coroutine == null)
            return default;

        return segment switch
        {
            Segment.Process => _processScheduler.Run(coroutine, tag),
            Segment.PhysicsProcess => _physicsScheduler.Run(coroutine, tag),
            Segment.DeferredProcess => _deferredScheduler.Run(coroutine, tag),
            _ => default
        };
    }

    #endregion

    #region 协程控制 API

    public static bool PauseCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.PauseOnInstance(handle) ?? false;
    }

    public static bool ResumeCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.ResumeOnInstance(handle) ?? false;
    }

    public static bool KillCoroutine(CoroutineHandle handle)
    {
        return GetInstance(handle.Key)?.KillOnInstance(handle) ?? false;
    }

    public static int KillCoroutines(string tag)
    {
        return Instance.KillByTagOnInstance(tag);
    }

    public static int KillAllCoroutines()
    {
        return Instance.ClearOnInstance();
    }

    private bool PauseOnInstance(CoroutineHandle handle)
    {
        return _processScheduler.Pause(handle)
               || _physicsScheduler.Pause(handle)
               || _deferredScheduler.Pause(handle);
    }

    private bool ResumeOnInstance(CoroutineHandle handle)
    {
        return _processScheduler.Resume(handle)
               || _physicsScheduler.Resume(handle)
               || _deferredScheduler.Resume(handle);
    }

    private bool KillOnInstance(CoroutineHandle handle)
    {
        return _processScheduler.Kill(handle)
               || _physicsScheduler.Kill(handle)
               || _deferredScheduler.Kill(handle);
    }

    private int KillByTagOnInstance(string tag)
    {
        int count = 0;
        count += _processScheduler.KillByTag(tag);
        count += _physicsScheduler.KillByTag(tag);
        count += _deferredScheduler.KillByTag(tag);
        return count;
    }

    private int ClearOnInstance()
    {
        int count = 0;
        count += _processScheduler.Clear();
        count += _physicsScheduler.Clear();
        count += _deferredScheduler.Clear();
        return count;
    }

    #endregion

    #region 工具方法

    public static Timing? GetInstance(byte id)
    {
        return id < ActiveInstances.Length ? ActiveInstances[id] : null;
    }

    /// <summary>
    /// 创建等待指定秒数的指令
    /// </summary>
    public static Delay WaitForSeconds(double seconds)
    {
        return new Delay(seconds);
    }

    /// <summary>
    /// 创建等待一帧的指令
    /// </summary>
    public static WaitOneFrame WaitForOneFrame()
    {
        return new WaitOneFrame();
    }

    /// <summary>
    /// 创建等待指定帧数的指令
    /// </summary>
    public static WaitForFrames WaitForFrames(int frames)
    {
        return new WaitForFrames(frames);
    }

    /// <summary>
    /// 创建等待直到条件满足的指令
    /// </summary>
    public static WaitUntil WaitUntil(Func<bool> predicate)
    {
        return new WaitUntil(predicate);
    }

    /// <summary>
    /// 创建等待当条件为真时持续等待的指令
    /// </summary>
    public static WaitWhile WaitWhile(Func<bool> predicate)
    {
        return new WaitWhile(predicate);
    }

    public static bool IsNodeAlive(Node? node)
    {
        return node != null
               && IsInstanceValid(node)
               && !node.IsQueuedForDeletion()
               && node.IsInsideTree();
    }

    #endregion

    #region 延迟调用

    public static CoroutineHandle CallDelayed(
        double delay,
        Action? action,
        Segment segment = Segment.Process)
    {
        if (action == null)
            return default;

        return RunCoroutine(DelayedCallCoroutine(delay, action), segment);
    }

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

    private static IEnumerator<IYieldInstruction> DelayedCallCoroutine(
        double delay,
        Action action)
    {
        yield return new Delay(delay);
        action();
    }

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
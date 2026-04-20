using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;

namespace GFramework.Godot.Coroutine;

public partial class Timing
{
    /// <summary>
    ///     使用可控时间源初始化当前 <see cref="Timing" /> 实例，供纯托管测试验证宿主阶段语义。
    /// </summary>
    /// <param name="processDeltaProvider">`Process` 段的增量提供器。</param>
    /// <param name="physicsDeltaProvider">`PhysicsProcess` 段的增量提供器。</param>
    /// <param name="deferredDeltaProvider">`DeferredProcess` 段的增量提供器。</param>
    /// <remarks>
    ///     该入口只用于测试宿主驱动顺序，不会挂接真实场景树，也不会暴露给运行时调用方。
    ///     由于协程句柄包含实例槽位前缀，这里仍会注册实例槽位，便于沿用生产代码的查询与控制路径。
    /// </remarks>
    internal void InitializeForTests(
        Func<double>? processDeltaProvider = null,
        Func<double>? physicsDeltaProvider = null,
        Func<double>? deferredDeltaProvider = null)
    {
        _instanceId = 1;
        _ownedCoroutineRegistrations ??= new Dictionary<CoroutineHandle, OwnedCoroutineRegistration>();
        _ownedCoroutinesByNode ??= new Dictionary<ulong, HashSet<CoroutineHandle>>();

        RegisterInstance();

        _processTimeSource = new GodotTimeSource(processDeltaProvider ?? DefaultDeltaProvider);
        _processRealtimeTimeSource = new GodotTimeSource(processDeltaProvider ?? DefaultDeltaProvider);
        _processIgnorePauseTimeSource = new GodotTimeSource(processDeltaProvider ?? DefaultDeltaProvider);
        _processIgnorePauseRealtimeTimeSource = new GodotTimeSource(processDeltaProvider ?? DefaultDeltaProvider);
        _physicsTimeSource = new GodotTimeSource(physicsDeltaProvider ?? DefaultDeltaProvider);
        _physicsRealtimeTimeSource = new GodotTimeSource(physicsDeltaProvider ?? DefaultDeltaProvider);
        _deferredTimeSource = new GodotTimeSource(deferredDeltaProvider ?? processDeltaProvider ?? DefaultDeltaProvider);
        _deferredRealtimeTimeSource =
            new GodotTimeSource(deferredDeltaProvider ?? processDeltaProvider ?? DefaultDeltaProvider);

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
    ///     以测试宿主的方式推进一次 Process 帧。
    /// </summary>
    /// <param name="paused">
    ///     指示当前帧是否视为场景暂停。
    ///     暂停时仅推进 `ProcessIgnorePause` 段，并跳过 `DeferredProcess`，以匹配生产宿主逻辑。
    /// </param>
    internal void AdvanceProcessFrameForTests(bool paused)
    {
        if (!paused)
        {
            _processScheduler?.Update();
        }

        _processIgnorePauseScheduler?.Update();
        _frameCounter++;

        if (!paused)
        {
            _deferredScheduler?.Update();
        }
    }

    /// <summary>
    ///     以测试宿主的方式推进一次 Physics 帧。
    /// </summary>
    internal void AdvancePhysicsFrameForTests()
    {
        _physicsScheduler?.Update();
    }

    /// <summary>
    ///     获取指定分段对应的调度器，供测试读取完成状态与快照。
    /// </summary>
    /// <param name="segment">目标分段。</param>
    /// <returns>对应分段的调度器实例。</returns>
    internal CoroutineScheduler GetSchedulerForTests(Segment segment)
    {
        return GetScheduler(segment);
    }

    /// <summary>
    ///     清理测试初始化留下的实例槽位与调度器状态，避免跨测试污染静态单例表。
    ///     仅当当前测试宿主仍持有共享单例引用时才会清理 `_instance`，以免误伤同进程内的其他宿主。
    /// </summary>
    internal void DisposeForTests()
    {
        DetachAllOwnedRegistrations();
        ClearOnInstance();

        if (_instanceId < ActiveInstances.Length)
        {
            ActiveInstances[_instanceId] = null;
        }

        CleanupInstanceIfNecessary(this);

        _processScheduler = null;
        _processIgnorePauseScheduler = null;
        _physicsScheduler = null;
        _deferredScheduler = null;
        _processTimeSource = null;
        _processRealtimeTimeSource = null;
        _processIgnorePauseTimeSource = null;
        _processIgnorePauseRealtimeTimeSource = null;
        _physicsTimeSource = null;
        _physicsRealtimeTimeSource = null;
        _deferredTimeSource = null;
        _deferredRealtimeTimeSource = null;
        _frameCounter = 0;
        _instanceId = 1;
    }

    /// <summary>
    ///     提供测试默认使用的稳定帧增量。
    /// </summary>
    /// <returns>固定的 60 FPS 增量。</returns>
    private static double DefaultDeltaProvider()
    {
        return 1.0 / 60.0;
    }
}

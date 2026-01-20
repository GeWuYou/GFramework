using GFramework.Core.system;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 协程系统类，负责管理和更新协程调度器
/// </summary>
/// <param name="scheduler">协程调度器实例</param>
public class CoroutineSystem(CoroutineScheduler scheduler) : AbstractSystem, ICoroutineSystem
{
    /// <summary>
    /// 更新协程系统，驱动协程调度器执行协程逻辑
    /// </summary>
    /// <param name="deltaTime">时间间隔，表示自上一帧以来经过的时间（秒）</param>
    public void OnUpdate(float deltaTime)
    {
        // 更新协程调度器，处理等待中的协程
        scheduler.Update(deltaTime);
    }

    /// <summary>
    /// 初始化协程系统
    /// </summary>
    protected override void OnInit()
    {
    }
}
namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程调度器接口，用于管理和执行协程任务
/// </summary>
public interface ICoroutineScheduler
{
    /// <summary>
    /// 获取当前活跃的协程数量
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// 更新协程调度器，处理当前帧需要执行的协程任务
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔（以秒为单位）</param>
    void Update(float deltaTime);
}
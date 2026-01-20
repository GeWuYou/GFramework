namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程调度器接口，定义了协程系统的基本调度方法
/// </summary>
public interface ICoroutineScheduler
{
    /// <summary>
    /// 更新协程调度器，处理等待中的协程
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔（以秒为单位）</param>
    void Update(float deltaTime);
}
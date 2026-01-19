namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程调度器接口，定义协程调度和管理的核心功能
/// </summary>
/// <remarks>
///     协程调度器负责协程的生命周期管理、优先级调度和时间驱动
///     实现此接口的类应该作为ISystem注册到架构中
/// </remarks>
public interface ICoroutineScheduler
{
    /// <summary>
    ///     获取当前活跃的协程数量
    /// </summary>
    int ActiveCoroutineCount { get; }

    /// <summary>
    ///     获取自启动以来的总协程数量
    /// </summary>
    int TotalCoroutineCount { get; }

    /// <summary>
    ///     创建一个新的协程作用域
    /// </summary>
    /// <param name="name">作用域名称</param>
    /// <param name="priority">作用域内协程的默认优先级</param>
    /// <returns>新创建的协程作用域</returns>
    ICoroutineScope CreateScope(string name, CoroutinePriority priority = CoroutinePriority.Normal);

    /// <summary>
    ///     创建一个新的协程分组
    /// </summary>
    /// <param name="name">分组名称</param>
    /// <param name="priority">分组内协程的默认优先级</param>
    /// <returns>新创建的协程分组</returns>
    ICoroutineGroup CreateGroup(string name, CoroutinePriority priority = CoroutinePriority.Normal);

    /// <summary>
    ///     更新所有协程，执行一帧的协程逻辑
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    void UpdateCoroutines(float deltaTime);
}
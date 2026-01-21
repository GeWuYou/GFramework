namespace GFramework.Core.Abstractions.coroutine;

/// <summary>
/// 定义一个可等待指令的接口，用于协程系统中的异步操作控制
/// </summary>
public interface IYieldInstruction
{
    /// <summary>
    /// 获取当前等待指令是否已完成执行
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    /// 每帧由调度器调用，用于更新当前等待指令的状态
    /// </summary>
    /// <param name="deltaTime">自上一帧以来的时间间隔（以秒为单位）</param>
    void Update(double deltaTime);
}
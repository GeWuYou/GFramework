namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 表示一个可等待的指令接口，用于协程中的等待操作
/// </summary>
public interface IYieldInstruction
{
    /// <summary>
    /// 获取当前等待指令是否已完成
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    /// 更新等待指令的状态
    /// </summary>
    /// <param name="deltaTime">自上次更新以来的时间间隔（以秒为单位）</param>
    void Update(float deltaTime);
}
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 表示一个等待指定秒数的时间延迟指令
/// </summary>
/// <param name="seconds">需要等待的秒数</param>
public class WaitForSeconds(float seconds) : IYieldInstruction
{
    private float _elapsed;

    /// <summary>
    /// 获取当前等待是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新时间进度
    /// </summary>
    /// <param name="deltaTime">自上次更新以来经过的时间（秒）</param>
    public void Update(float deltaTime)
    {
        if (IsDone) return;
        _elapsed += deltaTime;
        if (_elapsed >= seconds) IsDone = true;
    }

    /// <summary>
    /// 重置等待状态到初始状态
    /// </summary>
    public void Reset()
    {
        _elapsed = 0;
        IsDone = false;
    }
}
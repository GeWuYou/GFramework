using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 表示一个等待指定秒数的时间延迟指令
/// </summary>
/// <param name="seconds">需要等待的秒数</param>
public class WaitForSeconds(float seconds) : IYieldInstruction
{
    private float _elapsed;
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新时间进度，当累计时间达到指定秒数时标记完成
    /// </summary>
    /// <param name="deltaTime">自上次更新以来经过的时间（秒）</param>
    public void Update(float deltaTime)
    {
        if (IsDone) return;
        _elapsed += deltaTime;
        if (_elapsed >= seconds) IsDone = true;
    }
}
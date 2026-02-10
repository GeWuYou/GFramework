using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     带超时的条件等待指令
///     当条件满足或超时时间到达时完成
/// </summary>
/// <param name="predicate">条件判断函数</param>
/// <param name="timeoutSeconds">超时时间（秒）</param>
public sealed class WaitUntilOrTimeout(Func<bool> predicate, double timeoutSeconds) : IYieldInstruction
{
    private readonly Func<bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    private readonly double _timeout = Math.Max(0, timeoutSeconds);
    private double _elapsedTime;

    /// <summary>
    ///     获取是否因条件满足而完成
    /// </summary>
    public bool ConditionMet => _predicate();

    /// <summary>
    ///     获取是否因超时而完成
    /// </summary>
    public bool IsTimedOut => _elapsedTime >= _timeout;

    /// <summary>
    ///     更新方法，累计时间和检查条件
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        _elapsedTime += deltaTime;
    }

    /// <summary>
    ///     获取等待是否已完成（条件满足或超时）
    /// </summary>
    public bool IsDone => ConditionMet || IsTimedOut;
}
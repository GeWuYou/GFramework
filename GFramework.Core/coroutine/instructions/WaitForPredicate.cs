using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     通用谓词等待指令
///     支持自定义比较逻辑，可以替代 WaitUntil 和 WaitWhile
/// </summary>
/// <param name="predicate">条件判断函数</param>
/// <param name="waitForTrue">true表示等待条件为真时完成，false表示等待条件为假时完成</param>
public sealed class WaitForPredicate(Func<bool> predicate, bool waitForTrue = true) : IYieldInstruction
{
    private readonly Func<bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <summary>
    ///     更新协程状态
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 不需要特殊处理时间
    }

    /// <summary>
    ///     获取协程指令是否已完成
    /// </summary>
    public bool IsDone => waitForTrue ? _predicate() : !_predicate();
}
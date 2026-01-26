using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 表示一个等待直到指定条件满足的协程指令
/// </summary>
/// <param name="predicate">用于判断条件是否满足的函数委托</param>
public sealed class WaitUntil(Func<bool> predicate) : IYieldInstruction
{
    private readonly Func<bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <summary>
    /// 更新协程状态（此实现中不需要处理时间）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 不需要时间
    }

    /// <summary>
    /// 获取协程指令是否已完成
    /// </summary>
    public bool IsDone => _predicate();
}
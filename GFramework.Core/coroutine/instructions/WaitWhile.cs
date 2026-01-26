using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 表示一个等待条件为假时才完成的协程指令
/// </summary>
/// <param name="predicate">用于判断是否继续等待的条件函数，当返回true时继续等待，返回false时完成</param>
public sealed class WaitWhile(Func<bool> predicate) : IYieldInstruction
{
    private readonly Func<bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <summary>
    /// 更新协程状态（此实现中为空方法）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
    }

    /// <summary>
    /// 获取协程指令是否已完成
    /// 当谓词函数返回false时，表示条件不再满足，指令完成
    /// </summary>
    public bool IsDone => !_predicate();
}
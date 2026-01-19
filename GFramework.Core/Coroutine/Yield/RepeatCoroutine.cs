using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     重复执行协程的yield指令
/// </summary>
/// <remarks>
///     该指令会重复执行指定的协程，直到达到指定的重复次数或满足某个条件
///     常用于需要重复执行某项任务的情况
/// </remarks>
public class RepeatCoroutine : IYieldInstruction
{
    private readonly int? _repeatCount;
    private readonly IEnumerator _routine;
    private readonly ICoroutineScope _scope;
    private readonly Func<bool>? _untilCondition;
    private ICoroutineHandle? _currentHandle;
    private int _executedCount;

    /// <summary>
    ///     初始化RepeatCoroutine指令的新实例（按次数重复）
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routine">要重复执行的协程</param>
    /// <param name="repeatCount">重复次数</param>
    /// <exception cref="System.ArgumentNullException">当scope或routine为null时抛出</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">当repeatCount小于1时抛出</exception>
    public RepeatCoroutine(ICoroutineScope scope, IEnumerator routine, int repeatCount)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _routine = routine ?? throw new ArgumentNullException(nameof(routine));

        if (repeatCount < 1)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Repeat count must be at least 1");

        _repeatCount = repeatCount;
        _untilCondition = null;
        _currentHandle = null;
        _executedCount = 0;
        IsDone = false;
    }

    /// <summary>
    ///     初始化RepeatCoroutine指令的新实例（按条件重复）
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routine">要重复执行的协程</param>
    /// <param name="untilCondition">重复终止条件，返回true时停止重复</param>
    /// <exception cref="System.ArgumentNullException">当scope、routine或untilCondition为null时抛出</exception>
    public RepeatCoroutine(ICoroutineScope scope, IEnumerator routine, Func<bool> untilCondition)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _routine = routine ?? throw new ArgumentNullException(nameof(routine));
        _untilCondition = untilCondition ?? throw new ArgumentNullException(nameof(untilCondition));

        _repeatCount = null;
        _currentHandle = null;
        _executedCount = 0;
        IsDone = false;
    }

    /// <summary>
    ///     获取指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    ///     更新指令状态
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    public void Update(float deltaTime)
    {
        if (IsDone) return;

        // 检查终止条件
        if (_untilCondition != null && _untilCondition())
        {
            IsDone = true;
            return;
        }

        // 检查重复次数
        if (_repeatCount.HasValue && _executedCount >= _repeatCount.Value)
        {
            IsDone = true;
            return;
        }

        // 如果没有当前协程或当前协程已完成，启动下一次执行
        if (_currentHandle == null || _currentHandle.IsDone)
        {
            if (!ShouldContinue())
            {
                IsDone = true;
                return;
            }

            _currentHandle = _scope.Launch(_routine);
            _executedCount++;

            // 注册完成事件
            _currentHandle.OnComplete += h =>
            {
                if (!ShouldContinue() && h.IsDone)
                {
                    IsDone = true;
                }
            };
        }
    }

    /// <summary>
    ///     判断是否应该继续重复
    /// </summary>
    /// <returns>如果应该继续返回true，否则返回false</returns>
    private bool ShouldContinue()
    {
        // 检查终止条件
        if (_untilCondition != null && _untilCondition())
        {
            return false;
        }

        // 检查重复次数
        if (_repeatCount.HasValue && _executedCount >= _repeatCount.Value)
        {
            return false;
        }

        return true;
    }
}
using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 等待Task完成的等待指令
/// </summary>
/// <summary>
/// 等待Task完成的等待指令
/// </summary>
public sealed class WaitForTask : IYieldInstruction
{
    private readonly Task _task;
    private volatile bool _done;

    /// <summary>
    /// 初始化等待Task的指令
    /// </summary>
    /// <param name="task">要等待完成的Task</param>
    public WaitForTask(Task task)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        
        // 检查Task是否已经完成
        if (_task.IsCompleted)
        {
            _done = true;
        }
        else
        {
            // 注册完成回调
            _task.ContinueWith(_ =>
            {
                _done = true;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    /// <summary>
    /// 更新方法，用于处理时间更新逻辑
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // Task的完成由ContinueWith回调设置
    }

    /// <summary>
    /// 获取等待是否已完成
    /// </summary>
    public bool IsDone => _done;

    /// <summary>
    /// 获取Task的异常（如果有）
    /// </summary>
    public Exception? Exception => _task.Exception;
}
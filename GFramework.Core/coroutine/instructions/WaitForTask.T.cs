using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 等待泛型Task完成的等待指令
/// </summary>
/// <typeparam name="T">Task返回值的类型</typeparam>
public sealed class WaitForTask<T> : IYieldInstruction
{
    private readonly Task<T> _task;
    private volatile bool _done;

    /// <summary>
    /// 初始化等待泛型Task的指令
    /// </summary>
    /// <param name="task">要等待完成的Task</param>
    public WaitForTask(Task<T> task)
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
    /// 获取Task的结果值
    /// </summary>
    public T Result => _task.GetAwaiter().GetResult();

    /// <summary>
    /// 获取Task的异常（如果有）
    /// </summary>
    public Exception? Exception => _task.Exception;
}
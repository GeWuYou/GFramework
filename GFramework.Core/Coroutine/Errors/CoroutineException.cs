namespace GFramework.Core.Coroutine.Errors;

/// <summary>
///     协程异常类，表示协程执行过程中发生的异常
/// </summary>
public class CoroutineException : Exception
{
    /// <summary>
    ///     初始化协程异常的新实例
    /// </summary>
    public CoroutineException()
        : base("Coroutine execution failed")
    {
    }

    /// <summary>
    ///     初始化协程异常的新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    public CoroutineException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     初始化协程异常的新实例
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public CoroutineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
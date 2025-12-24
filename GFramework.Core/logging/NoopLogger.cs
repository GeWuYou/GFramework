namespace GFramework.Core.logging;

/// <summary>
/// 空操作日志记录器实现，不执行任何实际的日志记录操作
/// </summary>
internal sealed class NoopLogger : ILogger
{
    /// <summary>
    /// 获取日志记录器的名称
    /// </summary>
    /// <returns>返回"NOOP"字符串</returns>
    public string Name()
    {
        return "NOOP";
    }

    /// <summary>
    /// 检查是否启用了跟踪级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsTraceEnabled()
    {
        return false;
    }

    /// <summary>
    /// 检查是否启用了调试级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsDebugEnabled()
    {
        return false;
    }

    /// <summary>
    /// 检查是否启用了信息级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsInfoEnabled()
    {
        return false;
    }

    /// <summary>
    /// 检查是否启用了警告级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsWarnEnabled()
    {
        return false;
    }

    /// <summary>
    /// 检查是否启用了错误级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsErrorEnabled()
    {
        return false;
    }

    /// <summary>
    /// 检查是否启用了致命错误级别日志
    /// </summary>
    /// <returns>始终返回false</returns>
    public bool IsFatalEnabled()
    {
        return false;
    }
    

    /// <summary>
    /// 记录跟踪级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Trace(string msg)
    {
        
    }

    /// <summary>
    /// 记录跟踪级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Trace(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录跟踪级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Trace(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录跟踪级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Trace(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录跟踪级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Trace(string msg, Exception t)
    {
        
    }

    /// <summary>
    /// 记录调试级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Debug(string msg)
    {
        
    }

    /// <summary>
    /// 记录调试级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Debug(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录调试级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Debug(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录调试级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Debug(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录调试级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Debug(string msg, Exception t)
    {
        
    }

    /// <summary>
    /// 记录信息级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Info(string msg)
    {
        
    }

    /// <summary>
    /// 记录信息级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Info(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录信息级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Info(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录信息级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Info(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录信息级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Info(string msg, Exception t)
    {
        
    }

    /// <summary>
    /// 记录警告级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Warn(string msg)
    {
        
    }

    /// <summary>
    /// 记录警告级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Warn(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录警告级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Warn(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录警告级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Warn(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录警告级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Warn(string msg, Exception t)
    {
        
    }

    /// <summary>
    /// 记录错误级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Error(string msg)
    {
        
    }

    /// <summary>
    /// 记录错误级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Error(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录错误级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Error(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录错误级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Error(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录错误级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Error(string msg, Exception t)
    {
        
    }

    /// <summary>
    /// 记录致命错误级别日志消息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    public void Fatal(string msg)
    {
        
    }

    /// <summary>
    /// 记录致命错误级别日志消息，支持格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg">格式化参数</param>
    public void Fatal(string format, object arg)
    {
        
    }

    /// <summary>
    /// 记录致命错误级别日志消息，支持两个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arg1">第一个格式化参数</param>
    /// <param name="arg2">第二个格式化参数</param>
    public void Fatal(string format, object arg1, object arg2)
    {
        
    }

    /// <summary>
    /// 记录致命错误级别日志消息，支持多个格式化参数
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="arguments">格式化参数数组</param>
    public void Fatal(string format, params object[] arguments)
    {
        
    }

    /// <summary>
    /// 记录致命错误级别日志消息和异常信息
    /// </summary>
    /// <param name="msg">要记录的消息</param>
    /// <param name="t">要记录的异常</param>
    public void Fatal(string msg, Exception t)
    {
        
    }
}

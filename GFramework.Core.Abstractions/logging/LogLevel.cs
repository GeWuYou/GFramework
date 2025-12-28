namespace GFramework.Core.Abstractions.logging;

/// <summary>
///     定义日志级别的枚举，用于标识不同严重程度的日志消息
/// </summary>
public enum LogLevel
{
    /// <summary>
    ///     跟踪级别，用于详细的程序执行流程信息
    /// </summary>
    Trace,

    /// <summary>
    ///     调试级别，用于调试过程中的详细信息
    /// </summary>
    Debug,

    /// <summary>
    ///     信息级别，用于一般性的程序运行信息
    /// </summary>
    Info,

    /// <summary>
    ///     警告级别，用于表示可能的问题或异常情况
    /// </summary>
    Warning,

    /// <summary>
    ///     错误级别，用于表示错误但程序仍可继续运行的情况
    /// </summary>
    Error,

    /// <summary>
    ///     致命级别，用于表示严重的错误导致程序无法继续运行
    /// </summary>
    Fatal
}
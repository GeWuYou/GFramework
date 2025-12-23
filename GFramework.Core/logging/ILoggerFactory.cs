namespace GFramework.Core.logging;

/// <summary>
/// 定义日志工厂接口，用于创建日志记录器实例
/// </summary>
public interface ILoggerFactory
{
    /// <summary>
    /// 创建指定类别的日志记录器实例
    /// </summary>
    /// <param name="category">日志类别，用于区分不同的日志源</param>
    /// <returns>返回指定类别的日志记录器实例</returns>
    ILog Create(string category);
}


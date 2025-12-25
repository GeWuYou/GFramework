using GFramework.Core.logging;

namespace GFramework.Core.rule;

/// <summary>
/// 定义一个支持日志记录的接口，允许实现类设置和使用日志记录器
/// </summary>
public interface ILogAware
{
    /// <summary>
    /// 设置日志记录器
    /// </summary>
    /// <param name="logger">要设置的ILogger实例</param>
    void SetLogger(ILogger logger);
}

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
/// 架构上下文提供者接口，用于解耦上下文获取逻辑
/// </summary>
public interface IArchitectureContextProvider
{
    /// <summary>
    /// 获取当前的架构上下文
    /// </summary>
    /// <returns>架构上下文实例</returns>
    IArchitectureContext GetContext();

    /// <summary>
    /// 尝试获取指定类型的架构上下文
    /// </summary>
    /// <typeparam name="T">架构上下文类型</typeparam>
    /// <param name="context">输出的上下文实例</param>
    /// <returns>如果成功获取则返回true，否则返回false</returns>
    bool TryGetContext<T>(out T? context) where T : class, IArchitectureContext;
}
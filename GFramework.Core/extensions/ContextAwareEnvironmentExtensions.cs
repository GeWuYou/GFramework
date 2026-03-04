using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的环境访问扩展方法
/// </summary>
public static class ContextAwareEnvironmentExtensions
{
    /// <summary>
    ///     获取指定类型的环境对象
    /// </summary>
    /// <typeparam name="T">要获取的环境对象类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>指定类型的环境对象,如果无法转换则返回null</returns>
    public static T? GetEnvironment<T>(this IContextAware contextAware) where T : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetEnvironment() as T;
    }

    /// <summary>
    ///     获取环境对象
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>环境对象</returns>
    public static IEnvironment GetEnvironment(this IContextAware contextAware)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetEnvironment();
    }
}
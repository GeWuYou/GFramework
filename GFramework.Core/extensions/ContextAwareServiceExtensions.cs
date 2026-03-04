using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的服务访问扩展方法
///     包含单例和批量获取服务、系统、模型、工具的方法
/// </summary>
public static class ContextAwareServiceExtensions
{
    #region 单例获取

    /// <summary>
    ///     从上下文感知对象中获取指定类型的服务
    /// </summary>
    /// <typeparam name="TService">要获取的服务类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的上下文感知对象</param>
    /// <returns>指定类型的服务实例,如果未找到则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 参数为 null 时抛出</exception>
    public static TService? GetService<TService>(this IContextAware contextAware) where TService : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetService<TService>();
    }

    /// <summary>
    ///     获取架构上下文中的指定系统
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的系统实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TSystem? GetSystem<TSystem>(this IContextAware contextAware) where TSystem : class, ISystem
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetSystem<TSystem>();
    }

    /// <summary>
    ///     获取架构上下文中的指定模型
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的模型实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TModel? GetModel<TModel>(this IContextAware contextAware) where TModel : class, IModel
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetModel<TModel>();
    }

    /// <summary>
    ///     获取架构上下文中的指定工具
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的工具实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TUtility? GetUtility<TUtility>(this IContextAware contextAware) where TUtility : class, IUtility
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetUtility<TUtility>();
    }

    #endregion

    #region 批量获取

    /// <summary>
    ///     从上下文感知对象中获取指定类型的所有服务
    /// </summary>
    /// <typeparam name="TService">要获取的服务类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的上下文感知对象</param>
    /// <returns>所有符合条件的服务实例列表</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 参数为 null 时抛出</exception>
    public static IReadOnlyList<TService> GetServices<TService>(this IContextAware contextAware)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetServices<TService>();
    }

    /// <summary>
    ///     获取架构上下文中的所有指定系统
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>所有符合条件的系统实例列表</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static IReadOnlyList<TSystem> GetSystems<TSystem>(this IContextAware contextAware)
        where TSystem : class, ISystem
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetSystems<TSystem>();
    }

    /// <summary>
    ///     获取架构上下文中的所有指定模型
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>所有符合条件的模型实例列表</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static IReadOnlyList<TModel> GetModels<TModel>(this IContextAware contextAware)
        where TModel : class, IModel
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetModels<TModel>();
    }

    /// <summary>
    ///     获取架构上下文中的所有指定工具
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>所有符合条件的工具实例列表</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static IReadOnlyList<TUtility> GetUtilities<TUtility>(this IContextAware contextAware)
        where TUtility : class, IUtility
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetUtilities<TUtility>();
    }

    #endregion
}
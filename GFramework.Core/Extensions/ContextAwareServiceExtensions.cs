// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Extensions;

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
    /// <returns>指定类型的服务实例，如果未找到则抛出异常</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 参数为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当指定服务未注册时抛出</exception>
    public static TService GetService<TService>(this IContextAware contextAware) where TService : class
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return GetRequiredComponent(context, static architectureContext => architectureContext.GetService<TService>(),
            "Service");
    }

    /// <summary>
    ///     获取架构上下文中的指定系统
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的系统实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当指定系统未注册时抛出</exception>
    public static TSystem GetSystem<TSystem>(this IContextAware contextAware) where TSystem : class, ISystem
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return GetRequiredComponent(context, static architectureContext => architectureContext.GetSystem<TSystem>(),
            "System");
    }

    /// <summary>
    ///     获取架构上下文中的指定模型
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的模型实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当指定模型未注册时抛出</exception>
    public static TModel GetModel<TModel>(this IContextAware contextAware) where TModel : class, IModel
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return GetRequiredComponent(context, static architectureContext => architectureContext.GetModel<TModel>(),
            "Model");
    }

    /// <summary>
    ///     获取架构上下文中的指定工具
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的工具实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当指定工具未注册时抛出</exception>
    public static TUtility GetUtility<TUtility>(this IContextAware contextAware) where TUtility : class, IUtility
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return GetRequiredComponent(context, static architectureContext => architectureContext.GetUtility<TUtility>(),
            "Utility");
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
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

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
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

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
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

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
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return context.GetUtilities<TUtility>();
    }

    /// <summary>
    /// 获取指定类型的所有服务实例，并按优先级排序
    /// 实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>按优先级排序后的服务实例列表</returns>
    public static IReadOnlyList<TService> GetServicesByPriority<TService>(this IContextAware contextAware)
        where TService : class
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return context.GetServicesByPriority<TService>();
    }

    /// <summary>
    /// 获取指定类型的所有系统实例，并按优先级排序
    /// 实现 IPrioritized 接口的系统将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>按优先级排序后的系统实例列表</returns>
    public static IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>(this IContextAware contextAware)
        where TSystem : class, ISystem
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return context.GetSystemsByPriority<TSystem>();
    }

    /// <summary>
    /// 获取指定类型的所有模型实例，并按优先级排序
    /// 实现 IPrioritized 接口的模型将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>按优先级排序后的模型实例列表</returns>
    public static IReadOnlyList<TModel> GetModelsByPriority<TModel>(this IContextAware contextAware)
        where TModel : class, IModel
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return context.GetModelsByPriority<TModel>();
    }

    /// <summary>
    /// 获取指定类型的所有工具实例，并按优先级排序
    /// 实现 IPrioritized 接口的工具将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>按优先级排序后的工具实例列表</returns>
    public static IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>(this IContextAware contextAware)
        where TUtility : class, IUtility
    {
        if (contextAware is null)
        {
            throw new ArgumentNullException(nameof(contextAware));
        }

        var context = contextAware.GetContext();
        return context.GetUtilitiesByPriority<TUtility>();
    }

    private static TComponent GetRequiredComponent<TComponent>(IArchitectureContext context,
        Func<IArchitectureContext, TComponent> resolver, string componentKind)
        where TComponent : class
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var component = resolver(context);
        return component ?? throw new InvalidOperationException($"{componentKind} {typeof(TComponent)} not registered");
    }

    #endregion
}

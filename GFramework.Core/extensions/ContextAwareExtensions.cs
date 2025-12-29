using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.extensions;

/// <summary>
/// 提供对 IContextAware 接口的扩展方法
/// </summary>
public static class ContextAwareExtensions
{
    /// <summary>
    /// 获取架构上下文中的指定系统
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的系统实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TSystem? GetSystem<TSystem>(this IContextAware contextAware) where TSystem : class, ISystem
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        var context = contextAware.GetContext();
        return context.GetSystem<TSystem>();
    }

    /// <summary>
    /// 获取架构上下文中的指定模型
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的模型实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TModel? GetModel<TModel>(this IContextAware contextAware) where TModel : class, IModel
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        var context = contextAware.GetContext();
        return context.GetModel<TModel>();
    }

    /// <summary>
    /// 获取架构上下文中的指定工具
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的工具实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TUtility? GetUtility<TUtility>(this IContextAware contextAware) where TUtility : class, IUtility
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        var context = contextAware.GetContext();
        return context.GetUtility<TUtility>();
    }

    /// <summary>
    /// 发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static TResult SendQuery<TResult>(this IContextAware contextAware, IQuery<TResult> query)
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (query == null) throw new ArgumentNullException(nameof(query));

        var context = contextAware.GetContext();
        return context.SendQuery(query);
    }

    /// <summary>
    /// 发送一个无返回结果的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static void SendCommand(this IContextAware contextAware, ICommand command)
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = contextAware.GetContext();
        context.SendCommand(command);
    }

    /// <summary>
    /// 发送一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResult SendCommand<TResult>(this IContextAware contextAware, ICommand<TResult> command)
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    /// 发送一个事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware) where TEvent : new()
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        var context = contextAware.GetContext();
        context.SendEvent<TEvent>();
    }

    /// <summary>
    /// 发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="e">事件实例</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 e 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware, TEvent e) where TEvent : class
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (e == null) throw new ArgumentNullException(nameof(e));

        var context = contextAware.GetContext();
        context.SendEvent(e);
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public static IUnRegister RegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> handler)
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var context = contextAware.GetContext();
        return context.RegisterEvent(handler);
    }

    /// <summary>
    /// 取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public static void UnRegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> onEvent)
    {
        if (contextAware == null) throw new ArgumentNullException(nameof(contextAware));
        if (onEvent == null) throw new ArgumentNullException(nameof(onEvent));

        var context = contextAware.GetContext();
        context.UnRegisterEvent(onEvent);
    }
}
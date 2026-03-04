using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的事件管理扩展方法
/// </summary>
public static class ContextAwareEventExtensions
{
    /// <summary>
    ///     发送一个事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware) where TEvent : new()
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        context.SendEvent<TEvent>();
    }

    /// <summary>
    ///     发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="e">事件实例</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 e 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware, TEvent e) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(e);

        var context = contextAware.GetContext();
        context.SendEvent(e);
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public static IUnRegister RegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(handler);

        var context = contextAware.GetContext();
        return context.RegisterEvent(handler);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public static void UnRegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(onEvent);

        var context = contextAware.GetContext();
        context.UnRegisterEvent(onEvent);
    }
}
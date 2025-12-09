using GWFramework.framework.command;
using GWFramework.framework.events;
using GWFramework.framework.query;

namespace GWFramework.framework.extensions;

/// <summary>
/// 提供发送命令功能的扩展类
/// </summary>
public static class CanSendCommandExtension
{
    /// <summary>
    /// 发送指定类型的命令，该命令类型必须实现ICommand接口且具有无参构造函数
    /// </summary>
    /// <typeparam name="T">命令类型，必须实现ICommand接口且具有无参构造函数</typeparam>
    /// <param name="self">实现ICanSendCommand接口的对象实例</param>
    public static void SendCommand<T>(this ICanSendCommand self) where T : ICommand, new() =>
        self.GetArchitecture().SendCommand(new T());

    /// <summary>
    /// 发送指定的命令实例
    /// </summary>
    /// <typeparam name="T">命令类型，必须实现ICommand接口</typeparam>
    /// <param name="self">实现ICanSendCommand接口的对象实例</param>
    /// <param name="command">要发送的命令实例</param>
    public static void SendCommand<T>(this ICanSendCommand self, T command) where T : ICommand =>
        self.GetArchitecture().SendCommand(command);

    /// <summary>
    /// 发送带有返回值的命令并获取执行结果
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="self">实现ICanSendCommand接口的对象实例</param>
    /// <param name="command">要发送的命令实例，必须实现ICommand&lt;TResult&gt;接口</param>
    /// <returns>命令执行后的返回结果</returns>
    public static TResult SendCommand<TResult>(this ICanSendCommand self, ICommand<TResult> command) =>
        self.GetArchitecture().SendCommand(command);
}

/// <summary>
/// 提供发送事件功能的扩展类
/// </summary>
public static class CanSendEventExtension
{
    /// <summary>
    /// 发送指定类型的事件，该事件类型必须具有无参构造函数
    /// </summary>
    /// <typeparam name="T">事件类型，必须具有无参构造函数</typeparam>
    /// <param name="self">实现ICanSendEvent接口的对象实例</param>
    public static void SendEvent<T>(this ICanSendEvent self) where T : new() =>
        self.GetArchitecture().SendEvent<T>();

    /// <summary>
    /// 发送指定的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="self">实现ICanSendEvent接口的对象实例</param>
    /// <param name="e">要发送的事件实例</param>
    public static void SendEvent<T>(this ICanSendEvent self, T e) => self.GetArchitecture().SendEvent(e);
}

/// <summary>
/// 提供发送查询功能的扩展类
/// </summary>
public static class CanSendQueryExtension
{
    /// <summary>
    /// 发送查询请求并获取查询结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="self">实现ICanSendQuery接口的对象实例</param>
    /// <param name="query">要发送的查询实例，必须实现IQuery&lt;TResult&gt;接口</param>
    /// <returns>查询操作的返回结果</returns>
    public static TResult SendQuery<TResult>(this ICanSendQuery self, IQuery<TResult> query) =>
        self.GetArchitecture().SendQuery(query);
}

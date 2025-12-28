using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.query;

namespace GFramework.Core.architecture;

/// <summary>
///     架构运行时默认实现，委托 ArchitectureContext 执行具体操作
/// </summary>
public class ArchitectureRuntime(IArchitectureContext context) : IArchitectureRuntime
{
    private readonly IArchitectureContext _context = context ?? throw new ArgumentNullException(nameof(context));

    #region Query Execution

    /// <summary>
    ///     发起一次查询请求并获得其结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的数据类型</typeparam>
    /// <param name="query">要发起的查询对象</param>
    /// <returns>查询得到的结果数据</returns>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        return _context.SendQuery(query);
    }

    #endregion

    #region Command Execution

    /// <summary>
    ///     发送一个无返回结果的命令请求
    /// </summary>
    /// <typeparam name="TCommand">命令的具体类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    public void SendCommand<TCommand>(TCommand command) where TCommand : ICommand
    {
        _context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个带返回结果的命令请求
    /// </summary>
    /// <typeparam name="TResult">命令执行后的返回值类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行的结果</returns>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        return _context.SendCommand(command);
    }

    #endregion

    #region Event Management

    /// <summary>
    ///     发布一个默认构造的新事件对象
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _context.SendEvent<TEvent>();
    }

    /// <summary>
    ///     发布一个具体的事件对象
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">要发布的事件实例</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        _context.SendEvent(e);
    }

    /// <summary>
    ///     订阅某个特定类型的事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">当事件发生时触发的动作</param>
    /// <returns>可用于取消订阅的对象</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        return _context.RegisterEvent(onEvent);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        _context.UnRegisterEvent(onEvent);
    }

    #endregion
}
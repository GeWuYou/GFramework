
using System.ComponentModel;
using GFramework.Core.command;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Core.query;
using GFramework.Core.system;
using GFramework.Core.utility;

namespace GFramework.Core.architecture;

public class DefaultArchitectureContext(
    IIocContainer container, 
    ITypeEventSystem typeEventSystem, 
    ILogger logger)
    : IArchitectureContext
{
    private readonly ITypeEventSystem _typeEventSystem = typeEventSystem;
    public ILogger Logger { get; } = logger;

    #region Component Retrieval

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return container.Get<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return container.Get<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return container.Get<TUtility>();
    }

    #endregion

    public void SendCommand(ICommand command)
    {
        throw new NotImplementedException();
    }

    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        throw new NotImplementedException();
    }

    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        throw new NotImplementedException();
    }

    public void SendEvent<TEvent>()
    {
        throw new NotImplementedException();
    }

    public void SendEvent<TEvent>(TEvent e)
    {
        throw new NotImplementedException();
    }

    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        throw new NotImplementedException();
    }


}
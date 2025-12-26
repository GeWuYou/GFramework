namespace GFramework.Core.events;

/// <summary>
///     类型事件系统接口，定义基于类型的事件发送、注册和注销功能
/// </summary>
public interface ITypeEventSystem
{
    /// <summary>
    ///     发送事件，自动创建事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须具有无参构造函数</typeparam>
    void Send<T>() where T : new();

    /// <summary>
    ///     发送指定的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    void Send<T>(T e);

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    IUnRegister Register<T>(Action<T> onEvent);

    /// <summary>
    ///     注销事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">要注销的事件处理回调函数</param>
    void UnRegister<T>(Action<T> onEvent);
}
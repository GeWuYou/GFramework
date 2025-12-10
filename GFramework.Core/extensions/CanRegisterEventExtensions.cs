using GFramework.Core.events;

namespace GFramework.Core.extensions;

/// <summary>
/// 事件注册扩展类，提供ICanRegisterEvent接口的扩展方法用于注册和注销事件
/// </summary>
public static class CanRegisterEventExtensions
{
    /// <summary>
    /// 注册指定类型的事件处理函数
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    /// <param name="self">实现ICanRegisterEvent接口的对象实例</param>
    /// <param name="onEvent">事件处理回调函数</param>
    /// <returns>返回事件注销器接口，可用于后续注销事件</returns>
    public static IUnRegister RegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent) =>
        self.GetArchitecture().RegisterEvent(onEvent);

    /// <summary>
    /// 注销指定类型的事件处理函数
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    /// <param name="self">实现ICanRegisterEvent接口的对象实例</param>
    /// <param name="onEvent">要注销的事件处理回调函数</param>
    public static void UnRegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent) =>
        self.GetArchitecture().UnRegisterEvent(onEvent);
}

using GWFramework.framework.model;
using GWFramework.framework.system;
using GWFramework.framework.utility;

namespace GWFramework.framework.extensions;

/// <summary>
/// 提供获取模型对象扩展方法的静态类
/// </summary>
public static class CanGetModelExtension
{
    /// <summary>
    /// 从架构中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="T">要获取的模型类型，必须实现IModel接口</typeparam>
    /// <param name="self">实现ICanGetModel接口的对象实例</param>
    /// <returns>指定类型的模型实例</returns>
    public static T GetModel<T>(this ICanGetModel self) where T : class, IModel =>
        self.GetArchitecture().GetModel<T>();
}

/// <summary>
/// 提供获取系统对象扩展方法的静态类
/// </summary>
public static class CanGetSystemExtension
{
    /// <summary>
    /// 从架构中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="T">要获取的系统类型，必须实现ISystem接口</typeparam>
    /// <param name="self">实现ICanGetSystem接口的对象实例</param>
    /// <returns>指定类型的系统实例</returns>
    public static T GetSystem<T>(this ICanGetSystem self) where T : class, ISystem =>
        self.GetArchitecture().GetSystem<T>();
}

/// <summary>
/// 提供获取工具对象扩展方法的静态类
/// </summary>
public static class CanGetUtilityExtension
{
    /// <summary>
    /// 从架构中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="T">要获取的工具类型，必须实现IUtility接口</typeparam>
    /// <param name="self">实现ICanGetUtility接口的对象实例</param>
    /// <returns>指定类型的工具实例</returns>
    public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility =>
        self.GetArchitecture().GetUtility<T>();
}
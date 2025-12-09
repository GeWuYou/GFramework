using GWFramework.framework.events;

namespace GWFramework.framework.extensions;

/// <summary>
/// 提供Or事件扩展方法的静态类
/// </summary>
public static class OrEventExtensions
{
    /// <summary>
    /// 创建一个OrEvent实例，将当前事件与指定事件进行逻辑或运算组合
    /// </summary>
    /// <param name="self">当前的IEasyEvent事件实例</param>
    /// <param name="e">要与当前事件进行或运算的另一个IEasyEvent事件实例</param>
    /// <returns>返回一个新的OrEvent实例，表示两个事件的或运算结果</returns>
    public static OrEvent Or(this IEasyEvent self, IEasyEvent e) => new OrEvent().Or(self).Or(e);
}
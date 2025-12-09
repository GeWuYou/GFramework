using GWFramework.framework.events;

namespace GWFramework.framework.extensions;


/// <summary>
/// 扩展方法类，为IUnRegister和IUnRegisterList接口提供便捷的注册和注销功能
/// </summary>
public static class UnRegisterListExtension
{
    /// <summary>
    /// 将指定的可注销对象添加到注销列表中
    /// </summary>
    /// <param name="self">要添加的可注销对象</param>
    /// <param name="unRegisterList">目标注销列表</param>
    public static void AddToUnregisterList(this IUnRegister self, IUnRegisterList unRegisterList) =>
        unRegisterList.UnregisterList.Add(self);

    /// <summary>
    /// 注销列表中的所有对象并清空列表
    /// </summary>
    /// <param name="self">包含注销列表的对象</param>
    public static void UnRegisterAll(this IUnRegisterList self)
    {
        // 遍历注销列表中的所有对象并执行注销操作
        foreach (var unRegister in self.UnregisterList)
        {
            unRegister.UnRegister();
        }

        // 清空注销列表
        self.UnregisterList.Clear();
    }
}

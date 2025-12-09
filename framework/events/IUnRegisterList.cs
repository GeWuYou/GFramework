namespace GWFramework.framework.events;

/// <summary>
/// 提供统一注销功能的接口，用于管理需要注销的对象列表
/// </summary>
public interface IUnRegisterList
{
    /// <summary>
    /// 获取需要注销的对象列表
    /// </summary>
    List<IUnRegister> UnregisterList { get; }
}

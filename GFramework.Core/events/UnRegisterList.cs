using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     取消注册列表类，用于管理多个需要取消注册的对象
/// </summary>
public class UnRegisterList : IUnRegisterList
{
    private readonly List<IUnRegister> _unRegisterList = [];

    /// <summary>
    ///     获取取消注册列表的只读属性
    /// </summary>
    public IList<IUnRegister> UnregisterList { get; } = null!;

    /// <summary>
    ///     向取消注册列表中添加一个新的可取消注册对象
    /// </summary>
    /// <param name="unRegister">需要添加到列表中的可取消注册对象</param>
    public void Add(IUnRegister unRegister)
    {
        _unRegisterList.Add(unRegister);
    }

    /// <summary>
    ///     对列表中的所有对象执行取消注册操作，并清空列表
    /// </summary>
    public void UnRegisterAll()
    {
        // 遍历所有注册项并执行取消注册
        foreach (var t in _unRegisterList) t.UnRegister();

        // 清空列表
        _unRegisterList.Clear();
    }
}
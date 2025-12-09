namespace GFramework.framework.events;


/// <summary>
/// 默认注销器类，用于执行注销操作
/// </summary>
/// <param name="onUnRegister">注销时要执行的回调函数</param>
public class DefaultUnRegister(Action onUnRegister): IUnRegister
{
    private Action _mOnUnRegister = onUnRegister;

    /// <summary>
    /// 执行注销操作，调用注册的回调函数并清理引用
    /// </summary>
    public void UnRegister()
    {
        // 调用注销回调函数并清理引用
        _mOnUnRegister.Invoke();
        _mOnUnRegister = null;
    }
}

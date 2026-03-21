using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Abstractions.Pause;

/// <summary>
/// 暂停栈管理器接口，管理嵌套暂停状态
/// </summary>
public interface IPauseStackManager : IContextUtility
{
    /// <summary>
    /// 推入暂停请求
    /// </summary>
    /// <param name="reason">暂停原因（用于调试）</param>
    /// <param name="group">暂停组</param>
    /// <returns>暂停令牌（用于后续恢复）</returns>
    PauseToken Push(string reason, PauseGroup group = PauseGroup.Global);

    /// <summary>
    /// 弹出暂停请求
    /// </summary>
    /// <param name="token">暂停令牌</param>
    /// <returns>是否成功弹出</returns>
    bool Pop(PauseToken token);

    /// <summary>
    /// 查询指定组是否暂停
    /// </summary>
    /// <param name="group">暂停组</param>
    /// <returns>是否暂停</returns>
    bool IsPaused(PauseGroup group = PauseGroup.Global);

    /// <summary>
    /// 获取指定组的暂停深度（栈中元素数量）
    /// </summary>
    /// <param name="group">暂停组</param>
    /// <returns>暂停深度</returns>
    int GetPauseDepth(PauseGroup group = PauseGroup.Global);

    /// <summary>
    /// 获取指定组的所有暂停原因
    /// </summary>
    /// <param name="group">暂停组</param>
    /// <returns>暂停原因列表</returns>
    IReadOnlyList<string> GetPauseReasons(PauseGroup group = PauseGroup.Global);

    /// <summary>
    /// 创建暂停作用域（支持 using 语法）
    /// </summary>
    /// <param name="reason">暂停原因</param>
    /// <param name="group">暂停组</param>
    /// <returns>可释放的作用域对象</returns>
    IDisposable PauseScope(string reason, PauseGroup group = PauseGroup.Global);

    /// <summary>
    /// 清空指定组的所有暂停请求
    /// </summary>
    /// <param name="group">暂停组</param>
    void ClearGroup(PauseGroup group);

    /// <summary>
    /// 清空所有暂停请求
    /// </summary>
    void ClearAll();

    /// <summary>
    /// 注册暂停处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    void RegisterHandler(IPauseHandler handler);

    /// <summary>
    /// 注销暂停处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    void UnregisterHandler(IPauseHandler handler);

    /// <summary>
    /// 暂停状态变化事件。
    /// 事件遵循标准 .NET 事件模式，事件源为触发通知的暂停管理器实例，
    /// 事件数据由 <see cref="PauseStateChangedEventArgs"/> 提供。
    /// </summary>
    event EventHandler<PauseStateChangedEventArgs>? OnPauseStateChanged;
}
namespace GFramework.Game.Abstractions.coroutine;

/// <summary>
/// 协程作用域接口，用于管理协程的生命周期
/// </summary>
public interface ICoroutineScope
{
    /// <summary>
    /// 获取协程是否处于活动状态
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 取消协程执行
    /// </summary>
    void Cancel();
}
namespace GFramework.Core.Abstractions.pool;

/// <summary>
/// 定义可池化对象的接口，提供对象在池中的生命周期管理方法
/// </summary>
public interface IPoolableObject
{
    /// <summary>
    /// 当对象从池中被获取时调用，用于初始化或重置对象状态
    /// </summary>
    void OnAcquire();

    /// <summary>
    /// 当对象被释放回池中时调用，用于清理或重置对象状态以便下次使用
    /// </summary>
    void OnRelease();

    /// <summary>
    /// 当对象池被销毁时调用，用于执行最终的清理工作
    /// </summary>
    void OnPoolDestroy();
}
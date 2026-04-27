using GFramework.Core.Abstractions.Pool;

namespace GFramework.Core.Tests.Pool;

/// <summary>
///     供对象池测试使用的池化对象，记录生命周期回调是否被触发。
/// </summary>
public class TestPoolableObject : IPoolableObject
{
    /// <summary>
    ///     获取或设置对象所属的池键。
    /// </summary>
    public string PoolKey { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置测试中写入的整数值。
    /// </summary>
    public int TestValue { get; set; }

    /// <summary>
    ///     获取或设置对象获取回调是否已执行。
    /// </summary>
    public bool OnAcquireCalled { get; set; }

    /// <summary>
    ///     获取或设置对象释放回调是否已执行。
    /// </summary>
    public bool OnReleaseCalled { get; set; }

    /// <summary>
    ///     获取或设置对象销毁回调是否已执行。
    /// </summary>
    public bool OnPoolDestroyCalled { get; set; }

    /// <summary>
    ///     在对象被对象池取出时记录回调执行状态。
    /// </summary>
    public void OnAcquire()
    {
        OnAcquireCalled = true;
    }

    /// <summary>
    ///     在对象被归还到对象池时记录回调执行状态。
    /// </summary>
    public void OnRelease()
    {
        OnReleaseCalled = true;
    }

    /// <summary>
    ///     在对象因容量限制或清池而销毁时记录回调执行状态。
    /// </summary>
    public void OnPoolDestroy()
    {
        OnPoolDestroyCalled = true;
    }
}

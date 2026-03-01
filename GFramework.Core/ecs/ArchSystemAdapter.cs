using Arch.Core;
using GFramework.Core.extensions;
using GFramework.Core.system;
using ArchSys = Arch.System;

namespace GFramework.Core.ecs;

/// <summary>
///     Arch 系统适配器 - 桥接 Arch.System.ISystem&lt;T&gt; 到框架上下文
/// </summary>
/// <typeparam name="T">系统数据类型（通常是 float 表示 deltaTime）</typeparam>
public abstract class ArchSystemAdapter<T> : AbstractSystem, ArchSys.ISystem<T>
{
    /// <summary>
    ///     获取或设置 Arch ECS 世界的实例
    /// </summary>
    public World World { get; private set; } = null!;

    // ===== Arch 显式接口实现 =====

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的初始化方法
    ///     调用受保护的虚方法 OnArchInitialize 以允许子类自定义初始化逻辑
    /// </summary>
    void ArchSys.ISystem<T>.Initialize()
    {
        OnArchInitialize();
    }

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的更新前回调方法
    ///     调用受保护的虚方法 OnBeforeUpdate 以允许子类自定义预处理逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    void ArchSys.ISystem<T>.BeforeUpdate(in T t)
    {
        OnBeforeUpdate(in t);
    }

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的主更新方法
    ///     调用受保护的抽象方法 OnUpdate 以强制子类实现核心更新逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    public void Update(in T t)
    {
        OnUpdate(in t);
    }

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的更新后回调方法
    ///     调用受保护的虚方法 OnAfterUpdate 以允许子类自定义后处理逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    void ArchSys.ISystem<T>.AfterUpdate(in T t)
    {
        OnAfterUpdate(in t);
    }

    /// <summary>
    ///     显式实现 IDisposable 的资源释放方法
    ///     调用受保护的虚方法 OnArchDispose 以允许子类自定义资源清理逻辑
    /// </summary>
    void IDisposable.Dispose()
    {
        OnArchDispose();
    }

    // ===== GFramework 生命周期 =====

    /// <summary>
    ///     系统初始化方法
    ///     在此方法中获取 Arch World 实例并调用 Arch 系统的初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
        World = this.GetService<World>()!;

        // 调用 Arch 初始化
        ((ArchSys.ISystem<T>)this).Initialize();
    }

    /// <summary>
    ///     系统销毁方法
    ///     在此方法中调用 Arch 系统的资源释放逻辑
    /// </summary>
    protected override void OnDestroy()
    {
        ((ArchSys.ISystem<T>)this).Dispose();
    }

    // ===== 子类可重写 Hook =====

    /// <summary>
    ///     Arch 系统初始化的受保护虚方法
    ///     子类可重写此方法以实现自定义的 Arch 系统初始化逻辑
    /// </summary>
    protected virtual void OnArchInitialize()
    {
    }

    /// <summary>
    ///     更新前处理的受保护虚方法
    ///     子类可重写此方法以实现自定义的预处理逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    protected virtual void OnBeforeUpdate(in T t)
    {
    }

    /// <summary>
    ///     核心更新逻辑的受保护抽象方法
    ///     子类必须重写此方法以实现具体的系统更新功能
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    protected virtual void OnUpdate(in T t)
    {
    }

    /// <summary>
    ///     更新后处理的受保护虚方法
    ///     子类可重写此方法以实现自定义的后处理逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    protected virtual void OnAfterUpdate(in T t)
    {
    }

    /// <summary>
    ///     Arch 系统资源释放的受保护虚方法
    ///     子类可重写此方法以实现自定义的资源清理逻辑
    /// </summary>
    protected virtual void OnArchDispose()
    {
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using Arch.Core;
using GFramework.Core.Abstractions.Lifecycle;
using ArchSys = Arch.System;

namespace GFramework.Ecs.Arch;

/// <summary>
///     Arch 系统适配器 - 通过显式 World 依赖桥接 Arch.System.ISystem&lt;T&gt;
/// </summary>
/// <typeparam name="T">系统数据类型（通常是 float 表示 deltaTime）</typeparam>
/// <remarks>Receives the owning world's explicit dependency; the adapter never resolves a container.</remarks>
/// <param name="world">The world owned by the composition root.</param>
public abstract class ArchSystemAdapter<T>(World world) : ILifecycle, IArchSystemAdapter<T>, ArchSys.ISystem<T>
{
    private int _disposed;
    private bool _isInitialized;

    /// <summary>
    ///     获取调用方拥有的 Arch ECS 世界实例
    /// </summary>
    public World World
    {
        get;
    } = world ?? throw new ArgumentNullException(nameof(world));

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的主更新方法
    ///     调用受保护的虚方法 OnUpdate 以强制子类实现核心更新逻辑
    /// </summary>
    /// <param name="t">系统数据参数（通常是 deltaTime）</param>
    public void Update(in T t)
    {
        OnUpdate(in t);
    }

    // ===== Arch 显式接口实现 =====

    /// <summary>
    ///     显式实现 Arch.System.ISystem&lt;T&gt; 的初始化方法
    ///     调用受保护的虚方法 OnArchInitialize 以允许子类自定义初始化逻辑
    /// </summary>
    void ArchSys.ISystem<T>.Initialize()
    {
        Initialize();
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
    /// <remarks>成功清理后抑制派生类终结器；清理抛错时不抑制终结器兜底。</remarks>
    void IDisposable.Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            OnArchDispose();
            // Successful cleanup suppresses derived finalizers; a cleanup failure keeps the finalizer fallback.
            GC.SuppressFinalize(this);
        }
    }

    // ===== GFramework 生命周期 =====

    /// <summary>
    ///     系统初始化方法
    ///     在此方法中使用构造时传入的 World 并调用 Arch 系统的初始化逻辑
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;


        // 原生接口和框架接口共享这一入口，避免混合调用时重复初始化。
        OnArchInitialize();
        _isInitialized = true;
    }

    /// <summary>
    ///     系统销毁方法
    ///     停止后的资源由拥有该实例的 DI provider 或外部调用者释放，不在此处重复 Dispose
    /// </summary>
    public void Destroy()
    {
        _isInitialized = false;
        OnArchStopped();
    }

    /// <summary>Stops game work without disposing the world or dependencies owned by the provider.</summary>
    protected virtual void OnArchStopped()
    {
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
    ///     受保护虚方法，在系统更新时被调用。
    ///     默认实现不执行任何操作，子类可以根据需要选择性地重写此方法以实现具体的系统逻辑。
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

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     把冻结后的 benchmark 根容器适配成可重复进入的 request 级标准服务解析视图。
/// </summary>
/// <remarks>
///     CQRS runtime 现在基于标准 <see cref="IServiceProvider" /> 做 handler / pipeline 解析，
///     因此 request lifetime benchmark 只需要一个把 <see cref="GetService" /> /
///     <see cref="IServiceProviderIsService.IsService" /> 转发到“当前激活作用域”的最小适配层。
///     注册、清空或冻结修改操作都继续发生在根容器构建阶段；适配器自身保持只读，
///     每次分发前通过 <see cref="EnterScope" /> 切换到新的 request 级作用域 provider。
/// </remarks>
internal sealed class ScopedBenchmarkContainer : IServiceProvider, IServiceProviderIsService, IDisposable, IAsyncDisposable
{
    private readonly BenchmarkServiceHost _rootContainer;
    private IServiceScope? _activeScope;
    private IServiceProvider? _scopedProvider;

    /// <summary>
    ///     初始化一个绑定到单个 request 作用域的 benchmark provider 适配器。
    /// </summary>
    /// <param name="rootContainer">已冻结的 benchmark 根容器。</param>
    internal ScopedBenchmarkContainer(BenchmarkServiceHost rootContainer)
    {
        _rootContainer = rootContainer ?? throw new ArgumentNullException(nameof(rootContainer));
    }

    /// <summary>
    ///     为当前 benchmark 调用创建并持有一个新的 request 级作用域。
    /// </summary>
    /// <returns>离开作用域时负责释放本次 request 级作用域的租约。</returns>
    /// <exception cref="InvalidOperationException">当前适配器仍持有上一次尚未释放的作用域。</exception>
    internal ScopeLease EnterScope()
    {
        if (_activeScope is not null)
        {
            throw new InvalidOperationException(
                "Scoped benchmark containers do not support overlapping active scopes.");
        }

        _activeScope = _rootContainer.CreateScope();
        _scopedProvider = _activeScope.ServiceProvider;
        return new ScopeLease(this);
    }

    /// <summary>
    ///     从当前 request 作用域解析单个服务实例。
    /// </summary>
    /// <param name="serviceType">目标服务类型。</param>
    /// <returns>解析到的服务实例；若当前作用域未注册则返回 <see langword="null" />。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope" /> 激活作用域。</exception>
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return GetScopedProvider().GetService(serviceType);
    }

    /// <summary>
    ///     判断当前 request 作用域是否声明了目标服务键。
    /// </summary>
    /// <param name="serviceType">目标服务类型。</param>
    /// <returns>当前作用域 provider 声明了该服务键时返回 <see langword="true" />。</returns>
    /// <remarks>
    ///     CQRS runtime 在热路径上先做注册存在性判断，再决定是否枚举 pipeline；
    ///     标准 provider 的非物化探测与根容器注册视图语义一致，因此这里直接转发当前作用域 provider。
    /// </remarks>
    public bool IsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        var scopedProvider = GetScopedProvider();
        return scopedProvider is IServiceProviderIsService isService
            ? isService.IsService(serviceType)
            : scopedProvider.GetService(serviceType) is not null;
    }

    /// <summary>
    ///     释放当前 request 适配器时，同时兜底释放任何尚未归还的激活作用域。
    /// </summary>
    public void Dispose()
    {
        ReleaseActiveScope();
    }

    /// <summary>异步释放当前适配器拥有的作用域，不释放外部根容器。</summary>
    /// <returns>作用域资源已完成释放。</returns>
    public async ValueTask DisposeAsync()
    {
        _scopedProvider = null;
        var scope = _activeScope;
        _activeScope = null;
        if (scope is IAsyncDisposable asyncScope)
        {
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            scope?.Dispose();
        }
    }

    /// <summary>
    ///     读取当前激活的 request 级作用域服务提供器。
    /// </summary>
    /// <returns>当前作用域对应的标准服务提供器。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope" /> 激活作用域。</exception>
    private IServiceProvider GetScopedProvider()
    {
        return _scopedProvider ?? throw new InvalidOperationException(
            "Scoped benchmark containers require an active scope. Call EnterScope() before resolving scoped services.");
    }

    /// <summary>
    ///     释放当前激活的 request 级作用域，并清空解析视图。
    /// </summary>
    private void ReleaseActiveScope()
    {
        _scopedProvider = null;

        var activeScope = _activeScope;
        _activeScope = null;
        activeScope?.Dispose();
    }

    /// <summary>
    ///     表示一次激活中的 request 级作用域租约。
    /// </summary>
    internal readonly struct ScopeLease : IDisposable
    {
        private readonly ScopedBenchmarkContainer _owner;

        /// <summary>
        ///     初始化一个绑定到目标适配器的作用域租约。
        /// </summary>
        /// <param name="owner">拥有当前作用域的 benchmark provider 适配器。</param>
        internal ScopeLease(ScopedBenchmarkContainer owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        ///     释放当前 request 级作用域。
        /// </summary>
        public void Dispose()
        {
            _owner.ReleaseActiveScope();
        }
    }
}

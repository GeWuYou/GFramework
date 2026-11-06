// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Ioc;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     把冻结后的 benchmark 根容器适配成可重复进入的 request 级解析视图。
/// </summary>
/// <remarks>
///     `CqrsDispatcher` 会直接依赖 <see cref="IIocContainer" /> 做 handler / pipeline 解析，
///     因此 request lifetime benchmark 需要一个既保留根容器注册元数据，又能在每次 benchmark 调用时把实例解析切换到
///     显式作用域 provider 的最小适配层。该类型只覆盖 benchmark 当前 request 路径会使用到的解析相关入口；
///     任何注册、清空或冻结修改操作都应继续发生在根容器构建阶段，因此这里统一拒绝可变更 API。
/// </remarks>
internal sealed class ScopedBenchmarkContainer : IIocContainer
{
    private readonly MicrosoftDiContainer _rootContainer;
    private IServiceScope? _activeScope;
    private IServiceProvider? _scopedProvider;

    /// <summary>
    ///     初始化一个绑定到单个 request 作用域的 benchmark 容器适配器。
    /// </summary>
    /// <param name="rootContainer">已冻结的 benchmark 根容器。</param>
    internal ScopedBenchmarkContainer(MicrosoftDiContainer rootContainer)
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
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="T">实例类型。</typeparam>
    /// <param name="instance">原本要注册到根容器中的单例实例。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterSingleton<T>(T instance)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TService">服务契约类型。</typeparam>
    /// <typeparam name="TImpl">服务实现类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterSingleton<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TService">服务契约类型。</typeparam>
    /// <typeparam name="TImpl">服务实现类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterTransient<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TService">服务契约类型。</typeparam>
    /// <typeparam name="TImpl">服务实现类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterScoped<TService, TImpl>()
        where TImpl : class, TService
        where TService : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <param name="instance">原本要附加到复数注册集合中的实例。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterPlurality(object instance)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="T">复数注册项类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterPlurality<T>() where T : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <param name="system">原本要注册到容器中的系统实例。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterSystem(ISystem system)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="T">实例类型。</typeparam>
    /// <param name="instance">原本要注册到容器中的实例。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void Register<T>(T instance)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <param name="type">原本要绑定的服务类型。</param>
    /// <param name="instance">原本要绑定到该类型的实例。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void Register(Type type, object instance)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TService">工厂要创建的服务类型。</typeparam>
    /// <param name="factory">原本要注册的工厂委托。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterFactory<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TBehavior">原本要注册的 request pipeline 行为类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterCqrsPipelineBehavior<TBehavior>() where TBehavior : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <typeparam name="TBehavior">原本要注册的 stream pipeline 行为类型。</typeparam>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterCqrsStreamPipelineBehavior<TBehavior>() where TBehavior : class
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <param name="assembly">原本要扫描 CQRS handler 的程序集。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterCqrsHandlersFromAssembly(System.Reflection.Assembly assembly)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持在 request 作用域内追加注册。
    /// </summary>
    /// <param name="assemblies">原本要扫描 CQRS handler 的程序集集合。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void RegisterCqrsHandlersFromAssemblies(IEnumerable<System.Reflection.Assembly> assemblies)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持执行额外的服务配置钩子。
    /// </summary>
    /// <param name="configurator">原本要执行的服务配置委托。</param>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void ExecuteServicesHook(Action<IServiceCollection>? configurator = null)
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     从当前 request 作用域解析单个服务实例。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <returns>解析到的服务实例；若当前作用域未注册则返回 <see langword="null"/>。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public T? Get<T>() where T : class
    {
        return GetScopedProvider().GetService<T>();
    }

    /// <summary>
    ///     从当前 request 作用域解析单个服务实例。
    /// </summary>
    /// <param name="type">目标服务类型。</param>
    /// <returns>解析到的服务实例；若当前作用域未注册则返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public object? Get(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return GetScopedProvider().GetService(type);
    }

    /// <summary>
    ///     从当前 request 作用域解析必需的单个服务实例。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <returns>解析到的服务实例。</returns>
    /// <exception cref="InvalidOperationException">
    ///     调用方尚未通过 <see cref="EnterScope"/> 激活作用域，或当前作用域缺少必需服务。
    /// </exception>
    public T GetRequired<T>() where T : class
    {
        return GetScopedProvider().GetRequiredService<T>();
    }

    /// <summary>
    ///     从当前 request 作用域解析必需的单个服务实例。
    /// </summary>
    /// <param name="type">目标服务类型。</param>
    /// <returns>解析到的服务实例。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">
    ///     调用方尚未通过 <see cref="EnterScope"/> 激活作用域，或当前作用域缺少必需服务。
    /// </exception>
    public object GetRequired(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return GetScopedProvider().GetRequiredService(type);
    }

    /// <summary>
    ///     从当前 request 作用域解析全部服务实例。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <returns>当前作用域中该服务类型的全部实例。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        return GetScopedProvider().GetServices<T>().ToList();
    }

    /// <summary>
    ///     从当前 request 作用域解析全部服务实例。
    /// </summary>
    /// <param name="type">目标服务类型。</param>
    /// <returns>当前作用域中该服务类型的全部实例。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IReadOnlyList<object> GetAll(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return GetScopedProvider().GetServices(type).Where(static service => service is not null).Cast<object>().ToList();
    }

    /// <summary>
    ///     从当前 request 作用域解析全部服务实例，并按调用方比较器排序。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <param name="comparison">用于排序的比较器。</param>
    /// <returns>按比较器排序后的服务列表。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparison"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IReadOnlyList<T> GetAllSorted<T>(Comparison<T> comparison) where T : class
    {
        ArgumentNullException.ThrowIfNull(comparison);

        var services = GetAll<T>().ToList();
        services.Sort(comparison);
        return services;
    }

    /// <summary>
    ///     从当前 request 作用域解析全部服务实例，并按优先级排序。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <returns>按优先级稳定排序后的服务列表。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IReadOnlyList<T> GetAllByPriority<T>() where T : class
    {
        return SortByPriority(GetAll<T>());
    }

    /// <summary>
    ///     从当前 request 作用域解析全部服务实例，并按优先级排序。
    /// </summary>
    /// <param name="type">目标服务类型。</param>
    /// <returns>按优先级稳定排序后的服务列表。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IReadOnlyList<object> GetAllByPriority(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return SortByPriority(GetAll(type));
    }

    /// <summary>
    ///     判断根容器是否声明了目标服务键。
    /// </summary>
    /// <param name="type">目标服务类型。</param>
    /// <returns>根容器中声明了该服务键时返回 <see langword="true"/>。</returns>
    /// <remarks>
    ///     `CqrsDispatcher` 在热路径上先做注册存在性判断，再决定是否枚举 pipeline；这里沿用根容器冻结后的注册视图，
    ///     避免把“当前 scope 还未物化实例”误判成“没有注册该行为”。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/>。</exception>
    public bool HasRegistration(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _rootContainer.HasRegistration(type);
    }

    /// <summary>
    ///     判断根容器是否声明了目标服务键。
    /// </summary>
    /// <typeparam name="T">目标服务类型。</typeparam>
    /// <returns>根容器中声明了该服务键时返回 <see langword="true"/>。</returns>
    public bool Contains<T>() where T : class
    {
        return _rootContainer.Contains<T>();
    }

    /// <summary>
    ///     当前 request 作用域适配器不追踪实例归属。
    /// </summary>
    /// <param name="instance">待检查的实例。</param>
    /// <returns>若根容器已追踪该实例，则返回根容器的检查结果。</returns>
    public bool ContainsInstance(object instance)
    {
        return _rootContainer.ContainsInstance(instance);
    }

    /// <summary>
    ///     当前适配器不支持清空注册。
    /// </summary>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void Clear()
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     当前适配器不支持重新冻结。
    /// </summary>
    /// <exception cref="InvalidOperationException">当前适配器始终为只读视图。</exception>
    public void Freeze()
    {
        throw CreateMutationNotSupportedException();
    }

    /// <summary>
    ///     继续暴露根容器底层服务集合，仅用于接口兼容。
    /// </summary>
    /// <returns>根容器当前持有的底层服务集合。</returns>
    public IServiceCollection GetServicesUnsafe => _rootContainer.GetServicesUnsafe;

    /// <summary>
    ///     基于当前 request 作用域继续创建嵌套作用域。
    /// </summary>
    /// <returns>从当前激活作用域继续派生出的子作用域。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
    public IServiceScope CreateScope()
    {
        return GetScopedProvider().CreateScope();
    }

    /// <summary>
    ///     将上下文转发给根容器，保持与 request 生命周期无关的上下文缓存行为一致。
    /// </summary>
    /// <param name="context">要绑定到根容器的架构上下文。</param>
    public void SetContext(GFramework.Core.Abstractions.Architectures.IArchitectureContext context)
    {
        ((IContextAware)_rootContainer).SetContext(context);
    }

    /// <summary>
    ///     读取根容器当前持有的架构上下文。
    /// </summary>
    /// <returns>根容器当前保存的架构上下文。</returns>
    public GFramework.Core.Abstractions.Architectures.IArchitectureContext GetContext()
    {
        return ((IContextAware)_rootContainer).GetContext();
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
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        else
            scope?.Dispose();
    }

    /// <summary>
    ///     读取当前激活的 request 级作用域服务提供器。
    /// </summary>
    /// <returns>当前作用域对应的服务提供器。</returns>
    /// <exception cref="InvalidOperationException">调用方尚未通过 <see cref="EnterScope"/> 激活作用域。</exception>
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
    ///     生成统一的只读适配器异常，避免 benchmark 误把 request 级容器当成可变组合根。
    /// </summary>
    /// <returns>描述当前适配器只读语义的统一异常。</returns>
    private static InvalidOperationException CreateMutationNotSupportedException()
    {
        return new InvalidOperationException(
            "Scoped benchmark containers are read-only request views. Mutate registrations on the root benchmark host before freezing it.");
    }

    /// <summary>
    ///     复用与根容器一致的优先级排序语义。
    /// </summary>
    /// <typeparam name="T">服务实例类型。</typeparam>
    /// <param name="services">待排序服务集合。</param>
    /// <returns>按优先级稳定排序后的服务列表。</returns>
    private static IReadOnlyList<T> SortByPriority<T>(IReadOnlyList<T> services) where T : class
    {
        if (services.Count <= 1)
        {
            return services;
        }

        return services
            .Select((service, index) => new { Service = service, Index = index })
            .OrderBy(static x =>
            {
                var priority = x.Service is IPrioritized prioritized ? prioritized.Priority : 0;
                return (priority, x.Index);
            })
            .Select(static x => x.Service)
            .ToList();
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
        /// <param name="owner">拥有当前作用域的 benchmark 容器适配器。</param>
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

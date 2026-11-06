// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Godot.Architectures;

/// <summary>
///     Godot 架构上下文装饰器，把架构上下文的非 CQRS 能力面原样委托给内部实现。
/// </summary>
/// <remarks>
///     <para><see cref="IArchitectureContext" /> 不再承载任何 CQRS 成员；请求、通知与流式分发由可选安装的
///     <c>CqrsRuntimeModule</c>（或组合根的 <c>UseCqrsRuntime</c> 扩展）提供，调用方通过
///     <c>GFramework.Cqrs.Extensions</c> 中的异步上下文扩展方法进入 dispatcher，本装饰器不参与 CQRS 派发。</para>
///     <para>装饰器保留 Godot 侧的统一上下文包装点，便于宿主在需要时为剩余成员附加架构级语义。</para>
/// </remarks>
internal sealed class GodotArchitectureContext(IArchitectureContext innerContext)
    : IArchitectureContext
{
    private readonly IArchitectureContext _innerContext =
        innerContext ?? throw new ArgumentNullException(nameof(innerContext));

    /// <inheritdoc />
    public TService GetService<TService>() where TService : class
    {
        return _innerContext.GetService<TService>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _innerContext.GetServices<TService>();
    }

    /// <inheritdoc />
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystem<TSystem>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystems<TSystem>();
    }

    /// <inheritdoc />
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModel<TModel>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModels<TModel>();
    }

    /// <inheritdoc />
    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtility<TUtility>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilities<TUtility>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _innerContext.GetServicesByPriority<TService>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _innerContext.GetSystemsByPriority<TSystem>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _innerContext.GetModelsByPriority<TModel>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _innerContext.GetUtilitiesByPriority<TUtility>();
    }

    /// <inheritdoc />
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _innerContext.SendEvent<TEvent>();
    }

    /// <inheritdoc />
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        _innerContext.SendEvent(e);
    }

    /// <inheritdoc />
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return _innerContext.RegisterEvent(handler);
    }

    /// <inheritdoc />
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        _innerContext.UnRegisterEvent(onEvent);
    }

    /// <inheritdoc />
    public IEnvironment GetEnvironment()
    {
        return _innerContext.GetEnvironment();
    }
}

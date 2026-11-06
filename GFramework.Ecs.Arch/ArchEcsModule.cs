// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Ecs.Arch;

/// <summary>
///     把 Arch 的 World 和系统接入当前架构。配置只声明服务，正式 provider 创建并拥有 World；
///     模块负责系统开始、更新与停止，provider 在模块停止后释放资源。
/// </summary>
public sealed class ArchEcsModule : IArchEcsModule
{
    private readonly ArchOptions _options;
    private IIocContainer? _container;
    private bool _isInitialized;
    private readonly List<ArchSystemAdapter<float>> _startedSystems = [];

    /// <summary>
    ///     创建属于单个架构实例的 ECS 服务模块。
    /// </summary>
    /// <param name="options">World 容量和模块优先级。</param>
    /// <param name="enabled">是否参与本架构生命周期。</param>
    public ArchEcsModule(ArchOptions? options = null, bool enabled = true)
    {
        _options = options ?? new ArchOptions();
        IsEnabled = enabled;
    }

    /// <inheritdoc />
    public string ModuleName => nameof(ArchEcsModule);

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <summary>
    ///     声明单一 owning World 工厂；注册阶段不创建原生资源。
    /// </summary>
    /// <param name="container">当前架构的注册容器。</param>
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (!IsEnabled)
            return;

        _container = container;
        container.RegisterPlurality(this);
        container.RegisterFactory<World>(_ => World.Create(_options.WorldCapacity));
    }

    /// <summary>
    ///     在 provider 建立之后解析同一 World 和显式注册的 ArchSystemAdapter 服务，并初始化系统。
    /// </summary>
    public void Initialize()
    {
        if (!IsEnabled || _isInitialized)
            return;

        var container = _container ??
            throw new InvalidOperationException("Register the ECS module before initializing it.");

        // 必须先解析 World，保证系统构造和运行时查询复用同一 owning 单例。
        _ = container.GetRequired<World>();
        foreach (var system in container.GetAllByPriority<ArchSystemAdapter<float>>())
        {
            ((IContextAware)system).SetContext(container.GetContext());
            system.Initialize();
            _startedSystems.Add(system);
        }

        _isInitialized = true;
    }

    /// <summary>
    ///     逆序停止已经成功启动的系统。即使初始化中途失败也执行已启动部分的停止；
    ///     World 及 DI 拥有的系统资源由 provider 释放，避免重复销毁。
    /// </summary>
    /// <returns>系统停止完成的值任务。</returns>
    public ValueTask DestroyAsync()
    {
        List<Exception>? errors = null;
        for (var i = _startedSystems.Count - 1; i >= 0; i--)
        {
            try
            {
                _startedSystems[i].Destroy();
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }

        _startedSystems.Clear();
        _isInitialized = false;
        if (errors is not null)
            throw new AggregateException("One or more ECS systems failed to stop.", errors);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     更新本模块已经启动的系统；未初始化或停止后不触发更新。
    /// </summary>
    /// <param name="deltaTime">当前宿主帧间隔。</param>
    public void Update(float deltaTime)
    {
        if (!_isInitialized)
            return;

        foreach (var system in _startedSystems)
            system.Update(deltaTime);
    }
}

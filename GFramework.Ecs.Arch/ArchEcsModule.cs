// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Core.Abstractions.Ioc;

namespace GFramework.Ecs.Arch;

/// <summary>
///     Arch ECS 模块 - 核心适配器，桥接 Arch 到框架生命周期
/// </summary>
public sealed class ArchEcsModule : IArchEcsModule
{
    private readonly ArchOptions _options;
    private IIocContainer? _container;
    private bool _isInitialized;
    private IReadOnlyList<ArchSystemAdapter<float>> _systems = [];
    private World? _world;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="enabled">是否启用模块</param>
    public ArchEcsModule(ArchOptions? options = null, bool enabled = true)
    {
        _options = options ?? new ArchOptions();
        IsEnabled = enabled;
    }

    /// <summary>
    ///     模块名称
    /// </summary>
    public string ModuleName => nameof(ArchEcsModule);

    /// <summary>
    ///     模块优先级
    /// </summary>
    public int Priority => _options.Priority;

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     注册服务 - 创建并注册 World
    /// </summary>
    public void Register(IIocContainer container)
    {
        if (!IsEnabled) return;

        _container = container;

        // 注册模块自身
        container.RegisterPlurality(this);

        // 创建并注册 World（使用配置的容量）
        _world = World.Create(_options.WorldCapacity);
        container.Register(_world);
    }

    /// <summary>
    ///     初始化 - 从容器获取所有适配器并初始化
    /// </summary>
    public void Initialize()
    {
        if (!IsEnabled || _world == null || _container == null) return;

        // 防止重复初始化
        if (_isInitialized)
        {
            return;
        }

        // 从容器按优先级获取所有适配器
        _systems = _container.GetAllByPriority<ArchSystemAdapter<float>>();

        // 初始化所有系统（会调用 Arch 系统的 Initialize）
        foreach (var system in _systems)
        {
            system.Initialize();
        }

        _isInitialized = true;
    }

    /// <summary>
    ///     异步销毁
    /// </summary>
    public ValueTask DestroyAsync()
    {
        if (!IsEnabled || !_isInitialized)
        {
            return ValueTask.CompletedTask;
        }

        // 销毁所有系统
        foreach (var system in _systems)
        {
            system.Destroy();
        }

        _systems = [];

        // 销毁 World
        if (_world != null)
        {
            World.Destroy(_world);
            _world = null;
        }

        _isInitialized = false;

        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     更新所有 ECS 系统
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Update(float deltaTime)
    {
        if (!IsEnabled) return;

        // 调用所有系统的更新
        foreach (var system in _systems)
        {
            system.Update(deltaTime);
        }
    }
}
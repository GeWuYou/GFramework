// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Architectures;

/// <summary>
/// 注册表初始化钩子抽象基类，简化注册表配置的初始化逻辑
/// </summary>
/// <typeparam name="TRegistry">注册表类型</typeparam>
/// <typeparam name="TConfig">配置类型</typeparam>
public abstract class RegistryInitializationHookBase<TRegistry, TConfig> : IArchitectureLifecycleHook
    where TRegistry : class, IUtility
{
    private readonly IEnumerable<TConfig> _configs;
    private readonly ArchitecturePhase _targetPhase;

    /// <summary>
    /// 初始化注册表初始化钩子
    /// </summary>
    /// <param name="configs">配置集合</param>
    /// <param name="targetPhase">目标执行阶段，默认为 AfterSystemInit</param>
    protected RegistryInitializationHookBase(
        IEnumerable<TConfig> configs,
        ArchitecturePhase targetPhase = ArchitecturePhase.AfterSystemInit)
    {
        _configs = configs;
        _targetPhase = targetPhase;
    }

    /// <summary>
    /// 当架构进入指定阶段时触发的回调方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    /// <param name="architecture">相关的架构实例</param>
    /// <exception cref="ArgumentNullException"><paramref name="architecture" /> 为 <see langword="null" />。</exception>
    /// <remarks>
    /// 当目标注册表未被装入当前架构上下文时，该钩子会保持 no-op，
    /// 以便同一组配置可以安全复用于不包含该注册表的测试或裁剪场景。
    /// </remarks>
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        ArgumentNullException.ThrowIfNull(architecture);

        if (phase != _targetPhase)
        {
            return;
        }

        TRegistry registry;

        try
        {
            registry = architecture.Context.GetUtility<TRegistry>();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        foreach (var config in _configs)
        {
            RegisterConfig(registry, config);
        }
    }

    /// <summary>
    /// 注册单个配置项到注册表
    /// </summary>
    /// <param name="registry">注册表实例</param>
    /// <param name="config">配置项</param>
    protected abstract void RegisterConfig(TRegistry registry, TConfig config);
}

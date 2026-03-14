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
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        if (phase != _targetPhase) return;

        var registry = architecture.Context.GetUtility<TRegistry>();
        if (registry == null) return;

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
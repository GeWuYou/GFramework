using GFramework.Core.Abstractions.enums;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     架构生命周期接口，定义了架构在不同阶段的回调方法
/// </summary>
public interface IArchitectureLifecycle
{
    /// <summary>
    ///     当架构进入指定阶段时触发的回调方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    /// <param name="architecture">相关的架构实例</param>
    void OnPhase(ArchitecturePhase phase, IArchitecture architecture);
}
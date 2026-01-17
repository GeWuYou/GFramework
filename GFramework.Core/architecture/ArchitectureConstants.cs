using System.Collections.Immutable;
using GFramework.Core.Abstractions.enums;

namespace GFramework.Core.architecture;

/// <summary>
///     架构常量类，定义了架构阶段转换规则
/// </summary>
public static class ArchitectureConstants
{
    /// <summary>
    ///     定义架构阶段的线性顺序
    /// </summary>
    /// <remarks>
    ///     架构阶段永远按照此顺序线性进行，无论是否有组件注册
    /// </remarks>
    public static readonly ArchitecturePhase[] PhaseOrder =
    [
        ArchitecturePhase.None,
        ArchitecturePhase.BeforeUtilityInit,
        ArchitecturePhase.AfterUtilityInit,
        ArchitecturePhase.BeforeModelInit,
        ArchitecturePhase.AfterModelInit,
        ArchitecturePhase.BeforeSystemInit,
        ArchitecturePhase.AfterSystemInit,
        ArchitecturePhase.Ready,
        ArchitecturePhase.Destroying,
        ArchitecturePhase.Destroyed
    ];

    /// <summary>
    ///     定义架构阶段之间的有效转换关系
    /// </summary>
    /// <remarks>
    ///     键为当前架构阶段，值为从该阶段可以转换到的下一阶段数组
    ///     架构采用线性状态机模式，只允许顺序转换，但允许从任何阶段转到 FailedInitialization
    /// </remarks>
    public static readonly ImmutableDictionary<ArchitecturePhase, ArchitecturePhase[]> PhaseTransitions =
        new Dictionary<ArchitecturePhase, ArchitecturePhase[]>
        {
            // 正常线性流程
            { ArchitecturePhase.None, [ArchitecturePhase.BeforeUtilityInit] },
            { ArchitecturePhase.BeforeUtilityInit, [ArchitecturePhase.AfterUtilityInit] },
            { ArchitecturePhase.AfterUtilityInit, [ArchitecturePhase.BeforeModelInit] },
            { ArchitecturePhase.BeforeModelInit, [ArchitecturePhase.AfterModelInit] },
            { ArchitecturePhase.AfterModelInit, [ArchitecturePhase.BeforeSystemInit] },
            { ArchitecturePhase.BeforeSystemInit, [ArchitecturePhase.AfterSystemInit] },
            { ArchitecturePhase.AfterSystemInit, [ArchitecturePhase.Ready] },
            { ArchitecturePhase.Ready, [ArchitecturePhase.Destroying] },
            { ArchitecturePhase.Destroying, [ArchitecturePhase.Destroyed] },
            // 失败路径：从任何阶段都可以转到 FailedInitialization
            { ArchitecturePhase.FailedInitialization, [ArchitecturePhase.Destroying] }
        }.ToImmutableDictionary();
}
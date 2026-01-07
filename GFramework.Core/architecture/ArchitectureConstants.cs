using System.Collections.Immutable;
using GFramework.Core.Abstractions.enums;

namespace GFramework.Core.architecture;

/// <summary>
///     架构常量类，定义了架构阶段转换规则
/// </summary>
public static class ArchitectureConstants
{
    /// <summary>
    ///     定义架构阶段之间的有效转换关系
    /// </summary>
    /// <remarks>
    ///     键为当前架构阶段，值为从该阶段可以转换到的下一阶段数组
    /// </remarks>
    public static readonly ImmutableDictionary<ArchitecturePhase, ArchitecturePhase[]> PhaseTransitions =
        new Dictionary<ArchitecturePhase, ArchitecturePhase[]>
        {
            { ArchitecturePhase.None, [ArchitecturePhase.BeforeUtilityInit] },
            {
                ArchitecturePhase.BeforeUtilityInit,
                [ArchitecturePhase.AfterUtilityInit, ArchitecturePhase.FailedInitialization]
            },
            {
                ArchitecturePhase.AfterUtilityInit,
                [ArchitecturePhase.BeforeModelInit, ArchitecturePhase.FailedInitialization]
            },
            {
                ArchitecturePhase.BeforeModelInit, [
                    ArchitecturePhase.AfterModelInit, ArchitecturePhase.FailedInitialization
                ]
            },
            {
                ArchitecturePhase.AfterModelInit, [
                    ArchitecturePhase.BeforeSystemInit, ArchitecturePhase.FailedInitialization
                ]
            },
            {
                ArchitecturePhase.BeforeSystemInit, [
                    ArchitecturePhase.AfterSystemInit, ArchitecturePhase.FailedInitialization
                ]
            },
            { ArchitecturePhase.AfterSystemInit, [ArchitecturePhase.Ready, ArchitecturePhase.FailedInitialization] },
            { ArchitecturePhase.Ready, [ArchitecturePhase.Destroying] },
            { ArchitecturePhase.FailedInitialization, [ArchitecturePhase.Destroying] },
            { ArchitecturePhase.Destroying, [ArchitecturePhase.Destroyed] }
        }.ToImmutableDictionary();
}
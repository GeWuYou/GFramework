using System.Collections.Immutable;
using GFramework.Core.Abstractions.architecture;

namespace GFramework.Core.architecture;

public static class ArchitectureConstants
{
    public static readonly ImmutableDictionary<ArchitecturePhase, ArchitecturePhase[]> PhaseTransitions =
        new Dictionary<ArchitecturePhase, ArchitecturePhase[]>
        {
            { ArchitecturePhase.None, [ArchitecturePhase.BeforeModelInit] },
            { ArchitecturePhase.BeforeModelInit, [ArchitecturePhase.AfterModelInit] },
            { ArchitecturePhase.AfterModelInit, [ArchitecturePhase.BeforeSystemInit] },
            { ArchitecturePhase.BeforeSystemInit, [ArchitecturePhase.AfterSystemInit] },
            { ArchitecturePhase.AfterSystemInit, [ArchitecturePhase.Ready] },
            { ArchitecturePhase.Ready, [ArchitecturePhase.Destroying] },
            { ArchitecturePhase.Destroying, [ArchitecturePhase.Destroyed] }
        }.ToImmutableDictionary();
}
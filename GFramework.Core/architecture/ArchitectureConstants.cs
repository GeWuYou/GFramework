
using System.Collections.Immutable;

namespace GFramework.Core.architecture;

public static class ArchitectureConstants
{
    public static readonly ImmutableDictionary<ArchitecturePhase, ArchitecturePhase[]> PhaseTransitions =
        new Dictionary<ArchitecturePhase, ArchitecturePhase[]>
        {
            { ArchitecturePhase.Created, [ArchitecturePhase.BeforeInit] },
            { ArchitecturePhase.BeforeInit, [ArchitecturePhase.AfterInit] },
            { ArchitecturePhase.AfterInit, [ArchitecturePhase.BeforeModelInit] },
            { ArchitecturePhase.BeforeModelInit, [ArchitecturePhase.AfterModelInit] },
            { ArchitecturePhase.AfterModelInit, [ArchitecturePhase.BeforeSystemInit] },
            { ArchitecturePhase.BeforeSystemInit, [ArchitecturePhase.AfterSystemInit] },
            { ArchitecturePhase.AfterSystemInit, [ArchitecturePhase.Ready] },
            { ArchitecturePhase.Ready, [ArchitecturePhase.Destroying] },
            { ArchitecturePhase.Destroying, [ArchitecturePhase.Destroyed] }
        }.ToImmutableDictionary();
}
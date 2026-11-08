// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Content;

namespace GFramework.Game.Modding;

/// <summary>Opaque proof that a particular content plan's code packages were approved and loaded by a live host.</summary>
public sealed class CodeModAdmission
{
    private readonly ContentLoadPlan _plan;
    private readonly GameModHost _owner;

    /// <summary>Creates admission only after all packages passed approval.</summary>
    internal CodeModAdmission(ContentLoadPlan plan, GameModHost owner)
    {
        _plan = plan;
        _owner = owner;
    }
    /// <summary>Rejects foreign plans and failed/stopped hosts; admission cannot silently bypass mod initialization.</summary>
    internal void Validate(ContentLoadPlan plan)
    {
        if (!ReferenceEquals(plan, _plan) || !_owner.IsUsable)
            throw new InvalidOperationException("Code admission belongs to another plan or an unusable mod host.");
    }
}

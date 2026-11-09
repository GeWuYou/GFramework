// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace DodgeTheCreeps.Advanced;

/// <summary>Routes menu content commands to dedicated selection and persistence services.</summary>
public sealed partial class GameApplication
{
    /// <summary>Opens the content manager.</summary>
    internal Task OpenContentAsync() => _selection.OpenContentAsync();
    /// <summary>Changes next-start content selection after executable-code consent if necessary.</summary>
    internal Task ToggleContentAsync(string id) => _selection.ToggleContentAsync(id);
    /// <summary>Re-approves a changed executable package fingerprint.</summary>
    internal Task ReapproveCodeAsync(string id) => _selection.ReapproveCodeAsync(id);
    /// <summary>Changes the next-start content order.</summary>
    internal Task MoveContentAsync(string id, int direction) => _selection.MoveContentAsync(id, direction);
    /// <summary>Resets next-start content to the base package.</summary>
    internal Task ResetContentAsync() => _selection.ResetContentAsync();
    /// <summary>Selects another currently available arena.</summary>
    internal Task CycleArenaAsync() => _selection.CycleArenaAsync();
    /// <summary>Restores a backup only after the player's explicit confirmation.</summary>
    internal Task RestoreContentBackupAsync() => _saveService.RestoreContentBackupAsync();
}

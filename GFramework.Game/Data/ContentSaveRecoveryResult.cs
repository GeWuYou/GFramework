// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Data;

/// <summary>Returns the committed save and the original-file backup needed to reverse an approved repair.</summary>
/// <typeparam name="TSaveData">Game DTO type.</typeparam>
public sealed class ContentSaveRecoveryResult<TSaveData> where TSaveData : class, IData, new()
{
    /// <summary>Creates the result only after the repaired/imported file has committed.</summary>
    internal ContentSaveRecoveryResult(TSaveData data, string backupKey)
    {
        Data = data;
        BackupKey = backupKey;
    }

    /// <summary>Gets the repaired or imported data which is now safe to enter gameplay.</summary>
    public TSaveData Data { get; }
    /// <summary>Gets a slot-relative backup key accepted by <c>RestoreBackupAsync</c>.</summary>
    public string BackupKey { get; }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Data;

/// <summary>A committed current save and the lossless original backup location.</summary>
/// <typeparam name="TSaveData">Current game DTO.</typeparam>
public sealed class SaveCommitResult<TSaveData> where TSaveData : class, IData, new()
{
    /// <summary>Creates a completed commit result.</summary>
    internal SaveCommitResult(TSaveData data, string backupKey)
    {
        Data = data;
        BackupKey = backupKey;
    }
    /// <summary>Gets a fresh game DTO decoded from the approved candidate.</summary>
    public TSaveData Data
    {
        get;
    }
    /// <summary>Gets the slot-relative backup key.</summary>
    public string BackupKey
    {
        get;
    }
}

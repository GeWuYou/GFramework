using System;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Enums;

namespace GFramework.Game.Tests.Data;

internal sealed record TestDataLocation(
    string Key,
    StorageKinds Kinds = StorageKinds.Local,
    string? Namespace = null,
    IReadOnlyDictionary<string, string>? Metadata = null) : IDataLocation;

internal sealed class TestSaveData : IData
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class TestVersionedSaveData : IVersionedData
{
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    public int Experience { get; set; }

    public int Version { get; set; } = 3;

    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

internal sealed class TestSimpleData : IData
{
    public int Value { get; set; }
}

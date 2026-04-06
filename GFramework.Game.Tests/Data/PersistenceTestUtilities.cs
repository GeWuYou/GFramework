using System;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Enums;

namespace GFramework.Game.Tests.Data;

/// <summary>
///     为持久化测试提供稳定的测试数据位置实现。
/// </summary>
internal sealed class TestDataLocation : IDataLocation
{
    /// <summary>
    ///     初始化测试数据位置。
    /// </summary>
    /// <param name="key">测试使用的存储键。</param>
    /// <param name="kinds">测试使用的存储类型。</param>
    /// <param name="namespaceValue">测试使用的命名空间。</param>
    /// <param name="metadata">附加测试元数据。</param>
    public TestDataLocation(
        string key,
        StorageKinds kinds = StorageKinds.Local,
        string? namespaceValue = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Key = key;
        Kinds = kinds;
        Namespace = namespaceValue;
        Metadata = metadata;
    }

    /// <summary>
    ///     获取测试数据对应的存储键。
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     获取测试数据使用的存储类型。
    /// </summary>
    public StorageKinds Kinds { get; }

    /// <summary>
    ///     获取测试数据使用的命名空间。
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    ///     获取附加到测试位置上的元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }
}

/// <summary>
///     为基础存档仓库测试提供的简单存档模型。
/// </summary>
internal sealed class TestSaveData : IData
{
    /// <summary>
    ///     获取或设置测试存档中的名称字段。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
///     为存档迁移测试提供的版本化存档模型。
/// </summary>
internal sealed class TestVersionedSaveData : IVersionedData
{
    /// <summary>
    ///     获取或设置测试存档中的名称字段。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置测试存档中的等级字段。
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///     获取或设置测试存档中的经验字段。
    /// </summary>
    public int Experience { get; set; }

    /// <summary>
    ///     获取或设置当前测试存档的版本号。
    /// </summary>
    public int Version { get; set; } = 3;

    /// <summary>
    ///     获取或设置测试存档的最后修改时间。
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     为通用持久化测试提供的简单数据模型。
/// </summary>
internal sealed class TestSimpleData : IData
{
    /// <summary>
    ///     获取或设置测试数据中的整数值。
    /// </summary>
    public int Value { get; set; }
}

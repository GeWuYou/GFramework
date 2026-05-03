// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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

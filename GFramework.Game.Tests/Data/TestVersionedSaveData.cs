// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Tests.Data;

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

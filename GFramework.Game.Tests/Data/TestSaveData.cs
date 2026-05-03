// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Tests.Data;

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

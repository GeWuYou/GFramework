// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Tests.Data;

/// <summary>
///     为批量持久化测试提供的另一种数据模型，用于验证运行时类型不会在接口路径上退化。
/// </summary>
internal sealed class TestNamedData : IData
{
    /// <summary>
    ///     获取或设置测试数据中的名称值。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

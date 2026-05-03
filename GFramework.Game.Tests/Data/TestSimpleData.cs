// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Tests.Data;

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

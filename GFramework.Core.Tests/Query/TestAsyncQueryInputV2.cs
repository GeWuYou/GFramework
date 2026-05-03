// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     测试用异步查询输入类V2
/// </summary>
public sealed class TestAsyncQueryInputV2 : IQueryInput
{
    /// <summary>
    ///     获取或设置值
    /// </summary>
    public int Value { get; init; }
}

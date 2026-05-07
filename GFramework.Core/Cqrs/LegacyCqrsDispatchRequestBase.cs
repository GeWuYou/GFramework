// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Cqrs;

/// <summary>
///     为 legacy Command / Query 到自有 CQRS runtime 的桥接请求提供共享的目标对象封装。
/// </summary>
internal abstract class LegacyCqrsDispatchRequestBase(object target)
{
    /// <summary>
    ///     获取当前 bridge request 代理的 legacy 目标对象。
    /// </summary>
    public object Target { get; } = target ?? throw new ArgumentNullException(nameof(target));
}

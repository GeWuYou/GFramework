// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Resource;

namespace GFramework.Core.Resource;

/// <summary>
///     自动释放策略
///     当资源引用计数降为 0 时自动卸载资源
/// </summary>
public class AutoReleaseStrategy : IResourceReleaseStrategy
{
    /// <summary>
    ///     判断是否应该释放资源
    ///     当引用计数降为 0 时返回 true
    /// </summary>
    public bool ShouldRelease(string path, int refCount)
    {
        return refCount <= 0;
    }
}
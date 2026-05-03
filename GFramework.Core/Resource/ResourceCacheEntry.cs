// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Resource;

/// <summary>
///     资源缓存条目。
/// </summary>
internal sealed class ResourceCacheEntry(object resource, Type resourceType)
{
    /// <summary>
    ///     获取缓存中的资源实例。
    /// </summary>
    public object Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <summary>
    ///     获取资源的运行时类型。
    /// </summary>
    public Type ResourceType { get; } = resourceType ?? throw new ArgumentNullException(nameof(resourceType));

    /// <summary>
    ///     获取或设置当前引用计数。
    /// </summary>
    public int ReferenceCount { get; set; }

    /// <summary>
    ///     获取或设置最近访问时间。
    /// </summary>
    public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;
}

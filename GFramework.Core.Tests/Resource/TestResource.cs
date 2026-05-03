// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Resource;

/// <summary>
///     表示 ResourceManager 测试使用的简单资源对象。
/// </summary>
public class TestResource
{
    /// <summary>
    ///     获取或设置资源内容。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置一个值，指示资源是否已经被测试加载器标记为已卸载。
    /// </summary>
    public bool IsDisposed { get; set; }
}

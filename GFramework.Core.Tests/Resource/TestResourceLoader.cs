// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;

namespace GFramework.Core.Tests.Resource;

/// <summary>
///     为 ResourceManager 测试提供可控数据源的资源加载器。
/// </summary>
public class TestResourceLoader : IResourceLoader<TestResource>
{
    private readonly Dictionary<string, string> _resourceData = new(StringComparer.Ordinal);

    /// <summary>
    ///     同步加载指定路径的测试资源。
    /// </summary>
    /// <param name="path">资源路径。</param>
    /// <returns>加载得到的测试资源。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> 为 <see langword="null" />。</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> 为空字符串。</exception>
    /// <exception cref="FileNotFoundException">指定路径的测试资源不存在。</exception>
    public TestResource Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (_resourceData.TryGetValue(path, out var content))
        {
            return new TestResource { Content = content };
        }

        throw new FileNotFoundException($"Resource not found: {path}");
    }

    /// <summary>
    ///     异步加载指定路径的测试资源。
    /// </summary>
    /// <param name="path">资源路径。</param>
    /// <returns>加载得到的测试资源任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> 为 <see langword="null" />。</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> 为空字符串。</exception>
    /// <exception cref="FileNotFoundException">指定路径的测试资源不存在。</exception>
    public Task<TestResource> LoadAsync(string path)
    {
        return Task.FromResult(Load(path));
    }

    /// <summary>
    ///     卸载已加载的测试资源。
    /// </summary>
    /// <param name="resource">要标记为已释放的资源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> 为 <see langword="null" />。</exception>
    public void Unload(TestResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        resource.IsDisposed = true;
    }

    /// <summary>
    ///     判断当前加载器是否包含指定路径的测试资源。
    /// </summary>
    /// <param name="path">资源路径。</param>
    /// <returns>存在对应测试资源时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> 为 <see langword="null" />。</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> 为空字符串。</exception>
    public bool CanLoad(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return _resourceData.ContainsKey(path);
    }

    /// <summary>
    ///     向测试加载器注册一条可返回的资源数据。
    /// </summary>
    /// <param name="path">资源路径。</param>
    /// <param name="content">资源内容。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="path" /> 或 <paramref name="content" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> 为空字符串。</exception>
    public void AddTestData(string path, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(content);
        _resourceData[path] = content;
    }
}

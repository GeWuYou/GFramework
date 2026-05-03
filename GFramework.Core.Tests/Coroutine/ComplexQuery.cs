// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     为 <see cref="QueryCoroutineExtensionsTests" /> 提供复杂对象结果的查询测试替身。
/// </summary>
internal class ComplexQuery : IQuery<ComplexResult>
{
    private IArchitectureContext? _context;

    /// <summary>
    ///     获取或设置测试查询使用的名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置需要聚合的整数集合。
    /// </summary>
    public List<int> Values { get; set; } = new();

    /// <summary>
    ///     获取或设置附加元数据。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     绑定当前查询所属的架构上下文。
    /// </summary>
    /// <param name="context">测试期间由查询管线注入的上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前查询持有的架构上下文。
    /// </summary>
    /// <returns>此前通过 <see cref="SetContext" /> 绑定的上下文实例。</returns>
    public IArchitectureContext GetContext()
    {
        return _context ?? throw new InvalidOperationException(
            $"{nameof(SetContext)} must be called before {nameof(GetContext)}.");
    }

    /// <summary>
    ///     执行查询并生成复杂结果对象。
    /// </summary>
    /// <returns>包含名称、求和值和计数信息的测试结果。</returns>
    public ComplexResult Do()
    {
        return new ComplexResult
        {
            ProcessedName = Name,
            Sum = Values.Sum(),
            Count = Values.Count
        };
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     为 <see cref="QueryCoroutineExtensionsTests" /> 提供布尔结果的整数查询测试替身。
/// </summary>
internal class IntQuery : IQuery<bool>
{
    private IArchitectureContext? _context;

    /// <summary>
    ///     获取或设置参与查询计算的整数值。
    /// </summary>
    public int Value { get; set; }

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
        return _context!;
    }

    /// <summary>
    ///     执行查询并返回布尔结果。
    /// </summary>
    /// <returns>当 <see cref="Value" /> 大于零时返回 <see langword="true" />。</returns>
    public bool Do()
    {
        return Value > 0;
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Query;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureContextTests" /> 提供的测试查询桩。
/// </summary>
public sealed class TestQueryV2 : IQuery<int>
{
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取查询返回值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int Result { get; init; }

    /// <summary>
    ///     执行查询并返回预设结果。
    /// </summary>
    /// <returns>测试预设的查询结果。</returns>
    public int Do()
    {
        return Result;
    }

    /// <summary>
    ///     关联当前查询所属的架构上下文。
    /// </summary>
    /// <param name="context">要保存的架构上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前查询已绑定的架构上下文。
    /// </summary>
    /// <returns>测试期间保存的架构上下文。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

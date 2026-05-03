// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureContextTests" /> 提供的带返回值测试命令桩。
/// </summary>
public sealed class TestCommandWithResultV2 : ICommand<int>
{
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取命令执行结果；该值只能在对象初始化阶段设置。
    /// </summary>
    public int Result { get; init; }

    /// <summary>
    ///     执行测试命令并返回预设结果。
    /// </summary>
    /// <returns>测试预设的命令结果。</returns>
    public int Execute()
    {
        return Result;
    }

    /// <summary>
    ///     关联当前命令所属的架构上下文。
    /// </summary>
    /// <param name="context">要保存的架构上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前命令已绑定的架构上下文。
    /// </summary>
    /// <returns>测试期间保存的架构上下文。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

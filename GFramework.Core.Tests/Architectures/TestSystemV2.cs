// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Systems;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureContextTests" /> 提供的测试系统桩。
/// </summary>
public sealed class TestSystemV2 : ISystem
{
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取或设置测试用标识。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     关联当前系统所属的架构上下文。
    /// </summary>
    /// <param name="context">要保存的架构上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前系统已绑定的架构上下文。
    /// </summary>
    /// <returns>测试期间保存的架构上下文。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }

    /// <summary>
    ///     初始化测试系统。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     销毁测试系统。
    /// </summary>
    public void Destroy()
    {
    }

    /// <summary>
    ///     接收架构阶段切换通知。
    /// </summary>
    /// <param name="phase">当前架构阶段。</param>
    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}

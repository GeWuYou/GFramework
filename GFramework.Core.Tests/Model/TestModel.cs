// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Model;

namespace GFramework.Core.Tests.Model;

/// <summary>
///     测试模型类，用于框架测试目的
/// </summary>
public sealed class TestModel : AbstractModel, ITestModel
{
    public const int DefaultXp = 5;

    /// <summary>
    ///     获取模型是否已初始化的状态
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     初始化模型
    /// </summary>
    public void Initialize()
    {
        Initialized = true;
    }


    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    public int GetCurrentXp { get; } = DefaultXp;

    protected override void OnInit()
    {
    }
}
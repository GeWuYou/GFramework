// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Tests.Model;
using GFramework.Core.Tests.Systems;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     异步测试架构，用于测试异步模型和系统的初始化
/// </summary>
public class AsyncTestArchitecture : TestArchitectureBase
{
    /// <summary>
    ///     异步初始化架构
    /// </summary>
    protected override void OnInitialize()
    {
        // 注册模型
        RegisterModel(new AsyncTestModel());
        // 注册系统
        RegisterSystem(new AsyncTestSystem());
        base.OnInitialize();
    }
}
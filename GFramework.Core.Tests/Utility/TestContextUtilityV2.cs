// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Utility;

namespace GFramework.Core.Tests.Utility;

/// <summary>
///     为 <see cref="AbstractContextUtilityTests" /> 提供的自定义初始化上下文工具测试桩。
/// </summary>
public sealed class TestContextUtilityV2 : AbstractContextUtility
{
    /// <summary>
    ///     获取一个值，该值指示当前工具是否已完成初始化。
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     获取一个值，该值指示当前工具是否已执行销毁逻辑。
    /// </summary>
    public bool Destroyed { get; private set; }

    /// <summary>
    ///     获取一个值，该值指示自定义初始化步骤是否已完成。
    /// </summary>
    public bool CustomInitializationDone { get; private set; }

    /// <summary>
    ///     在基础初始化期间记录自定义初始化步骤已执行。
    /// </summary>
    protected override void OnInit()
    {
        Initialized = true;
        CustomInitializationDone = true;
    }

    /// <summary>
    ///     记录销毁流程已运行，供生命周期测试断言使用。
    /// </summary>
    protected override void OnDestroy()
    {
        Destroyed = true;
    }
}

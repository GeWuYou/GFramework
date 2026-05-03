// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Utility;

namespace GFramework.Core.Tests.Utility;

/// <summary>
///     为 <see cref="AbstractContextUtilityTests" /> 提供的基础上下文工具测试桩。
/// </summary>
public sealed class TestContextUtilityV1 : AbstractContextUtility
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
    ///     获取一个值，该值指示测试专用的 <see cref="OnInit" /> 钩子是否已被调用。
    ///     该标记与 <see cref="Initialized" /> 共享同一执行时机，但单独暴露以便断言钩子本身已运行。
    /// </summary>
    public bool InitCalled { get; private set; }

    /// <summary>
    ///     获取初始化阶段创建的日志记录器，供测试断言使用。
    /// </summary>
    /// <returns>初始化后缓存的日志记录器；初始化前返回 <see langword="null" />。</returns>
    public ILogger? GetLogger()
    {
        return Logger;
    }

    /// <summary>
    ///     记录初始化已发生，并标记测试钩子调用状态。
    /// </summary>
    protected override void OnInit()
    {
        Initialized = true;
        InitCalled = true;
    }

    /// <summary>
    ///     记录销毁流程已运行，供生命周期测试断言使用。
    /// </summary>
    protected override void OnDestroy()
    {
        Destroyed = true;
    }

    /// <summary>
    ///     重置销毁标记，供同一实例的重复生命周期测试在下一轮初始化前清理观测状态。
    /// </summary>
    public void ResetDestroyedStateForTest()
    {
        Destroyed = false;
    }
}

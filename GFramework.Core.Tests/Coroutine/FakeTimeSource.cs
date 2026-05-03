// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     可控制的时间源，用于协程测试
/// </summary>
public sealed class FakeTimeSource : ITimeSource
{
    /// <summary>
    ///     获取当前累计时间
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    ///     获取上一帧的时间增量
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    ///     更新时间源
    /// </summary>
    public void Update()
    {
        // 在测试中，Update 不做任何事情
        // 时间推进由 Advance 方法控制
    }

    /// <summary>
    ///     前进指定的时间
    /// </summary>
    /// <param name="deltaTime">时间增量（秒）</param>
    public void Advance(double deltaTime)
    {
        DeltaTime = deltaTime;
        CurrentTime += deltaTime;
    }

    /// <summary>
    ///     重置时间源到初始状态
    /// </summary>
    public void Reset()
    {
        CurrentTime = 0;
        DeltaTime = 0;
    }
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     为协程测试提供固定时间步长的时间源。
/// </summary>
public sealed class TestTimeSource : ITimeSource
{
    /// <summary>
    ///     获取当前累计时间。
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    ///     获取最近一次更新产生的时间增量。
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    ///     按固定步长推进测试时间，确保调度器测试具有确定性。
    /// </summary>
    public void Update()
    {
        DeltaTime = 0.1;
        CurrentTime += DeltaTime;
    }
}

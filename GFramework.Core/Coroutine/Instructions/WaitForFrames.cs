// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待指定帧数的等待指令类
/// </summary>
/// <param name="frames">需要等待的帧数，最小值为1</param>
public sealed class WaitForFrames(int frames) : IYieldInstruction
{
    /// <summary>
    ///     剩余等待帧数
    /// </summary>
    private int _remaining = Math.Max(1, frames);

    /// <summary>
    ///     更新方法，在每一帧调用时减少剩余帧数
    /// </summary>
    /// <param name="deltaTime">时间间隔（秒）</param>
    public void Update(double deltaTime)
    {
        _remaining--;
    }

    /// <summary>
    ///     获取等待是否完成的状态
    /// </summary>
    public bool IsDone => _remaining <= 0;
}
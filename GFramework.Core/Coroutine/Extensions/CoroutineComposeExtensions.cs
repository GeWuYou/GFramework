// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Extensions;

/// <summary>
///     组合协程，并将内部枚举器的释放责任转交给组合结果。
/// </summary>
public static class CoroutineComposeExtensions
{
    /// <summary>
    ///     等待首个协程自然结束后执行动作；取消或失败时不执行动作。
    /// </summary>
    /// <param name="first">交由结果管理的枚举器。</param>
    /// <param name="next">首个协程成功结束后执行的动作。</param>
    /// <returns>拥有内部枚举器的组合结果。</returns>
    public static IEnumerator<IYieldInstruction> Then(
        this IEnumerator<IYieldInstruction> first,
        Action next)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(next);
        return new OwnedCoroutineEnumerator(new[] { first }, next);
    }

    /// <summary>
    ///     顺序执行两个协程；提前停止时也释放尚未开始的后续枚举器。
    /// </summary>
    /// <param name="first">首个枚举器。</param>
    /// <param name="second">首个成功结束后才推进的枚举器。</param>
    /// <returns>拥有两个内部枚举器的组合结果。</returns>
    public static IEnumerator<IYieldInstruction> Then(
        this IEnumerator<IYieldInstruction> first,
        IEnumerator<IYieldInstruction> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return new OwnedCoroutineEnumerator(new[] { first, second });
    }
}

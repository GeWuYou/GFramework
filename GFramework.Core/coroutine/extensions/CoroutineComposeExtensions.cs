// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.extensions;

/// <summary>
///     提供协程组合扩展方法的静态类
/// </summary>
public static class CoroutineComposeExtensions
{
    /// <summary>
    ///     将一个协程枚举器与一个动作组合，先执行完第一个协程，然后执行指定的动作
    /// </summary>
    /// <param name="first">第一个协程枚举器</param>
    /// <param name="next">在第一个协程完成后要执行的动作</param>
    /// <returns>组合后的协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> Then(
        this IEnumerator<IYieldInstruction> first,
        Action next)
    {
        // 执行第一个协程的所有步骤
        while (first.MoveNext())
            yield return first.Current;

        // 第一个协程完成后执行指定动作
        next();
    }

    /// <summary>
    ///     将两个协程枚举器顺序组合，先执行完第一个协程，再执行第二个协程
    /// </summary>
    /// <param name="first">第一个协程枚举器</param>
    /// <param name="second">第二个协程枚举器</param>
    /// <returns>组合后的协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> Then(
        this IEnumerator<IYieldInstruction> first,
        IEnumerator<IYieldInstruction> second)
    {
        // 执行第一个协程的所有步骤
        while (first.MoveNext())
            yield return first.Current;

        // 第一个协程完成后执行第二个协程的所有步骤
        while (second.MoveNext())
            yield return second.Current;
    }
}
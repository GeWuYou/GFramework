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
using GFramework.Core.coroutine;
using Godot;

namespace GFramework.Godot.coroutine;

/// <summary>
/// 提供协程相关的扩展方法，用于在Godot环境中管理协程的生命周期。
/// </summary>
public static class CoroutineNodeExtensions
{
    /// <summary>
    /// 启动协程的扩展方法。
    /// </summary>
    /// <param name="coroutine">要启动的协程枚举器。</param>
    /// <param name="segment">协程运行的时间段，默认为 Process。</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程。</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）。</returns>
    public static CoroutineHandle RunCoroutine(this IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process, string? tag = null)
    {
        return Timing.RunCoroutine(coroutine, segment, tag);
    }

    /// <summary>
    /// 让协程在指定节点被销毁时自动取消。
    /// </summary>
    /// <param name="coroutine">要包装的协程枚举器。</param>
    /// <param name="node">用于检查是否存活的节点。</param>
    /// <returns>包装后的协程枚举器。</returns>
    public static IEnumerator<IYieldInstruction> CancelWith(this IEnumerator<IYieldInstruction> coroutine,
        Node node)
    {
        while (Timing.IsNodeAlive(node) && coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    /// 让协程在任一节点被销毁时自动取消。
    /// </summary>
    /// <param name="coroutine">要包装的协程枚举器。</param>
    /// <param name="node1">第一个用于检查是否存活的节点。</param>
    /// <param name="node2">第二个用于检查是否存活的节点。</param>
    /// <returns>包装后的协程枚举器。</returns>
    public static IEnumerator<IYieldInstruction> CancelWith(this IEnumerator<IYieldInstruction> coroutine,
        Node node1, Node node2)
    {
        while (Timing.IsNodeAlive(node1) &&
               Timing.IsNodeAlive(node2) &&
               coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    /// 让协程在多个节点都被销毁时自动取消。
    /// </summary>
    /// <param name="coroutine">要包装的协程枚举器。</param>
    /// <param name="nodes">用于检查是否存活的节点数组。</param>
    /// <returns>包装后的协程枚举器。</returns>
    public static IEnumerator<IYieldInstruction> CancelWith(this IEnumerator<IYieldInstruction> coroutine,
        params Node[] nodes)
    {
        // 持续执行协程直到任一节点被销毁或协程执行完毕
        while (nodes.AllNodesAlive() && coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    /// 检查所有节点是否都处于存活状态。
    /// </summary>
    /// <param name="nodes">要检查的节点数组。</param>
    /// <returns>如果所有节点都存活则返回 true，否则返回 false。</returns>
    private static bool AllNodesAlive(this Node[] nodes)
    {
        return nodes.All(Timing.IsNodeAlive);
    }
}
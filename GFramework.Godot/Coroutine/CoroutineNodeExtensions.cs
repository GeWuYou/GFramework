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

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using Godot;

namespace GFramework.Godot.Coroutine;

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
    ///     以指定节点作为生命周期所有者运行协程。
    /// </summary>
    /// <param name="owner">拥有该协程生命周期的节点。</param>
    /// <param name="coroutine">要启动的协程枚举器。</param>
    /// <param name="segment">协程运行的时间段。</param>
    /// <param name="tag">协程标签。</param>
    /// <param name="cancellationToken">可选取消令牌。</param>
    /// <returns>返回协程句柄。</returns>
    public static CoroutineHandle RunCoroutine(
        this Node owner,
        IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return Timing.RunOwnedCoroutine(owner, coroutine, segment, tag, cancellationToken);
    }

    /// <summary>Runs a structured flow in the node's default Supervisor scope.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="factory">Creates a flow that can explicitly own child tasks.</param>
    /// <param name="segment">The initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The flow handle.</returns>
    public static CoroutineHandle RunCoroutine(this Node owner,
        Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default)
    {
        return Timing.RunOwnedCoroutine(owner, factory, segment, tag, cancellationToken);
    }

    /// <summary>Creates an independent scope that cannot outlive its node.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="policy">The immutable failure policy; independent node work defaults to Supervisor.</param>
    /// <returns>The new node-owned scope.</returns>
    public static CoroutineScope CreateCoroutineScope(this Node owner,
        CoroutineFailurePolicy policy = CoroutineFailurePolicy.Supervisor)
    {
        return Timing.CreateScope(owner, policy);
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
        return new NodeBoundCoroutine(coroutine, [node]);
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
        return new NodeBoundCoroutine(coroutine, [node1, node2]);
    }

    /// <summary>
    /// 让协程在任一绑定节点退出场景树时取消，包括当前等待尚未结束的情况。
    /// </summary>
    /// <param name="coroutine">要包装的协程枚举器。</param>
    /// <param name="nodes">用于检查是否存活的节点数组。</param>
    /// <returns>包装后的协程枚举器。</returns>
    public static IEnumerator<IYieldInstruction> CancelWith(this IEnumerator<IYieldInstruction> coroutine,
        params Node[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return new NodeBoundCoroutine(coroutine, nodes);
    }
}

using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using Godot;

namespace GFramework.Godot.coroutine;

public static class CoroutineExtensions
{
    /// <summary>
    ///     启动协程的扩展方法
    /// </summary>
    public static CoroutineHandle RunCoroutine(
        this IEnumerator<IYieldInstruction> coroutine,
        Segment segment = Segment.Process,
        string? tag = null)
    {
        return Timing.RunCoroutine(coroutine, segment, tag);
    }

    /// <summary>
    ///     让协程在指定节点被销毁时自动取消
    /// </summary>
    public static IEnumerator<IYieldInstruction> CancelWith(
        this IEnumerator<IYieldInstruction> coroutine,
        Node node)
    {
        while (Timing.IsNodeAlive(node) && coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    ///     让协程在任一节点被销毁时自动取消
    /// </summary>
    public static IEnumerator<IYieldInstruction> CancelWith(
        this IEnumerator<IYieldInstruction> coroutine,
        Node node1,
        Node node2)
    {
        while (Timing.IsNodeAlive(node1) &&
               Timing.IsNodeAlive(node2) &&
               coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    ///     让协程在多个节点都被销毁时自动取消
    /// </summary>
    /// <param name="coroutine">要执行的协程枚举器</param>
    /// <param name="nodes">用于检查是否存活的节点数组</param>
    /// <returns>包装后的协程枚举器</returns>
    public static IEnumerator<IYieldInstruction> CancelWith(
        this IEnumerator<IYieldInstruction> coroutine,
        params Node[] nodes)
    {
        // 持续执行协程直到任一节点被销毁或协程执行完毕
        while (AllNodesAlive(nodes) && coroutine.MoveNext())
            yield return coroutine.Current;
    }

    /// <summary>
    ///     检查所有节点是否都处于存活状态
    /// </summary>
    /// <param name="nodes">要检查的节点数组</param>
    /// <returns>如果所有节点都存活则返回true，否则返回false</returns>
    private static bool AllNodesAlive(Node[] nodes)
    {
        return nodes.All(Timing.IsNodeAlive);
    }
}
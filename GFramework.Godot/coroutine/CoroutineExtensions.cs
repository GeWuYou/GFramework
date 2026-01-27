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
    public static IEnumerator<IYieldInstruction> CancelWith(
        this IEnumerator<IYieldInstruction> coroutine,
        params Node[] nodes)
    {
        while (AllNodesAlive(nodes) && coroutine.MoveNext())
            yield return coroutine.Current;
    }

    private static bool AllNodesAlive(Node[] nodes)
    {
        foreach (var node in nodes)
            if (!Timing.IsNodeAlive(node))
                return false;

        return true;
    }
}
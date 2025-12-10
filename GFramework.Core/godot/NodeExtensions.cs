#if GODOT
using System.Threading.Tasks;
using Godot;
namespace GFramework.framework.godot;

/// <summary>
/// 节点扩展方法类，提供对Godot节点的扩展功能
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// 安全地将节点加入删除队列，在下一帧开始时释放节点资源
    /// </summary>
    /// <param name="node">要释放的节点实例</param>
    public static void QueueFreeX(this Node node)
    {
        // 检查节点是否为空
        if (node is null)
        {
            return;
        }

        // 检查节点实例是否有效
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        // 检查节点是否已经加入删除队列
        if (node.IsQueuedForDeletion())
        {
            return;
        }

        // 延迟调用QueueFree方法，避免在当前帧中直接删除节点
        node.CallDeferred(Node.MethodName.QueueFree);
    }

    /// <summary>
    /// 立即释放节点资源，不等待下一帧
    /// </summary>
    /// <param name="node">要立即释放的节点实例</param>
    public static void FreeX(this Node node)
    {
        // 检查节点是否为空
        if (node is null)
        {
            return;
        }

        // 检查节点实例是否有效
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        // 检查节点是否已经加入删除队列
        if (node.IsQueuedForDeletion())
        {
            return;
        }

        // 立即释放节点资源
        node.Free();
    }

    /// <summary>
    /// 如果节点尚未进入场景树，则等待 ready 信号。
    /// 如果已经在场景树中，则立刻返回。
    /// </summary>
    public static async Task WaitUntilReady(this Node node)
    {
        if (!node.IsInsideTree())
        {
            await node.ToSignal(node, Node.SignalName.Ready);
        }
    }

    /// <summary>
    /// 检查节点是否有效：
    /// 1. 非 null
    /// 2. Godot 实例仍然存在（未被释放）
    /// 3. 已经加入 SceneTree
    /// </summary>
    public static bool IsValidNode(this Node node)
    {
        return node is not null &&
               GodotObject.IsInstanceValid(node) &&
               node.IsInsideTree();
    }

    /// <summary>
    /// 检查节点是否无效：
    /// 1. 为 null，或者
    /// 2. Godot 实例已被释放，或者
    /// 3. 尚未加入 SceneTree
    /// 
    /// 返回 true 表示该节点不可用。
    /// </summary>
    public static bool IsInvalidNode(this Node node)
    {
        return node is null ||
               !GodotObject.IsInstanceValid(node) ||
               !node.IsInsideTree();
    }
}
#endif
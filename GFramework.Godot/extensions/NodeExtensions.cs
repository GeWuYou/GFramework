using Godot;

namespace GFramework.Godot.extensions;

/// <summary>
///     节点扩展方法类，提供对Godot节点的扩展功能
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    ///     安全地将节点加入删除队列，在下一帧开始时释放节点资源
    /// </summary>
    /// <param name="node">要释放的节点实例</param>
    public static void QueueFreeX(this Node? node)
    {
        // 检查节点是否为空
        if (node is null) return;

        // 检查节点实例是否有效
        if (!GodotObject.IsInstanceValid(node)) return;

        // 检查节点是否已经加入删除队列
        if (node.IsQueuedForDeletion()) return;

        // 延迟调用QueueFree方法，避免在当前帧中直接删除节点
        node.CallDeferred(Node.MethodName.QueueFree);
    }

    /// <summary>
    ///     立即释放节点资源，不等待下一帧
    /// </summary>
    /// <param name="node">要立即释放的节点实例</param>
    public static void FreeX(this Node? node)
    {
        // 检查节点是否为空
        if (node is null) return;

        // 检查节点实例是否有效
        if (!GodotObject.IsInstanceValid(node)) return;

        // 检查节点是否已经加入删除队列
        if (node.IsQueuedForDeletion()) return;

        // 立即释放节点资源
        node.Free();
    }

    /// <summary>
    ///     如果节点尚未进入场景树，则等待 ready 信号。
    ///     如果已经在场景树中，则立刻返回。
    /// </summary>
    public static async Task WaitUntilReady(this Node node)
    {
        if (!node.IsInsideTree()) await node.ToSignal(node, Node.SignalName.Ready);
    }

    /// <summary>
    ///     检查节点是否有效：
    ///     1. 非 null
    ///     2. Godot 实例仍然存在（未被释放）
    ///     3. 已经加入 SceneTree
    /// </summary>
    public static bool IsValidNode(this Node? node)
    {
        return node is not null &&
               GodotObject.IsInstanceValid(node) &&
               node.IsInsideTree();
    }

    /// <summary>
    ///     检查节点是否无效：
    ///     1. 为 null，或者
    ///     2. Godot 实例已被释放，或者
    ///     3. 尚未加入 SceneTree
    ///     返回 true 表示该节点不可用。
    /// </summary>
    public static bool IsInvalidNode(this Node? node)
    {
        return node is null ||
               !GodotObject.IsInstanceValid(node) ||
               !node.IsInsideTree();
    }

    /// <summary>
    ///     将当前节点的输入事件标记为已处理，防止事件继续向父节点传播。
    /// </summary>
    /// <param name="node">要处理输入事件的节点实例</param>
    public static void SetInputAsHandled(this Node node)
    {
        // 获取节点的视口并标记输入事件为已处理
        node.GetViewport().SetInputAsHandled();
    }

    /// <summary>
    ///     设置节点所在场景树的暂停状态
    /// </summary>
    /// <param name="node">要操作的节点对象</param>
    /// <param name="paused">暂停状态标识，默认为true表示暂停，false表示恢复运行</param>
    public static void Paused(this Node node, bool paused = true)
    {
        var tree = node.GetTree();
        tree.Paused = paused;
    }

    /// <summary>
    ///     查找指定名称的子节点并将其转换为指定类型
    /// </summary>
    /// <typeparam name="T">要转换到的目标节点类型</typeparam>
    /// <param name="node">要在其子节点中进行查找的父节点</param>
    /// <param name="name">要查找的子节点名称</param>
    /// <param name="recursive">是否递归查找所有层级的子节点，默认为true</param>
    /// <returns>找到的子节点转换为指定类型后的结果，如果未找到或转换失败则返回null</returns>
    public static T? FindChildX<T>(this Node node, string name, bool recursive = true)
        where T : Node
    {
        var child = node.FindChild(name, recursive, false);
        return child as T;
    }

    /// <summary>
    ///     获取指定路径的节点，如果不存在则创建一个新的节点
    /// </summary>
    /// <typeparam name="T">节点类型，必须继承自Node且具有无参构造函数</typeparam>
    /// <param name="node">父节点</param>
    /// <param name="path">节点路径</param>
    /// <returns>找到的现有节点或新创建的节点</returns>
    public static T GetOrCreateNode<T>(this Node node, string path)
        where T : Node, new()
    {
        // 尝试获取现有节点
        if (node.GetNodeOrNull<T>(path) is { } found)
            return found;

        // 创建新节点并添加到父节点
        var created = new T();
        node.AddChild(created);
        created.Name = path;
        return created;
    }

    /// <summary>
    ///     异步添加子节点并等待其准备就绪
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="child">要添加的子节点</param>
    /// <returns>异步任务</returns>
    public static async Task AddChildX(this Node parent, Node child)
    {
        parent.AddChild(child);
        await child.WaitUntilReady();
    }

    /// <summary>
    ///     获取父节点并将其转换为指定类型
    /// </summary>
    /// <typeparam name="T">要转换到的目标节点类型</typeparam>
    /// <param name="node">当前节点</param>
    /// <returns>父节点转换为指定类型后的结果，如果转换失败则返回null</returns>
    public static T? GetParentX<T>(this Node node) where T : Node
    {
        return node.GetParent() as T;
    }

    /// <summary>
    ///     获取场景树的根节点的第一个子节点
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    /// <returns>根节点的第一个子节点</returns>
    public static Node GetRootNodeX(this Node node)
    {
        return node.GetTree().Root.GetChild(0);
    }

    /// <summary>
    ///     遍历节点的所有子节点，并对指定类型的子节点执行特定操作
    /// </summary>
    /// <typeparam name="T">要筛选的节点类型</typeparam>
    /// <param name="node">扩展方法的目标节点</param>
    /// <param name="action">对符合条件的子节点执行的操作</param>
    public static void ForEachChild<T>(this Node node, Action<T> action) where T : Node
    {
        foreach (var child in node.GetChildren())
            if (child is T t)
                action(t);
    }

    /// <summary>
    ///     禁用节点所在场景树的输入处理功能
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    public static void DisableInput(this Node node)
    {
        // 检查根节点是否为Viewport类型，如果是则禁用GUI输入
        if (node.GetTree().Root is Viewport vp)
            vp.GuiDisableInput = true;
    }

    /// <summary>
    ///     启用节点所在场景树的输入处理功能
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    public static void EnableInput(this Node node)
    {
        // 检查根节点是否为Viewport类型，如果是则启用GUI输入
        if (node.GetTree().Root is Viewport vp)
            vp.GuiDisableInput = false;
    }

    /// <summary>
    ///     打印节点的路径信息到控制台
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    public static void LogNodePath(this Node node)
    {
        GD.Print($"[NodePath] {node.GetPath()}");
    }

    /// <summary>
    ///     以树形结构递归打印节点及其所有子节点的名称
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    /// <param name="indent">缩进字符串，用于显示层级关系</param>
    public static void PrintTreeX(this Node node, string indent = "")
    {
        GD.Print($"{indent}- {node.Name}");

        // 递归打印所有子节点
        foreach (var child in node.GetChildren())
            child.PrintTreeX(indent + "  ");
    }

    /// <summary>
    ///     安全地延迟调用指定方法，确保节点有效后再执行
    /// </summary>
    /// <param name="node">扩展方法的目标节点</param>
    /// <param name="method">要延迟调用的方法名</param>
    public static void SafeCallDeferred(this Node? node, string method)
    {
        // 检查节点是否为空且实例是否有效，有效时才执行延迟调用
        if (node.IsValidNode())
            node!.CallDeferred(method);
    }


    /// <summary>
    ///     将指定节点转换为目标类型T
    /// </summary>
    /// <typeparam name="T">目标节点类型，必须继承自Node</typeparam>
    /// <param name="node">要转换的节点对象，可以为null</param>
    /// <returns>转换后的目标类型节点</returns>
    /// <exception cref="InvalidCastException">当节点无效或类型不匹配时抛出</exception>
    public static T OfType<T>(this Node? node) where T : Node
    {
        // 检查节点是否有效且类型匹配
        if (node.IsValidNode() && node is T t)
            return t;
        throw new InvalidCastException($"Cannot cast {node} to {typeof(T)}");
    }
}
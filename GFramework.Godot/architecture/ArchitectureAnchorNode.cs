using Godot;

namespace GFramework.Godot.architecture;

/// <summary>
/// 架构锚点节点类，用于在Godot场景树中作为架构组件的根节点
/// 该类提供了退出时的回调绑定功能，可以在节点从场景树中移除时执行清理操作
/// </summary>
public partial class ArchitectureAnchorNode : Node
{
    private Action? _onExit;
    /// <summary>
    /// 绑定节点退出时的回调动作
    /// </summary>
    /// <param name="onExit">当节点从场景树退出时要执行的动作</param>
    public void Bind(Action onExit)
    {
        if (_onExit != null)
        {
            GD.PushWarning(
                $"{nameof(ArchitectureAnchorNode)} already bound. Rebinding will override previous callback.");
        }
        _onExit = onExit;
    }
    
    /// <summary>
    /// 当节点从场景树中移除时调用此方法
    /// 执行绑定的退出回调并清理引用
    /// </summary>
    public override void _ExitTree()
    {
        var callback = _onExit;
        _onExit = null;
        callback?.Invoke();
    }
}


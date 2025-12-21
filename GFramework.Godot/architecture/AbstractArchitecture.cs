using GFramework.Core.architecture;
using Godot;

namespace GFramework.Godot.architecture;

/// <summary>
/// 抽象架构类，为特定类型的架构提供基础实现框架
/// </summary>
/// <typeparam name="T">架构的具体类型，必须继承自Architecture且能被实例化</typeparam>
public abstract class AbstractArchitecture<T> : Architecture<T> where T : Architecture<T>, new()
{
    private const string ArchitectureName = "__GFramework__Architecture__Anchor";
    /// <summary>
    /// 初始化架构，按顺序注册模型、系统和工具
    /// </summary>
    protected override void Init()
    {
        RegisterModels();
        RegisterSystems();
        RegisterUtilities();
        AttachToGodotLifecycle();
    }
    
    /// <summary>
    /// 将架构绑定到Godot生命周期中，确保在场景树销毁时能够正确清理资源
    /// 通过创建一个锚节点来监听场景树的销毁事件
    /// </summary>
    private void AttachToGodotLifecycle()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        // 防止重复挂载（热重载 / 多次 Init）
        if (tree.Root.GetNodeOrNull(ArchitectureName) != null)
            return;

        var anchor = new ArchitectureAnchorNode
        {
            Name = ArchitectureName
        };

        anchor.Bind(Destroy);

        tree.Root.CallDeferred(Node.MethodName.AddChild, anchor);
    }
    
    /// <summary>
    /// 注册工具抽象方法，由子类实现具体的工具注册逻辑
    /// </summary>
    protected abstract void RegisterUtilities();

    /// <summary>
    /// 注册系统抽象方法，由子类实现具体系统注册逻辑
    /// </summary>
    protected abstract void RegisterSystems();

    /// <summary>
    /// 注册模型抽象方法，由子类实现具体模型注册逻辑
    /// </summary>
    protected abstract void RegisterModels();
}


using GFramework.Core.architecture;
using GFramework.Core.constants;
using GFramework.Godot.extensions;
using Godot;

namespace GFramework.Godot.architecture;

/// <summary>
/// 抽象架构类，为特定类型的架构提供基础实现框架。
/// 此类负责管理架构的初始化、生命周期绑定以及扩展模块的安装与销毁。
/// </summary>
/// <typeparam name="T">架构的具体类型，必须继承自Architecture且能被实例化。</typeparam>
public abstract class AbstractArchitecture<T> : Architecture<T> where T : Architecture<T>, new()
{
    
    /// <summary>
    /// 架构锚点节点的唯一标识名称
    /// 用于在Godot场景树中创建和查找架构锚点节点
    /// </summary>
    private const string ArchitectureAnchorName = $"__{GFrameworkConstants.FrameworkName}__ArchitectureAnchor__";
    
    /// <summary>
    /// 存储所有已安装的Godot架构扩展组件列表
    /// 用于在架构销毁时正确清理所有扩展资源
    /// </summary>
    private readonly List<IGodotModule<T>> _extensions = [];
    
    /// <summary>
    /// 架构锚点节点引用
    /// 用于将架构绑定到Godot生命周期并作为扩展节点的父节点
    /// </summary>
    private ArchitectureAnchor? _anchor;

    /// <summary>
    /// 获取架构根节点。如果尚未初始化或已被销毁，则抛出异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">当架构未准备就绪时抛出。</exception>
    protected Node ArchitectureRoot => _anchor ?? throw new InvalidOperationException("Architecture root not ready");

    /// <summary>
    /// 标记架构是否已被销毁的状态标志
    /// 用于防止架构被重复销毁，确保资源清理只执行一次
    /// </summary>
    private bool _destroyed;


    /// <summary>
    /// 初始化架构，按顺序注册模型、系统和工具。
    /// 包括将架构绑定到Godot生命周期并调用模块安装逻辑。
    /// </summary>
    protected override void Init()
    {
        AttachToGodotLifecycle();
        InstallModules();
    }

    /// <summary>
    /// 安装模块抽象方法，由子类实现具体的模块注册逻辑。
    /// 子类应在此方法中完成所有模型、系统及工具的注册工作。
    /// </summary>
    protected abstract void InstallModules();

    /// <summary>
    /// 将架构绑定到Godot生命周期中，确保在场景树销毁时能够正确清理资源。
    /// 通过创建一个锚节点来监听场景树的销毁事件。
    /// </summary>
    private void AttachToGodotLifecycle()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        // 防止重复挂载（热重载 / 多次 Init）
        if (tree.Root.GetNodeOrNull(ArchitectureAnchorName) != null)
            return;

        _anchor = new ArchitectureAnchor
        {
            Name = ArchitectureAnchorName
        };

        _anchor.Bind(Destroy);

        tree.Root.CallDeferred(Node.MethodName.AddChild, _anchor);
    }

    
    /// <summary>
    /// 安装Godot模块扩展
    /// </summary>
    /// <typeparam name="TModule">模块类型，必须实现IGodotModule接口</typeparam>
    /// <param name="module">要安装的模块实例</param>
    /// <returns>异步任务</returns>
    protected async Task InstallGodotModule<TModule>(TModule module) where TModule : IGodotModule<T>
    {
        module.Install(this);
        
        // 检查锚点是否已初始化，未初始化则抛出异常
        if (_anchor == null)
            throw new InvalidOperationException("Anchor not initialized");
            
        // 等待锚点准备就绪
        await _anchor.WaitUntilReady();
        
        // 延迟调用将扩展节点添加为锚点的子节点
        _anchor.CallDeferred(Node.MethodName.AddChild, module.Node);

        // 调用扩展的附加回调方法
        module.OnAttach(this);
        
        // 将扩展添加到扩展集合中
        _extensions.Add(module);
    }



    /// <summary>
    /// 销毁架构及其相关资源。
    /// 调用所有已安装扩展的OnDetach方法，并清空扩展列表。
    /// 若已被销毁则直接返回。
    /// </summary>
    public override void Destroy()
    {
        if (_destroyed)
            return;

        _destroyed = true;
        foreach (var ext in _extensions)
            ext.OnDetach();

        _extensions.Clear();

        base.Destroy();
    }
}
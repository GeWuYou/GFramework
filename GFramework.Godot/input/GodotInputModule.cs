using GFramework.Core.architecture;
using GFramework.Core.constants;
using GFramework.Game.input;
using GFramework.Godot.architecture;
using GFramework.Godot.extensions;
using Godot;

namespace GFramework.Godot.input;

/// <summary>
/// Godot输入模块类，用于管理Godot游戏引擎中的输入系统
/// </summary>
/// <typeparam name="T">架构类型，必须继承自Architecture且具有无参构造函数</typeparam>
public sealed class GodotInputModule<T> : AbstractGodotModule<T>
    where T : Architecture<T>, new()
{
    private GodotInputBridge? _node;
    /// <summary>
    /// 架构锚点节点的唯一标识名称
    /// 用于在Godot场景树中创建和查找架构锚点节点
    /// </summary>
    private const string GodotInputBridgeName = $"__${GFrameworkConstants.FrameworkName}__GodotInputBridge__";
    /// <summary>
    /// 获取模块对应的节点对象
    /// </summary>
    /// <exception cref="InvalidOperationException">当节点尚未创建时抛出异常</exception>
    public override Node Node => _node 
                                 ?? throw new InvalidOperationException("Node not created yet");
    private InputSystem _inputSystem = null!;
    
    /// <summary>
    /// 当模块被附加到架构时调用此方法
    /// </summary>
    /// <param name="architecture">要附加到的架构实例</param>
    public override void OnAttach(Architecture<T> architecture)
    {
        // 创建Godot输入桥接节点并绑定输入系统
        _node = new GodotInputBridge { Name = GodotInputBridgeName};
        _node.Bind(_inputSystem);
    }

    /// <summary>
    /// 当模块从架构中分离时调用此方法
    /// </summary>
    public override void OnDetach()
    {
        // 释放节点资源并清理引用
        Node.QueueFreeX();
        _node = null;
    }

    /// <summary>
    /// 安装模块时调用此方法，用于获取所需的系统组件
    /// </summary>
    /// <param name="architecture">当前架构实例</param>
    public override void Install(IArchitecture architecture)
    {
        // 从架构中获取输入系统实例
        _inputSystem = architecture.GetSystem<InputSystem>()!;
    }
}

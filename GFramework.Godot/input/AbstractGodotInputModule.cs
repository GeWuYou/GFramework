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
public abstract class AbstractGodotInputModule<T> : AbstractGodotModule<T>
    where T : Architecture<T>, new()
{
    private GodotInputBridge? _node;

    /// <summary>
    /// 启用默认的输入转换器
    /// </summary>
    protected virtual bool EnableDefaultTranslator => false;

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
        var inputSystem = architecture.GetSystem<InputSystem>()!;
        if (EnableDefaultTranslator)
        {
            // 注册输入转换器
            inputSystem.RegisterTranslator(new GodotInputTranslator(), true);
        }

        RegisterTranslator(inputSystem);
        // 创建Godot输入桥接节点并绑定输入系统
        _node = new GodotInputBridge { Name = GodotInputBridgeName };
        _node.Bind(inputSystem);
    }

    /// <summary>
    /// 注册翻译器的抽象方法，由子类实现具体的注册逻辑
    /// </summary>
    /// <param name="inputSystem">输入系统实例</param>
    protected abstract void RegisterTranslator(InputSystem inputSystem);
}
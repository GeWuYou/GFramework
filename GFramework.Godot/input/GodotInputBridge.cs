using GFramework.Game.input;
using Godot;

namespace GFramework.Godot.input;

/// <summary>
/// Godot输入桥接类，用于将Godot的输入事件转换为游戏框架的输入事件
/// </summary>
public partial class GodotInputBridge : Node
{
    private InputSystem _inputSystem = null!;

    /// <summary>
    /// 绑定输入系统
    /// </summary>
    /// <param name="inputSystem">要绑定的输入系统实例</param>
    public void Bind(InputSystem inputSystem)
    {
        _inputSystem = inputSystem;
    }

    /// <summary>
    /// 捕获阶段：最早
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        _inputSystem.HandleRaw(
            new GodotRawInput(@event, GodotInputPhase.Capture)
        );
    }

    /// <summary>
    /// 冒泡阶段：UI 未处理
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        _inputSystem.HandleRaw(
            new GodotRawInput(@event, GodotInputPhase.Bubble)
        );
    }
}

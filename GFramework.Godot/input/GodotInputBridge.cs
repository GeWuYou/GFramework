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
    /// 处理输入事件的回调方法
    /// </summary>
    /// <param name="event">Godot输入事件</param>
    public override void _Input(InputEvent @event)
    {
        var gameEvent = Translate(@event);
        if (gameEvent == null)
        {
            return;
        }

        _inputSystem.Handle(gameEvent);
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 将Godot输入事件翻译为游戏框架输入事件
    /// </summary>
    /// <param name="evt">Godot输入事件</param>
    /// <returns>翻译后的游戏输入事件，如果无法翻译则返回null</returns>
    private static IGameInputEvent? Translate(InputEvent evt)
    {
        // 处理动作输入事件
        if (evt is InputEventAction action)
        {
            return new InputEvents.KeyInputEvent(
                action.Action,
                action.Pressed,
                false
            );
        }

        // 鼠标按钮
        if (evt is InputEventMouseButton mb)
        {
            return new InputEvents.PointerInputEvent<Vector2>(
                mb.Position,
                Vector2.Zero,
                (int)mb.ButtonIndex,
                mb.Pressed
            );
        }

        // 鼠标移动
        if (evt is InputEventMouseMotion mm)
        {
            return new InputEvents.PointerInputEvent<Vector2>(
                mm.Position,
                mm.Relative,
                0,
                false
            );
        }

        return null;
    }
}

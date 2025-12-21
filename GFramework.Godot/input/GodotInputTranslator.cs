using GFramework.Game.input;
using Godot;

namespace GFramework.Godot.input;

/// <summary>
/// 将Godot引擎的输入事件转换为游戏通用输入事件的翻译器
/// </summary>
public sealed class GodotInputTranslator : IInputTranslator
{
    /// <summary>
    /// 尝试将原始输入对象转换为游戏输入事件
    /// </summary>
    /// <param name="rawInput">原始输入对象，应为Godot的InputEvent类型</param>
    /// <param name="gameEvent">输出参数，转换成功时返回对应的游戏输入事件，失败时返回null</param>
    /// <returns>转换成功返回true，否则返回false</returns>
    public bool TryTranslate(object rawInput, out IGameInputEvent gameEvent)
    {
        gameEvent = null!;

        // 检查输入是否为Godot的InputEvent类型
        if (rawInput is not InputEvent evt)
            return false;

        // Action
        // 处理动作输入事件（如键盘按键、手柄按钮等）
        if (evt is InputEventAction action)
        {
            gameEvent = new InputEvents.KeyInputEvent(
                action.Action,
                action.Pressed,
                false
            );
            return true;
        }

        // Mouse button
        // 处理鼠标按钮输入事件
        if (evt is InputEventMouseButton mb)
        {
            gameEvent = new InputEvents.PointerInputEvent<Vector2>(
                mb.Position,
                Vector2.Zero,
                (int)mb.ButtonIndex,
                mb.Pressed
            );
            return true;
        }

        // Mouse motion
        // 处理鼠标移动输入事件
        if (evt is InputEventMouseMotion mm)
        {
            gameEvent = new InputEvents.PointerInputEvent<Vector2>(
                mm.Position,
                mm.Relative,
                0,
                false
            );
            return true;
        }

        return false;
    }
}

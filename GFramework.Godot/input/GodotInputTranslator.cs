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

        if (rawInput is not GodotRawInput raw)
            return false;

        var evt = raw.Event;

        // 支持多个输入阶段：Capture, Bubble, 和其他阶段
        // 在拖拽过程中，可能需要在Capture阶段也处理输入
        if (raw.Phase != GodotInputPhase.Bubble && raw.Phase != GodotInputPhase.Capture)
            return false;

        // Action
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

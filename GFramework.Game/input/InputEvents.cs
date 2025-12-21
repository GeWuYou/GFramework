namespace GFramework.Game.input;

public static class InputEvents
{
    /// <summary>
    /// 按键输入事件
    /// </summary>
    /// <param name="Action">按键操作名称</param>
    /// <param name="Pressed">按键是否被按下，true表示按下，false表示释放</param>
    /// <param name="Echo">是否为回显事件，用于处理按键重复触发</param>
    public sealed record KeyInputEvent(
        string Action,
        bool Pressed,
        bool Echo
    ) : IGameInputEvent;

    /// <summary>
    /// 指针/鼠标输入事件
    /// </summary>
    /// <typeparam name="TVector2">二维向量类型</typeparam>
    /// <param name="Position">指针当前位置坐标</param>
    /// <param name="Delta">指针位置变化量</param>
    /// <param name="Button">鼠标按键编号，0表示左键，1表示右键，2表示中键</param>
    /// <param name="Pressed">按键是否被按下，true表示按下，false表示释放</param>
    public sealed record PointerInputEvent<TVector2>(
        TVector2 Position,
        TVector2 Delta,
        int Button,
        bool Pressed
    ) : IGameInputEvent where TVector2 : struct;
}
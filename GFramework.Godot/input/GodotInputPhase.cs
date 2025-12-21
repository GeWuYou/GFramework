namespace GFramework.Godot.input;

/// <summary>
/// 输入处理阶段枚举，用于区分Godot引擎中不同的输入处理阶段
/// </summary>
public enum GodotInputPhase
{
    /// <summary>
    /// 捕获阶段，在_Input方法中处理输入事件
    /// 这是输入事件的第一个处理阶段，事件会沿着节点树向下传递
    /// </summary>
    Capture,    // _Input

    /// <summary>
    /// 冒泡阶段，在_UnhandledInput方法中处理输入事件
    /// 这是输入事件的第二个处理阶段，未被处理的事件会向上冒泡传递
    /// </summary>
    Bubble      // _UnhandledInput
}

using Godot;

namespace GFramework.Godot.input;

/// <summary>
/// 表示Godot原始输入数据的只读结构体
/// </summary>
/// <param name="evt">输入事件对象</param>
/// <param name="phase">输入阶段</param>
public readonly struct GodotRawInput(InputEvent evt, GodotInputPhase phase)
{
    /// <summary>
    /// 获取输入事件对象
    /// </summary>
    public readonly InputEvent Event = evt;
    
    /// <summary>
    /// 获取输入阶段
    /// </summary>
    public readonly GodotInputPhase Phase = phase;
}

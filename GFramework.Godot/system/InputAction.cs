namespace GFramework.Godot.system;

/// <summary>
/// 输入动作类，表示游戏中的一种输入行为
/// </summary>
public class InputAction
{
    /// <summary>
    /// 动作名称
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// 动作类型
    /// </summary>
    public InputActionType ActionType { get; }
    
    /// <summary>
    /// 默认按键绑定
    /// </summary>
    public string[] DefaultBindings { get; }
    
    /// <summary>
    /// 当前按键绑定
    /// </summary>
    public string[] CurrentBindings { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="defaultBindings">默认按键绑定</param>
    public InputAction(string name, InputActionType actionType, params string[] defaultBindings)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ActionType = actionType;
        DefaultBindings = defaultBindings ?? throw new ArgumentNullException(nameof(defaultBindings));
        CurrentBindings = (string[])defaultBindings.Clone();
    }

    /// <summary>
    /// 重置为默认按键绑定
    /// </summary>
    public void ResetToDefault()
    {
        CurrentBindings = (string[])DefaultBindings.Clone();
    }
    
    /// <summary>
    /// 设置新的按键绑定
    /// </summary>
    /// <param name="bindings">新的按键绑定</param>
    public void SetBindings(params string[] bindings)
    {
        CurrentBindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
    }
}

/// <summary>
/// 输入动作类型枚举
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// 按钮类型，如跳跃、射击等
    /// </summary>
    Button,
    
    /// <summary>
    /// 轴类型，如移动、视角控制等
    /// </summary>
    Axis,
    
    /// <summary>
    /// 向量类型，如二维移动控制
    /// </summary>
    Vector
}
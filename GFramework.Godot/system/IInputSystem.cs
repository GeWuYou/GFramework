using GFramework.Core.system;

namespace GFramework.Godot.system;

/// <summary>
/// 输入系统接口，用于统一管理游戏中的输入操作和键位绑定
/// </summary>
public interface IInputSystem : ISystem
{
    /// <summary>
    /// 设置指定动作的按键绑定
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <param name="keyCode">按键码</param>
    void SetBinding(string actionName, string keyCode);
    
    /// <summary>
    /// 获取指定动作的按键绑定
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>绑定的按键码</returns>
    string GetBinding(string actionName);
    
    /// <summary>
    /// 检查指定动作是否正在被执行
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>如果动作正在执行则返回true，否则返回false</returns>
    bool IsActionPressed(string actionName);
    
    /// <summary>
    /// 检查指定动作是否刚刚开始执行
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>如果动作刚刚开始执行则返回true，否则返回false</returns>
    bool IsActionJustPressed(string actionName);
    
    /// <summary>
    /// 检查指定动作是否刚刚停止执行
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>如果动作刚刚停止执行则返回true，否则返回false</returns>
    bool IsActionJustReleased(string actionName);
    
    /// <summary>
    /// 添加输入动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <param name="defaultKeyCode">默认按键绑定</param>
    void AddAction(string actionName, string defaultKeyCode);
    
    /// <summary>
    /// 移除输入动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    void RemoveAction(string actionName);
    
    /// <summary>
    /// 保存输入配置到文件
    /// </summary>
    void SaveConfiguration();
    
    /// <summary>
    /// 从文件加载输入配置
    /// </summary>
    void LoadConfiguration();
    
    /// <summary>
    /// 更新输入系统状态，应在每帧调用
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    void Update(double delta);
}
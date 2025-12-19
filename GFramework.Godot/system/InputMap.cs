namespace GFramework.Godot.system;

/// <summary>
/// 输入映射类，管理所有的输入动作及其绑定
/// </summary>
public class InputMap
{
    private readonly Dictionary<string, InputAction> _actions = new();
    
    /// <summary>
    /// 添加输入动作
    /// </summary>
    /// <param name="action">输入动作</param>
    public void AddAction(InputAction action)
    {
        _actions[action.Name] = action;
    }
    
    /// <summary>
    /// 移除输入动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    public void RemoveAction(string actionName)
    {
        _actions.Remove(actionName);
    }
    
    /// <summary>
    /// 获取输入动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>输入动作，如果不存在则返回null</returns>
    public InputAction GetAction(string actionName)
    {
        return _actions.GetValueOrDefault(actionName);
    }
    
    /// <summary>
    /// 获取所有输入动作
    /// </summary>
    /// <returns>输入动作列表</returns>
    public IEnumerable<InputAction> GetAllActions()
    {
        return _actions.Values;
    }
    
    /// <summary>
    /// 检查是否存在指定名称的动作
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <returns>存在返回true，否则返回false</returns>
    public bool ContainsAction(string actionName)
    {
        return _actions.ContainsKey(actionName);
    }
    
    /// <summary>
    /// 根据按键查找绑定的动作
    /// </summary>
    /// <param name="keyCode">按键码</param>
    /// <returns>绑定到该按键的所有动作</returns>
    public IEnumerable<InputAction> FindActionsByBinding(string keyCode)
    {
        return _actions.Values.Where(action => 
            action.CurrentBindings.Contains(keyCode));
    }
}
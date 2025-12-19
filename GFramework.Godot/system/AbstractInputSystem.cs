namespace GFramework.Godot.system;

/// <summary>
/// 抽象输入系统基类，实现了IInputSystem接口的基本功能
/// </summary>
public abstract class AbstractInputSystem : AbstractAssetCatalogSystem, IInputSystem
{
    // 存储动作名称与按键绑定的映射关系
    private readonly Dictionary<string, string> _actionBindings = new();
    
    // 存储动作名称与当前状态的映射关系
    private readonly Dictionary<string, bool> _actionStates = new();
    
    // 存储动作名称与上一帧状态的映射关系
    private readonly Dictionary<string, bool> _previousActionStates = new();

    /// <inheritdoc />
    public virtual void SetBinding(string actionName, string keyCode)
    {
        _actionBindings[actionName] = keyCode;
    }

    /// <inheritdoc />
    public virtual string GetBinding(string actionName)
    {
        return _actionBindings.TryGetValue(actionName, out var binding) ? binding : string.Empty;
    }

    /// <inheritdoc />
    public virtual bool IsActionPressed(string actionName)
    {
        return _actionStates.TryGetValue(actionName, out var state) && state;
    }

    /// <inheritdoc />
    public virtual bool IsActionJustPressed(string actionName)
    {
        var current = _actionStates.TryGetValue(actionName, out var currentState) && currentState;
        var previous = _previousActionStates.TryGetValue(actionName, out var previousState) && previousState;
        return current && !previous;
    }

    /// <inheritdoc />
    public virtual bool IsActionJustReleased(string actionName)
    {
        var current = _actionStates.TryGetValue(actionName, out var currentState) && currentState;
        var previous = _previousActionStates.TryGetValue(actionName, out var previousState) && previousState;
        return !current && previous;
    }

    /// <inheritdoc />
    public virtual void AddAction(string actionName, string defaultKeyCode)
    {
        if (!_actionBindings.ContainsKey(actionName))
        {
            _actionBindings[actionName] = defaultKeyCode;
            _actionStates[actionName] = false;
            _previousActionStates[actionName] = false;
        }
    }

    /// <inheritdoc />
    public virtual void RemoveAction(string actionName)
    {
        _actionBindings.Remove(actionName);
        _actionStates.Remove(actionName);
        _previousActionStates.Remove(actionName);
    }

    /// <inheritdoc />
    public abstract void SaveConfiguration();

    /// <inheritdoc />
    public abstract void LoadConfiguration();

    /// <inheritdoc />
    public abstract void Update(double delta);

    /// <summary>
    /// 更新输入状态，在每一帧调用
    /// </summary>
    protected virtual void UpdateInputStates()
    {
        // 保存当前状态作为下一帧的前一状态
        foreach (var kvp in _actionStates)
        {
            _previousActionStates[kvp.Key] = kvp.Value;
        }

        // 更新当前状态
        foreach (var kvp in _actionBindings)
        {
            var actionName = kvp.Key;
            var keyCode = kvp.Value;
            
            // 这里需要根据具体平台和引擎实现具体的按键检测逻辑
            // 当前只是一个占位实现
            _actionStates[actionName] = CheckKeyPressed(keyCode);
        }
    }

    /// <summary>
    /// 检查特定按键是否被按下
    /// </summary>
    /// <param name="keyCode">按键码</param>
    /// <returns>按键是否被按下</returns>
    protected virtual bool CheckKeyPressed(string keyCode)
    {
        // 这里需要根据Godot引擎的具体实现来检查按键状态
        // 目前只是示例实现
        return false;
    }
}
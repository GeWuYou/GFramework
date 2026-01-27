using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI切换事件，包含UI切换过程中的上下文信息
/// </summary>
public sealed class UiTransitionEvent
{
    /// <summary>
    ///     用户自定义数据字典，用于Handler之间传递数据
    /// </summary>
    private readonly Dictionary<string, object> _context = new(StringComparer.Ordinal);

    /// <summary>
    ///     源UI的标识符，切换前的UI key
    /// </summary>
    public string FromUiKey { get; init; } = string.Empty;

    /// <summary>
    ///     目标UI的标识符，切换后的UI key
    /// </summary>
    public string? ToUiKey { get; init; } = string.Empty;

    /// <summary>
    ///     UI切换类型
    /// </summary>
    public UiTransitionType TransitionType { get; init; }

    /// <summary>
    ///     UI切换策略
    /// </summary>
    public UiTransitionPolicy Policy { get; init; }

    /// <summary>
    ///     UI进入参数
    /// </summary>
    public IUiPageEnterParam? EnterParam { get; init; }

    /// <summary>
    ///     获取用户自定义数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <param name="defaultValue">默认值（当键不存在或类型不匹配时返回）</param>
    /// <returns>用户数据</returns>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_context.TryGetValue(key, out var obj) && obj is T value)
            return value;
        return defaultValue;
    }

    /// <summary>
    ///     尝试获取用户自定义数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <param name="value">输出值</param>
    /// <returns>是否成功获取</returns>
    public bool TryGet<T>(string key, out T value)
    {
        if (_context.TryGetValue(key, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    ///     设置用户自定义数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <param name="value">数据值</param>
    public void Set<T>(string key, T value)
    {
        _context[key] = value!;
    }

    /// <summary>
    ///     检查是否存在指定的用户数据键
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>是否存在</returns>
    public bool Has(string key)
    {
        return _context.ContainsKey(key);
    }

    /// <summary>
    ///     移除指定的用户数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>是否成功移除</returns>
    public bool Remove(string key)
    {
        return _context.Remove(key);
    }
}
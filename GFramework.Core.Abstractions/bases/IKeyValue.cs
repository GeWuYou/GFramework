namespace GFramework.Core.Abstractions.bases;

/// <summary>
/// 表示键值对的接口，定义了通用的键值对数据结构契约
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
/// <typeparam name="TValue">值的类型</typeparam>
public interface IKeyValue<out TKey, out TValue>
{
    /// <summary>
    /// 获取键值对中的键
    /// </summary>
    TKey Key { get; }

    /// <summary>
    /// 获取键值对中的值
    /// </summary>
    TValue Value { get; }
}
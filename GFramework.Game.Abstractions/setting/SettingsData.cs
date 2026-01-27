using System.Reflection;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     设置数据抽象基类，提供默认的 Reset() 实现
/// </summary>
public abstract class SettingsData : ISettingsData
{
    /// <summary>
    ///     重置设置为默认值
    ///     使用反射将所有属性重置为它们的默认值
    /// </summary>
    public virtual void Reset()
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanWrite || !prop.CanRead) continue;

            var defaultValue = GetDefaultValue(prop.PropertyType);
            prop.SetValue(this, defaultValue);
        }
    }

    /// <summary>
    ///     获取指定类型的默认值
    /// </summary>
    /// <param name="type">要获取默认值的类型</param>
    /// <returns>类型的默认值</returns>
    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
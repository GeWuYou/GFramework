using GFramework.Core.Abstractions.serializer;
using Newtonsoft.Json;

namespace GFramework.Game.serializer;

/// <summary>
///     JSON序列化器实现类，用于将对象序列化为JSON字符串或将JSON字符串反序列化为对象
/// </summary>
public sealed class JsonSerializer
    : IRuntimeTypeSerializer
{
    /// <summary>
    ///     将指定类型的对象序列化为JSON字符串
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="value">要序列化的对象实例</param>
    /// <returns>序列化后的JSON字符串</returns>
    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value);
    }

    /// <summary>
    ///     将JSON字符串反序列化为指定类型的对象
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型</typeparam>
    /// <param name="data">要反序列化的JSON字符串数据</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="ArgumentException">当无法反序列化数据时抛出</exception>
    public T Deserialize<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data)
               ?? throw new ArgumentException("Cannot deserialize data");
    }

    /// <summary>
    ///     将对象序列化为JSON字符串（使用运行时类型）
    /// </summary>
    /// <param name="obj">要序列化的对象实例</param>
    /// <param name="type">对象的运行时类型</param>
    /// <returns>序列化后的JSON字符串</returns>
    public string Serialize(object obj, Type type)
    {
        return JsonConvert.SerializeObject(obj, type, null);
    }

    /// <summary>
    ///     将JSON字符串反序列化为指定类型的对象（使用运行时类型）
    /// </summary>
    /// <param name="data">要反序列化的JSON字符串数据</param>
    /// <param name="type">反序列化目标类型</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="ArgumentException">当无法反序列化到指定类型时抛出</exception>
    public object Deserialize(string data, Type type)
    {
        return JsonConvert.DeserializeObject(data, type)
               ?? throw new ArgumentException($"Cannot deserialize to {type.Name}");
    }
}
using GFramework.Game.Abstractions.serializer;
using Newtonsoft.Json;

namespace GFramework.Game.serializer;

/// <summary>
/// JSON序列化器实现类，用于将对象序列化为JSON字符串或将JSON字符串反序列化为对象
/// </summary>
public sealed class JsonSerializer : ISerializer
{
    /// <summary>
    /// 将指定的对象序列化为JSON字符串
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="value">要序列化的对象实例</param>
    /// <returns>序列化后的JSON字符串</returns>
    public string Serialize<T>(T value)
        => JsonConvert.SerializeObject(value);

    /// <summary>
    /// 将JSON字符串反序列化为指定类型的对象
    /// </summary>
    /// <typeparam name="T">要反序列化的对象类型</typeparam>
    /// <param name="data">包含JSON数据的字符串</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="ArgumentException">当无法反序列化数据时抛出</exception>
    public T Deserialize<T>(string data)
        => JsonConvert.DeserializeObject<T>(data) ?? throw new ArgumentException("Cannot deserialize data");
}
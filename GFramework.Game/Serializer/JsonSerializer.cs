using GFramework.Core.Abstractions.Serializer;
using Newtonsoft.Json;

namespace GFramework.Game.Serializer;

/// <summary>
///     JSON序列化器实现类，用于将对象序列化为JSON字符串或将JSON字符串反序列化为对象
/// </summary>
public sealed class JsonSerializer
    : IRuntimeTypeSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    ///     初始化 JSON 序列化器。
    /// </summary>
    /// <param name="settings">可选的 Newtonsoft.Json 配置；不提供时使用默认配置。</param>
    public JsonSerializer(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();
    }

    /// <summary>
    ///     获取当前序列化器使用的 Newtonsoft.Json 配置实例。
    /// </summary>
    public JsonSerializerSettings Settings => _settings;

    /// <summary>
    ///     获取当前序列化器使用的自定义转换器集合。
    /// </summary>
    public IList<JsonConverter> Converters => _settings.Converters;

    /// <summary>
    ///     将指定类型的对象序列化为JSON字符串
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="value">要序列化的对象实例</param>
    /// <returns>序列化后的JSON字符串</returns>
    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, _settings);
    }


    /// <summary>
    ///     将对象序列化为JSON字符串（使用运行时类型）
    /// </summary>
    /// <param name="obj">要序列化的对象实例</param>
    /// <param name="type">对象的运行时类型</param>
    /// <returns>序列化后的JSON字符串</returns>
    public string Serialize(object obj, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return JsonConvert.SerializeObject(obj, type, _settings);
    }

    /// <summary>
    ///     将JSON字符串反序列化为指定类型的对象
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型</typeparam>
    /// <param name="data">要反序列化的JSON字符串数据</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="InvalidOperationException">当无法反序列化数据时抛出</exception>
    public T Deserialize<T>(string data)
    {
        return (T)DeserializeCore(
            data,
            typeof(T),
            static (json, type, settings) => JsonConvert.DeserializeObject<T>(json, settings));
    }

    /// <summary>
    ///     将JSON字符串反序列化为指定类型的对象（使用运行时类型）
    /// </summary>
    /// <param name="data">要反序列化的JSON字符串数据</param>
    /// <param name="type">反序列化目标类型</param>
    /// <returns>反序列化后的对象实例</returns>
    /// <exception cref="InvalidOperationException">当无法反序列化到指定类型时抛出</exception>
    public object Deserialize(string data, Type type)
    {
        return DeserializeCore(
            data,
            type,
            static (json, targetType, settings) => JsonConvert.DeserializeObject(json, targetType, settings));
    }

    private object DeserializeCore(
        string data,
        Type targetType,
        Func<string, Type, JsonSerializerSettings, object?> deserialize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(deserialize);

        try
        {
            var result = deserialize(data, targetType, _settings);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"Deserialization returned null for target type '{targetType.FullName}'.");
            }

            return result;
        }
        catch (Exception ex) when (ex is InvalidCastException)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize JSON to target type '{targetType.FullName}'.",
                ex);
        }
    }
}
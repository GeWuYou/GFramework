// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Serializer;
using Newtonsoft.Json;

namespace GFramework.Game.Serializer;

/// <summary>
///     基于 Newtonsoft.Json 的运行时 JSON 序列化器。
/// </summary>
/// <remarks>
///     该类型会直接持有并复用外部提供的 <see cref="JsonSerializerSettings" /> 实例及其转换器集合，而不会在构造时复制配置。
///     请在组合根或启动阶段完成全部配置，并在注册给其他组件后将这些配置视为只读；否则在并发调用期间同时修改设置或转换器集合可能导致不可预测行为。
/// </remarks>
public sealed class JsonSerializer
    : IRuntimeTypeSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    ///     初始化 JSON 序列化器。
    /// </summary>
    /// <param name="settings">
    ///     可选的 Newtonsoft.Json 配置实例；不提供时使用默认配置。
    ///     传入的实例会被当前序列化器直接复用，后续对该实例的修改会影响所有后续序列化与反序列化调用。
    /// </param>
    public JsonSerializer(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();
    }

    /// <summary>
    ///     获取当前序列化器使用的 Newtonsoft.Json 配置实例。
    /// </summary>
    /// <remarks>
    ///     返回的是当前序列化器持有的活动配置实例，适合在启动阶段补充 contract resolver、格式化策略或 converter。
    ///     一旦该序列化器被共享给其他组件，应避免再修改返回值，以免破坏调用方对并发读行为的假设。
    /// </remarks>
    public JsonSerializerSettings Settings => _settings;

    /// <summary>
    ///     获取当前序列化器使用的自定义转换器集合。
    /// </summary>
    /// <remarks>
    ///     该集合与 <see cref="Settings" /> 的 <see cref="JsonSerializerSettings.Converters" /> 引用相同。
    ///     请在注册序列化器前完成 converter 配置，并避免在序列化器已经发布后继续增删转换器。
    /// </remarks>
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
            static (json, _, settings) => JsonConvert.DeserializeObject<T>(json, settings));
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

        object? result;

        try
        {
            result = deserialize(data, targetType, _settings);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize JSON to target type '{targetType.FullName}'.",
                ex);
        }

        if (result == null)
        {
            throw new InvalidOperationException(
                $"Deserialization returned null for target type '{targetType.FullName}'.");
        }

        return result;
    }
}

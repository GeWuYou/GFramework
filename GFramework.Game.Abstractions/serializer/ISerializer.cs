using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.serializer;

/// <summary>
/// 定义序列化器接口，提供对象序列化和反序列化的通用方法
/// </summary>
public interface ISerializer : IUtility
{
    /// <summary>
    /// 将指定的对象序列化为字符串
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型</typeparam>
    /// <param name="value">要序列化的对象实例</param>
    /// <returns>序列化后的字符串表示</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// 将字符串数据反序列化为指定类型的对象
    /// </summary>
    /// <typeparam name="T">要反序列化的目标对象类型</typeparam>
    /// <param name="data">包含序列化数据的字符串</param>
    /// <returns>反序列化后的对象实例</returns>
    T Deserialize<T>(string data);
}
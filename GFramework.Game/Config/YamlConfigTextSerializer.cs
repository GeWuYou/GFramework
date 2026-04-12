namespace GFramework.Game.Config;

/// <summary>
///     提供可复用的 YAML 文本序列化入口，供生成配置绑定与宿主写回流程共享。
/// </summary>
public static class YamlConfigTextSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .DisableAliases()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
        .Build();

    /// <summary>
    ///     将配置对象序列化为 YAML 文本。
    /// </summary>
    /// <typeparam name="TValue">配置对象类型。</typeparam>
    /// <param name="value">要序列化的配置对象。</param>
    /// <returns>带尾随换行的 YAML 文本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value" /> 为 <see langword="null" /> 时抛出。</exception>
    public static string Serialize<TValue>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var yaml = Serializer.Serialize(value);
        return yaml.EndsWith('\n')
            ? yaml
            : $"{yaml}{Environment.NewLine}";
    }
}

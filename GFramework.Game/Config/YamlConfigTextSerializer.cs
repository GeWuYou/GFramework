// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GFramework.Game.Config;

/// <summary>
///     提供可复用的 YAML 文本序列化入口，供生成配置绑定与宿主写回流程共享。
/// </summary>
public static class YamlConfigTextSerializer
{
    /// <summary>
    ///     将配置对象序列化为 YAML 文本，并统一以 LF 作为尾随换行。
    ///     该约定与底层 YamlDotNet 输出保持一致，避免不同操作系统的宿主行尾约定影响生成结果。
    /// </summary>
    /// <typeparam name="TValue">配置对象类型。</typeparam>
    /// <param name="value">要序列化的配置对象。</param>
    /// <returns>带尾随 LF 换行的 YAML 文本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value" /> 为 <see langword="null" /> 时抛出。</exception>
    public static string Serialize<TValue>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Build one serializer per call so the helper does not rely on undocumented
        // cross-thread safety guarantees from YamlDotNet's serializer implementation.
        var yaml = CreateSerializer().Serialize(value);
        return yaml.EndsWith('\n')
            ? yaml
            : $"{yaml}\n";
    }

    /// <summary>
    ///     创建与运行时配置绑定共享的 YAML 序列化器。
    /// </summary>
    /// <returns>复用统一命名与默认值策略的序列化器。</returns>
    private static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .Build();
    }
}

using GFramework.Game.Config;

namespace GFramework.Godot.Config;

/// <summary>
///     描述 Godot YAML 配置加载器的初始化约定。
/// </summary>
public sealed class GodotYamlConfigLoaderOptions
{
    /// <summary>
    ///     获取或设置配置源根目录。
    ///     默认值为 <c>res://</c>，表示从项目资源路径读取 YAML 与 schema 文本。
    /// </summary>
    public string SourceRootPath { get; init; } = "res://";

    /// <summary>
    ///     获取或设置运行时缓存根目录。
    ///     当 <see cref="SourceRootPath" /> 在当前环境下无法直接映射为普通文件系统目录时，
    ///     加载器会先把所需文本资产复制到这里，再交给底层 <see cref="YamlConfigLoader" />。
    /// </summary>
    public string RuntimeCacheRootPath { get; init; } = "user://config_cache";

    /// <summary>
    ///     获取或设置本次启动会访问到的配置表来源描述。
    ///     Godot 导出态无法假设任意文本目录都可被枚举，因此调用方应显式提供参与本轮加载的配置目录与 schema 文件。
    /// </summary>
    public IReadOnlyCollection<GodotYamlConfigTableSource> TableSources { get; init; } =
        Array.Empty<GodotYamlConfigTableSource>();

    /// <summary>
    ///     获取或设置用于配置底层 <see cref="YamlConfigLoader" /> 的回调。
    ///     调用方通常应在这里调用生成器产出的 <c>RegisterAllGeneratedConfigTables()</c>，
    ///     或显式注册当前场景所需的手写表定义。
    /// </summary>
    public Action<YamlConfigLoader>? ConfigureLoader { get; init; }
}

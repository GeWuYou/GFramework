using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     描述官方配置启动帮助器的初始化约定。
///     该选项对象把配置根目录、表注册回调和热重载策略收敛到一个稳定入口，
///     让消费项目不必在多个启动脚本里重复拼装加载器细节。
/// </summary>
public sealed class GameConfigBootstrapOptions
{
    /// <summary>
    ///     获取或设置配置根目录。
    ///     该路径会直接传给 <see cref="YamlConfigLoader" /> 作为 YAML 与 schema 的共同根目录。
    /// </summary>
    public string RootPath { get; init; } = string.Empty;

    /// <summary>
    ///     获取或设置用于配置 <see cref="YamlConfigLoader" /> 的回调。
    ///     调用方通常应在这里调用生成器产出的 <c>RegisterAllGeneratedConfigTables()</c>，
    ///     或显式注册当前场景所需的手写表定义。
    /// </summary>
    public Action<YamlConfigLoader>? ConfigureLoader { get; init; }

    /// <summary>
    ///     获取或设置要复用的配置注册表。
    ///     为空时启动帮助器会创建默认的 <see cref="ConfigRegistry" /> 实例。
    /// </summary>
    public IConfigRegistry? Registry { get; init; }

    /// <summary>
    ///     获取或设置是否在初次加载成功后立即启用开发期热重载。
    /// </summary>
    public bool EnableHotReload { get; init; }

    /// <summary>
    ///     获取或设置初始化阶段启用热重载时使用的选项。
    ///     当 <see cref="EnableHotReload" /> 为 <see langword="false" /> 时，该值会被忽略。
    /// </summary>
    public YamlConfigHotReloadOptions? HotReloadOptions { get; init; }
}

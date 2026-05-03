// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     描述开发期热重载的可选行为。
///     该选项对象集中承载回调和防抖等可扩展参数，
///     以避免后续继续在 <see cref="YamlConfigLoader.EnableHotReload(GFramework.Game.Abstractions.Config.IConfigRegistry,YamlConfigHotReloadOptions?)" />
///     上堆叠额外重载。
/// </summary>
public sealed class YamlConfigHotReloadOptions
{
    /// <summary>
    ///     获取或设置单个配置表重载成功后的可选回调。
    /// </summary>
    public Action<string>? OnTableReloaded { get; init; }

    /// <summary>
    ///     获取或设置单个配置表重载失败后的可选回调。
    ///     当失败来自加载器本身时，异常通常为 <see cref="GFramework.Game.Abstractions.Config.ConfigLoadException" />。
    /// </summary>
    public Action<string, Exception>? OnTableReloadFailed { get; init; }

    /// <summary>
    ///     获取或设置文件系统事件的防抖延迟。
    ///     默认值为 200 毫秒，用于吸收编辑器保存时的短时间重复触发。
    /// </summary>
    public TimeSpan DebounceDelay { get; init; } = TimeSpan.FromMilliseconds(200);
}
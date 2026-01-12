namespace GFramework.Godot.setting;

/// <summary>
/// 音频总线映射配置类，用于定义音频系统中不同类型的音频总线名称
/// </summary>
public sealed class AudioBusMap
{
    /// <summary>
    /// 主音频总线名称，默认值为"Master"
    /// </summary>
    public string Master { get; init; } = "Master";

    /// <summary>
    /// 背景音乐音频总线名称，默认值为"BGM"
    /// </summary>
    public string Bgm { get; init; } = "BGM";

    /// <summary>
    /// 音效音频总线名称，默认值为"SFX"
    /// </summary>
    public string Sfx { get; init; } = "SFX";
}
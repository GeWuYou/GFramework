namespace GFramework.Godot.setting.data;

/// <summary>
///     音频总线映射设置
///     定义了游戏中不同音频类型的总线名称配置
/// </summary>
public class AudioBusMap
{
    /// <summary>
    ///     主音频总线名称
    ///     默认值为"Master"
    /// </summary>
    public string Master { get; set; } = "Master";

    /// <summary>
    ///     背景音乐总线名称
    ///     默认值为"BGM"
    /// </summary>
    public string Bgm { get; set; } = "BGM";

    /// <summary>
    ///     音效总线名称
    ///     默认值为"SFX"
    /// </summary>
    public string Sfx { get; set; } = "SFX";

    /// <summary>
    ///     重置音频总线映射设置为默认值
    /// </summary>
    public void Reset()
    {
        Master = "Master";
        Bgm = "BGM";
        Sfx = "SFX";
    }
}
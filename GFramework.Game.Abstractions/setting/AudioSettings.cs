namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 音频设置类，用于管理游戏中的音频配置
/// </summary>
public class AudioSettings : ISettingsData
{
    /// <summary>
    /// 获取或设置主音量，控制所有音频的总体音量
    /// </summary>
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>
    /// 获取或设置背景音乐音量，控制BGM的播放音量
    /// </summary>
    public float BgmVolume { get; set; } = 0.8f;

    /// <summary>
    /// 获取或设置音效音量，控制SFX的播放音量
    /// </summary>
    public float SfxVolume { get; set; } = 0.8f;
}
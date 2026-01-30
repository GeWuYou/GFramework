namespace GFramework.Game.Abstractions.setting.data;

/// <summary>
///     音频设置类，用于管理游戏中的音频配置
/// </summary>
public class AudioSettings : ISettingsData
{
    /// <summary>
    ///     获取或设置主音量，控制所有音频的总体音量
    /// </summary>
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>
    ///     获取或设置背景音乐音量，控制BGM的播放音量
    /// </summary>
    public float BgmVolume { get; set; } = 0.8f;

    /// <summary>
    ///     获取或设置音效音量，控制SFX的播放音量
    /// </summary>
    public float SfxVolume { get; set; } = 0.8f;

    /// <summary>
    ///     重置音频设置为默认值
    /// </summary>
    public void Reset()
    {
        // 重置所有音量设置为默认值
        MasterVolume = 1.0f;
        BgmVolume = 0.8f;
        SfxVolume = 0.8f;
    }

    /// <summary>
    ///     获取或设置设置数据的版本号
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    ///     获取设置数据最后修改的时间
    /// </summary>
    public DateTime LastModified { get; } = DateTime.Now;
}

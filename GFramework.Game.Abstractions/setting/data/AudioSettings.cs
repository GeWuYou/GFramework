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
    public int Version { get; private set; } = 1;

    /// <summary>
    ///     获取设置数据最后修改的时间
    /// </summary>
    public DateTime LastModified { get; } = DateTime.Now;

    /// <summary>
    ///     从指定的数据源加载音频设置
    /// </summary>
    /// <param name="source">包含设置数据的源对象</param>
    public void LoadFrom(ISettingsData source)
    {
        // 检查数据源是否为音频设置类型
        if (source is not AudioSettings audioSettings)
        {
            return;
        }

        // 将源数据中的各个音量设置复制到当前对象
        MasterVolume = audioSettings.MasterVolume;
        BgmVolume = audioSettings.BgmVolume;
        SfxVolume = audioSettings.SfxVolume;
        Version = audioSettings.Version;
    }
}
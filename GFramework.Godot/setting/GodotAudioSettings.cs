using GFramework.Game.Abstractions.setting;
using Godot;

namespace GFramework.Godot.setting;

/// <summary>
///     Godot音频设置实现类，用于应用音频配置到Godot音频系统
/// </summary>
/// <param name="settings">音频设置对象，包含主音量、背景音乐音量和音效音量</param>
/// <param name="audioBusMapSettings">音频总线映射对象，定义了不同音频类型的总线名称</param>
public class GodotAudioSettings(AudioSettings settings, AudioBusMapSettings audioBusMapSettings)
    : IPersistentApplyAbleSettings
{
    /// <summary>
    ///     应用音频设置到Godot音频系统
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public Task Apply()
    {
        SetBus(audioBusMapSettings.Master, settings.MasterVolume);
        SetBus(audioBusMapSettings.Bgm, settings.BgmVolume);
        SetBus(audioBusMapSettings.Sfx, settings.SfxVolume);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        audioBusMapSettings.Reset();
    }

    /// <summary>
    ///     设置指定音频总线的音量
    /// </summary>
    /// <param name="busName">音频总线名称</param>
    /// <param name="linear">线性音量值（0-1之间）</param>
    private static void SetBus(string busName, float linear)
    {
        // 获取音频总线索引
        var idx = AudioServer.GetBusIndex(busName);
        if (idx < 0)
        {
            GD.PushWarning($"Audio bus not found: {busName}");
            return;
        }

        // 将线性音量转换为分贝并设置到音频总线
        AudioServer.SetBusVolumeDb(
            idx,
            Mathf.LinearToDb(Mathf.Clamp(linear, 0.0001f, 1f))
        );
    }
}
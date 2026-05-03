// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting.Data;
using Godot;

namespace GFramework.Godot.Setting;

/// <summary>
///     Godot音频设置实现类，用于应用音频配置到Godot音频系统
/// </summary>
/// <param name="model">设置模型对象，提供音频设置数据访问</param>
/// <param name="audioBusMap">音频总线映射对象，定义了不同音频类型的总线名称</param>
public class GodotAudioSettings(ISettingsModel model, AudioBusMap audioBusMap)
    : IResetApplyAbleSettings
{
    /// <summary>
    ///     应用音频设置到Godot音频系统
    /// </summary>
    /// <returns>已完成的任务；该实现只执行同步音频总线更新。</returns>
    public Task ApplyAsync()
    {
        var settings = model.GetData<AudioSettings>();
        SetBus(audioBusMap.Master, settings.MasterVolume);
        SetBus(audioBusMap.Bgm, settings.BgmVolume);
        SetBus(audioBusMap.Sfx, settings.SfxVolume);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     重置音频设置为默认值
    /// </summary>
    public void Reset()
    {
        model.GetData<AudioSettings>().Reset();
    }

    /// <summary>
    ///     获取音频设置的数据对象。
    ///     该属性提供对音频设置数据的只读访问。
    /// </summary>
    public ISettingsData Data { get; } = model.GetData<AudioSettings>();

    /// <summary>
    ///     获取音频设置数据的类型。
    ///     该属性返回音频设置数据的具体类型信息。
    /// </summary>
    public Type DataType { get; } = typeof(AudioSettings);

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

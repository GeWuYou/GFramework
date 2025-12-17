using GFramework.Core.system;
using Godot;

namespace GFramework.Core.Godot.system;

/// <summary>
/// 音频管理器系统接口，用于统一管理背景音乐和音效的播放
/// </summary>
public interface IAudioManagerSystem : ISystem
{
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="loop">是否循环播放</param>
    void PlayMusic(string audioPath, float volume = 1.0f, bool loop = true);
    
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    void PlaySound(string audioPath, float volume = 1.0f, float pitch = 1.0f);
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    void StopMusic();
    
    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    void PauseMusic();
    
    /// <summary>
    /// 恢复背景音乐播放
    /// </summary>
    void ResumeMusic();
    
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    void SetMusicVolume(float volume);
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    void SetSoundVolume(float volume);
    
    /// <summary>
    /// 设置主音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    void SetMasterVolume(float volume);
    
    /// <summary>
    /// 检查背景音乐是否正在播放
    /// </summary>
    /// <returns>正在播放返回true，否则返回false</returns>
    bool IsMusicPlaying();
    
    /// <summary>
    /// 淡入背景音乐
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="duration">淡入持续时间（秒）</param>
    /// <param name="volume">目标音量</param>
    void FadeInMusic(string audioPath, float duration, float volume = 1.0f);
    
    /// <summary>
    /// 淡出背景音乐
    /// </summary>
    /// <param name="duration">淡出持续时间（秒）</param>
    void FadeOutMusic(float duration);
    
    /// <summary>
    /// 播放3D音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="position">3D空间中的位置</param>
    /// <param name="volume">音量大小，范围0-1</param>
    void PlaySound3D(string audioPath, Vector3 position, float volume = 1.0f);
    
    /// <summary>
    /// 设置低通滤波器强度
    /// </summary>
    /// <param name="amount">滤波器强度，范围0-1</param>
    void SetLowPassFilter(float amount);
    
    /// <summary>
    /// 设置音频混响效果
    /// </summary>
    /// <param name="roomSize">房间大小</param>
    /// <param name="damping">阻尼</param>
    /// <param name="wetLevel">湿声级别</param>
    void SetReverb(float roomSize, float damping, float wetLevel);
}
using GFramework.Core.system;
using GFramework.Game.assets;
using GFramework.Godot.assets;
using Godot;

namespace GFramework.Godot.system;

/// <summary>
///     音频管理器抽象基类，提供音频播放的基础实现
/// </summary>
public abstract class AbstractAudioManagerSystem : AbstractSystem, IAudioManagerSystem
{
    /// <summary>
    ///     最大同时播放的音效数量
    /// </summary>
    protected const int MaxSoundPlayers = 10;

    /// <summary>
    ///     最大同时播放的3D音效数量
    /// </summary>
    protected const int MaxSound3DPlayers = 5;

    /// <summary>
    ///     可用3D音效播放器队列
    /// </summary>
    protected readonly Queue<AudioStreamPlayer3D> AvailableSound3DPlayers = new();

    /// <summary>
    ///     可用音效播放器队列
    /// </summary>
    protected readonly Queue<AudioStreamPlayer> AvailableSoundPlayers = new();

    /// <summary>
    ///     3D音效播放器列表
    /// </summary>
    protected readonly List<AudioStreamPlayer3D> Sound3DPlayers = [];

    /// <summary>
    ///     音效播放器列表
    /// </summary>
    protected readonly List<AudioStreamPlayer> SoundPlayers = [];

    /// <summary>
    ///     环境音量
    /// </summary>
    protected float AmbientVolume = 1.0f;

    /// <summary>
    ///     资源目录系统依赖
    /// </summary>
    protected IAssetCatalogSystem? AssetCatalogSystem;

    /// <summary>
    ///     主音量
    /// </summary>
    protected float MasterVolume = 1.0f;

    /// <summary>
    ///     音乐淡入淡出动画
    /// </summary>
    protected Tween? MusicFadeTween;

    /// <summary>
    ///     背景音乐播放器
    /// </summary>
    protected AudioStreamPlayer? MusicPlayer;

    /// <summary>
    ///     背景音乐音量
    /// </summary>
    protected float MusicVolume = 1.0f;

    /// <summary>
    ///     资源工厂系统依赖
    /// </summary>
    protected IResourceFactorySystem? ResourceFactorySystem;

    /// <summary>
    ///     音频资源加载系统依赖
    /// </summary>
    protected IResourceLoadSystem? ResourceLoadSystem;

    /// <summary>
    ///     特效音量
    /// </summary>
    protected float SfxVolume = 1.0f;

    /// <summary>
    ///     音效音量
    /// </summary>
    protected float SoundVolume = 1.0f;

    /// <summary>
    ///     语音音量
    /// </summary>
    protected float VoiceVolume = 1.0f;

    /// <summary>
    ///     所有者节点的抽象属性
    /// </summary>
    protected abstract Node Owner { get; }

    /// <summary>
    ///     播放背景音乐
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="loop">是否循环播放</param>
    public virtual void PlayMusic(string audioPath, float volume = 1.0f, bool loop = true)
    {
        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null || MusicPlayer == null) return;

        // 停止当前正在进行的淡入淡出效果
        MusicFadeTween?.Kill();

        MusicPlayer.Stream = audioStream;
        MusicPlayer.VolumeDb = LinearToDb(volume * MusicVolume * MasterVolume);
        MusicPlayer.Play();
    }

    /// <summary>
    ///     播放音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    public virtual void PlaySound(string audioPath, float volume = 1.0f, float pitch = 1.0f)
    {
        if (AvailableSoundPlayers.Count == 0) return;

        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null) return;

        var player = AvailableSoundPlayers.Dequeue();
        player.Stream = audioStream;
        player.VolumeDb = LinearToDb(volume * SoundVolume * MasterVolume);
        player.Play();
    }

    /// <summary>
    ///     播放特效音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    public virtual void PlaySfx(string audioPath, float volume = 1.0f, float pitch = 1.0f)
    {
        if (AvailableSoundPlayers.Count == 0) return;

        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null) return;

        var player = AvailableSoundPlayers.Dequeue();
        player.Stream = audioStream;
        player.VolumeDb = LinearToDb(volume * SfxVolume * MasterVolume);
        player.Play();
    }

    /// <summary>
    ///     播放语音
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    public virtual void PlayVoice(string audioPath, float volume = 1.0f, float pitch = 1.0f)
    {
        if (AvailableSoundPlayers.Count == 0) return;

        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null) return;

        var player = AvailableSoundPlayers.Dequeue();
        player.Stream = audioStream;
        player.VolumeDb = LinearToDb(volume * VoiceVolume * MasterVolume);
        player.Play();
    }

    /// <summary>
    ///     播放环境音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    public virtual void PlayAmbient(string audioPath, float volume = 1.0f, float pitch = 1.0f)
    {
        if (AvailableSoundPlayers.Count == 0) return;

        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null) return;

        var player = AvailableSoundPlayers.Dequeue();
        player.Stream = audioStream;
        player.VolumeDb = LinearToDb(volume * AmbientVolume * MasterVolume);
        player.Play();
    }

    /// <summary>
    ///     播放3D音效
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="position">3D空间中的位置</param>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void PlaySound3D(string audioPath, Vector3 position, float volume = 1.0f)
    {
        if (AvailableSound3DPlayers.Count == 0) return;

        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null) return;

        var player = AvailableSound3DPlayers.Dequeue();
        player.Stream = audioStream;
        player.VolumeDb = LinearToDb(volume * SoundVolume * MasterVolume);
        player.Position = position;
        player.Play();
    }

    /// <summary>
    ///     停止背景音乐
    /// </summary>
    public virtual void StopMusic()
    {
        MusicFadeTween?.Kill();
        MusicPlayer?.Stop();
    }

    /// <summary>
    ///     暂停背景音乐
    /// </summary>
    public virtual void PauseMusic()
    {
        MusicFadeTween?.Kill();
        // todo 需要记录音乐播放位置，以便恢复播放时从正确位置开始
    }

    /// <summary>
    ///     恢复背景音乐播放
    /// </summary>
    public virtual void ResumeMusic()
    {
        MusicPlayer?.Play();
    }

    /// <summary>
    ///     设置背景音乐音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetMusicVolume(float volume)
    {
        MusicVolume = volume;
        if (MusicPlayer != null) MusicPlayer.VolumeDb = LinearToDb(MusicVolume * MasterVolume);
    }

    /// <summary>
    ///     获取背景音乐音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetMusicVolume()
    {
        return MusicVolume;
    }

    /// <summary>
    ///     设置音效音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetSoundVolume(float volume)
    {
        SoundVolume = volume;
    }

    /// <summary>
    ///     获取音效音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetSoundVolume()
    {
        return SoundVolume;
    }

    /// <summary>
    ///     设置主音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetMasterVolume(float volume)
    {
        MasterVolume = volume;

        // 更新音乐音量
        if (MusicPlayer != null) MusicPlayer.VolumeDb = LinearToDb(MusicVolume * MasterVolume);
    }

    /// <summary>
    ///     获取主音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetMasterVolume()
    {
        return MasterVolume;
    }

    /// <summary>
    ///     设置SFX音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetSfxVolume(float volume)
    {
        SfxVolume = volume;
    }

    /// <summary>
    ///     获取SFX音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetSfxVolume()
    {
        return SfxVolume;
    }

    /// <summary>
    ///     设置语音音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetVoiceVolume(float volume)
    {
        VoiceVolume = volume;
    }

    /// <summary>
    ///     获取语音音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetVoiceVolume()
    {
        return VoiceVolume;
    }

    /// <summary>
    ///     设置环境音量
    /// </summary>
    /// <param name="volume">音量大小，范围0-1</param>
    public virtual void SetAmbientVolume(float volume)
    {
        AmbientVolume = volume;
    }

    /// <summary>
    ///     获取环境音量
    /// </summary>
    /// <returns>音量大小，范围0-1</returns>
    public virtual float GetAmbientVolume()
    {
        return AmbientVolume;
    }

    /// <summary>
    ///     检查背景音乐是否正在播放
    /// </summary>
    /// <returns>正在播放返回true，否则返回false</returns>
    public virtual bool IsMusicPlaying()
    {
        return MusicPlayer?.Playing ?? false;
    }

    /// <summary>
    ///     淡入背景音乐
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="duration">淡入持续时间（秒）</param>
    /// <param name="volume">目标音量</param>
    public virtual void FadeInMusic(string audioPath, float duration, float volume = 1.0f)
    {
        var audioStream = ResourceLoadSystem?.LoadResource<AudioStream>(audioPath);
        if (audioStream == null || MusicPlayer == null) return;

        // 停止当前正在进行的淡入淡出效果
        MusicFadeTween?.Kill();

        MusicPlayer.Stream = audioStream;
        MusicPlayer.VolumeDb = LinearToDb(0.0f); // 初始音量为0
        MusicPlayer.Play();

        // 创建淡入动画
        MusicFadeTween = Owner.CreateTween();
        MusicFadeTween.TweenProperty(MusicPlayer, "volume_db", LinearToDb(volume * MusicVolume * MasterVolume),
            duration);
    }

    /// <summary>
    ///     淡出背景音乐
    /// </summary>
    /// <param name="duration">淡出持续时间（秒）</param>
    public virtual void FadeOutMusic(float duration)
    {
        if (MusicPlayer == null) return;

        // 停止当前正在进行的淡入淡出效果
        MusicFadeTween?.Kill();

        // 创建淡出动画
        MusicFadeTween = Owner.CreateTween();
        MusicFadeTween.TweenProperty(MusicPlayer, "volume_db", LinearToDb(0.0f), duration);
        MusicFadeTween.TweenCallback(Callable.From(() => MusicPlayer.Stop()));
    }

    /// <summary>
    ///     设置低通滤波器强度
    /// </summary>
    /// <param name="amount">滤波器强度，范围0-1</param>
    public virtual void SetLowPassFilter(float amount)
    {
        // TODO: 实现低通滤波器效果
        // 可以通过AudioEffectLowPassFilter实现
    }

    /// <summary>
    ///     设置音频混响效果
    /// </summary>
    /// <param name="roomSize">房间大小</param>
    /// <param name="damping">阻尼</param>
    /// <param name="wetLevel">湿声级别</param>
    public virtual void SetReverb(float roomSize, float damping, float wetLevel)
    {
        // TODO: 实现音频混响效果
        // 可以通过AudioEffectReverb实现
    }

    /// <summary>
    ///     系统初始化方法
    /// </summary>
    protected override void OnInit()
    {
        // 获取依赖的系统
        ResourceLoadSystem = Context.GetSystem<IResourceLoadSystem>();
        AssetCatalogSystem = Context.GetSystem<IAssetCatalogSystem>();
        ResourceFactorySystem = Context.GetSystem<IResourceFactorySystem>();

        // 初始化背景音乐播放器
        MusicPlayer = new AudioStreamPlayer();
        Owner.AddChild(MusicPlayer);

        // 预创建音效播放器池
        for (var i = 0; i < MaxSoundPlayers; i++)
        {
            var soundPlayer = new AudioStreamPlayer();
            Owner.AddChild(soundPlayer);
            soundPlayer.Finished += () => OnSoundFinished(soundPlayer);
            SoundPlayers.Add(soundPlayer);
            AvailableSoundPlayers.Enqueue(soundPlayer);
        }

        // 预创建3D音效播放器池
        for (var i = 0; i < MaxSound3DPlayers; i++)
        {
            var sound3DPlayer = new AudioStreamPlayer3D();
            Owner.AddChild(sound3DPlayer);
            sound3DPlayer.Finished += () => OnSound3DFinished(sound3DPlayer);
            Sound3DPlayers.Add(sound3DPlayer);
            AvailableSound3DPlayers.Enqueue(sound3DPlayer);
        }
    }

    /// <summary>
    ///     当音效播放完成时的回调
    /// </summary>
    /// <param name="player">完成播放的音频播放器</param>
    private void OnSoundFinished(AudioStreamPlayer player)
    {
        // 将播放器放回可用队列
        AvailableSoundPlayers.Enqueue(player);
    }

    /// <summary>
    ///     当3D音效播放完成时的回调
    /// </summary>
    /// <param name="player">完成播放的3D音频播放器</param>
    private void OnSound3DFinished(AudioStreamPlayer3D player)
    {
        // 将播放器放回可用队列
        AvailableSound3DPlayers.Enqueue(player);
    }

    /// <summary>
    ///     通过资源ID播放背景音乐
    /// </summary>
    /// <param name="musicId">音乐资源ID</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="loop">是否循环播放</param>
    public virtual void PlayMusic(AssetCatalog.AssetId musicId, float volume = 1.0f, bool loop = true)
    {
        PlayMusic(musicId.Path, volume, loop);
    }

    /// <summary>
    ///     通过资源ID播放音效
    /// </summary>
    /// <param name="soundId">音效资源ID</param>
    /// <param name="volume">音量大小，范围0-1</param>
    /// <param name="pitch">音调调整</param>
    public virtual void PlaySound(AssetCatalog.AssetId soundId, float volume = 1.0f, float pitch = 1.0f)
    {
        PlaySound(soundId.Path, volume, pitch);
    }

    /// <summary>
    ///     将线性音量值转换为分贝值
    /// </summary>
    /// <param name="linear">线性音量值（0-1）</param>
    /// <returns>分贝值</returns>
    protected static float LinearToDb(float linear)
    {
        return linear > 0 ? 20 * Mathf.Log(linear) : -100;
    }

    /// <summary>
    ///     将分贝值转换为线性音量值
    /// </summary>
    /// <param name="db">分贝值</param>
    /// <returns>线性音量值（0-1）</returns>
    protected static float DbToLinear(float db)
    {
        return db > -100 ? Mathf.Exp(db / 20) : 0;
    }

    /// <summary>
    ///     系统销毁时清理资源
    /// </summary>
    protected override void OnDestroy()
    {
        // 停止并清理淡入淡出动画
        MusicFadeTween?.Kill();

        // 清理音乐播放器
        MusicPlayer?.QueueFree();

        // 清理音效播放器池
        foreach (var player in SoundPlayers) player.QueueFree();

        // 清理3D音效播放器池
        foreach (var player in Sound3DPlayers) player.QueueFree();

        SoundPlayers.Clear();
        AvailableSoundPlayers.Clear();
        Sound3DPlayers.Clear();
        AvailableSound3DPlayers.Clear();
    }
}
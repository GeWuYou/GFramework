namespace GFramework.Game.Abstractions.setting.data;

/// <summary>
///     图形设置类，用于管理游戏的图形相关配置
/// </summary>
public class GraphicsSettings : ISettingsData
{
    /// <summary>
    ///     获取或设置是否启用全屏模式
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>
    ///     获取或设置屏幕分辨率宽度
    /// </summary>
    public int ResolutionWidth { get; set; } = 1920;

    /// <summary>
    ///     获取或设置屏幕分辨率高度
    /// </summary>
    public int ResolutionHeight { get; set; } = 1080;

    /// <summary>
    ///     重置图形设置为默认值
    /// </summary>
    public void Reset()
    {
        Fullscreen = false;
        ResolutionWidth = 1920;
        ResolutionHeight = 1080;
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
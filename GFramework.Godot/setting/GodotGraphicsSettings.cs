using GFramework.Game.Abstractions.setting;
using Godot;

namespace GFramework.Godot.setting;

/// <summary>
/// Godot图形设置应用器
/// </summary>
/// <param name="settings">图形设置配置对象</param>
public class GodotGraphicsSettings(GraphicsSettings settings) : IApplyAbleSettings
{
    /// <summary>
    /// 应用图形设置到Godot引擎
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task Apply()
    {
        // 创建分辨率向量
        var size = new Vector2I(settings.ResolutionWidth, settings.ResolutionHeight);

        // 设置窗口边框状态
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, settings.Fullscreen);

        // 设置窗口模式（全屏或窗口化）
        DisplayServer.WindowSetMode(
            settings.Fullscreen
                ? DisplayServer.WindowMode.ExclusiveFullscreen
                : DisplayServer.WindowMode.Windowed
        );

        // 非全屏模式下设置窗口大小和居中位置
        if (!settings.Fullscreen)
        {
            DisplayServer.WindowSetSize(size);
            var screen = DisplayServer.GetPrimaryScreen();
            var screenSize = DisplayServer.ScreenGetSize(screen);
            var pos = (screenSize - size) / 2;
            DisplayServer.WindowSetPosition(pos);
        }

        await Task.CompletedTask;
    }
}
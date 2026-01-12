using GFramework.Game.Abstractions.setting;
using Godot;

namespace GFramework.Godot.setting;

/// <summary>
/// Godot图形设置类，继承自GraphicsSettings并实现IApplyAbleSettings接口
/// 用于管理游戏的图形显示设置，包括分辨率、全屏模式等
/// </summary>
public class GodotGraphicsSettings : GraphicsSettings, IApplyAbleSettings
{
    /// <summary>
    /// 异步应用当前图形设置到游戏窗口
    /// 该方法会根据设置的分辨率、全屏状态等参数调整Godot窗口的显示属性
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public async Task Apply()
    {
        var size = new Vector2I(ResolutionWidth, ResolutionHeight);

        // 直接调用DisplayServer API，不使用异步或延迟
        // 1. 设置边框标志
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, Fullscreen);

        // 2. 设置窗口模式
        DisplayServer.WindowSetMode(
            Fullscreen ? DisplayServer.WindowMode.ExclusiveFullscreen : DisplayServer.WindowMode.Windowed
        );

        // 3. 窗口化下设置尺寸和位置
        if (!Fullscreen)
        {
            DisplayServer.WindowSetSize(size);
            // 居中窗口
            var screen = DisplayServer.GetPrimaryScreen();
            var screenSize = DisplayServer.ScreenGetSize(screen);
            var pos = (screenSize - size) / 2;
            DisplayServer.WindowSetPosition(pos);
        }

        await Task.CompletedTask;
    }
}
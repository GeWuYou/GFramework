// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using Godot;

namespace GFramework.Godot.Setting;

/// <summary>
///     Godot图形设置应用器
/// </summary>
/// <param name="model">设置模型接口</param>
public class GodotGraphicsSettings(ISettingsModel model) : IResetApplyAbleSettings
{
    /// <summary>
    ///     将当前图形设置同步应用到 Godot 引擎。
    /// </summary>
    /// <returns>已完成的任务；该实现只执行同步引擎调用，不会启动后台异步工作。</returns>
    public Task ApplyAsync()
    {
        var settings = model.GetData<GraphicsSettings>();
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

        return Task.CompletedTask;
    }

    /// <summary>
    ///     重置图形设置
    /// </summary>
    public void Reset()
    {
        model.GetData<GraphicsSettings>().Reset();
    }

    /// <summary>
    ///     获取图形设置的数据对象。
    ///     该属性提供对图形设置数据的只读访问。
    /// </summary>
    public ISettingsData Data { get; } = model.GetData<GraphicsSettings>();

    /// <summary>
    ///     获取图形设置数据的类型。
    ///     该属性返回图形设置数据的具体类型信息。
    /// </summary>
    public Type DataType { get; } = typeof(GraphicsSettings);
}

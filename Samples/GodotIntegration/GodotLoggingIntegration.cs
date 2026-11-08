// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Architectures;
using GFramework.Godot.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotIntegration;

/// <summary>
///     Godot 宿主的日志接入路径：GodotLog 输出引擎（GD.Print/模板/颜色/appsettings 首载与热重载）
///     以标准 <see cref="ILoggerProvider"/> 形式由宿主工厂组合；架构实例通过
///     <see cref="LoggerProperties.LoggerFactory"/> 使用该工厂。
///     不存在进程级日志覆盖：每个架构实例与工厂的组合互不影响。
/// </summary>
public static class GodotLoggingIntegration
{
    /// <summary>配置 GodotLog 并创建携带 Godot 输出引擎的标准日志工厂。</summary>
    /// <remarks>
    ///     工厂由宿主创建并拥有；宿主停止时负责释放工厂（其会级联释放 provider），
    ///     并调用 <see cref="GodotLog.Shutdown" /> 释放配置热重载资源。
    /// </remarks>
    /// <returns>携带 Godot 输出引擎的标准日志工厂。</returns>
    public static ILoggerFactory CreateLoggerFactory()
    {
        GodotLog.Configure(options =>
        {
            // 模板、颜色与 appsettings.json 首载/热重载能力保持原有行为。
            options.Mode = GodotLoggerMode.Debug;
        });
        return LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(GodotLog.Provider));
    }

    /// <summary>创建使用给定日志工厂的架构实例。</summary>
    /// <param name="loggerFactory">宿主拥有的标准日志工厂。</param>
    /// <returns>尚未启动的架构实例。</returns>
    public static OrderArchitecture CreateArchitecture(ILoggerFactory loggerFactory) =>
        new(new ArchitectureConfiguration
        {
            LoggerProperties = new LoggerProperties
            {
                LoggerFactory = loggerFactory,
            },
        });
}

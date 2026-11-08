// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace GFramework.Core.Abstractions.Properties;

/// <summary>
///     日志配置选项，决定当前架构实例使用哪个标准日志工厂。
/// </summary>
/// <remarks>
///     工厂属于当前架构实例：为空时架构创建独立的默认工厂（控制台输出），
///     由架构负责释放；外部传入的工厂所有权仍归调用方，架构不会释放它。
///     不存在进程级日志覆盖，两个架构实例的日志输出互不影响。
/// </remarks>
public sealed class LoggerProperties
{
    /// <summary>
    ///     获取或设置当前架构实例使用的标准 <see cref="ILoggerFactory"/>。
    ///     为 null 时由架构创建独立的默认工厂。
    /// </summary>
    public ILoggerFactory? LoggerFactory
    {
        get; set;
    }
}

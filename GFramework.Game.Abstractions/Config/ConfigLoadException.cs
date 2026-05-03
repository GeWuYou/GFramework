// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     表示配置加载流程中的结构化失败。
///     该异常保留原有异常链，同时通过 <see cref="Diagnostic" /> 暴露稳定字段，
///     便于上层在不解析消息文本的情况下识别失败表、文件和字段位置。
/// </summary>
public sealed class ConfigLoadException : InvalidOperationException
{
    /// <summary>
    ///     初始化一个配置加载异常。
    /// </summary>
    /// <param name="diagnostic">结构化诊断信息。</param>
    /// <param name="message">面向人类阅读的错误消息。</param>
    /// <param name="innerException">底层异常；不存在时为空。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="diagnostic" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="message" /> 为空时抛出。</exception>
    public ConfigLoadException(
        ConfigLoadDiagnostic diagnostic,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException(nameof(diagnostic));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Exception message cannot be null or whitespace.", nameof(message));
        }

        Diagnostic = diagnostic;
    }

    /// <summary>
    ///     获取结构化诊断信息。
    /// </summary>
    public ConfigLoadDiagnostic Diagnostic { get; }
}
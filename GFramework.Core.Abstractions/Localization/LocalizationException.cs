// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Localization;

/// <summary>
/// 本地化异常基类
/// </summary>
public class LocalizationException : Exception
{
    /// <summary>
    /// 初始化本地化异常
    /// </summary>
    public LocalizationException()
    {
    }

    /// <summary>
    /// 初始化本地化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public LocalizationException(string message) : base(message)
    {
    }

    /// <summary>
    /// 初始化本地化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public LocalizationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
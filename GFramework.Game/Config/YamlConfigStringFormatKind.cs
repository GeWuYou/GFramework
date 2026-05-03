// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Abstractions.Enums;

namespace GFramework.Game.Config;

/// <summary>
///     表示当前 Runtime / Generator / Tooling 共享支持的字符串 format 子集。
/// </summary>
[GenerateEnumExtensions]
internal enum YamlConfigStringFormatKind
{
    /// <summary>
    ///     表示 <c>yyyy-MM-dd</c> 形式的日期。
    /// </summary>
    Date,

    /// <summary>
    ///     表示带显式时区偏移的 RFC 3339 日期时间。
    /// </summary>
    DateTime,

    /// <summary>
    ///     表示 day-time duration 形式的持续时间。
    /// </summary>
    Duration,

    /// <summary>
    ///     表示基础电子邮件地址格式。
    /// </summary>
    Email,

    /// <summary>
    ///     表示带显式时区偏移的 RFC 3339 时间。
    /// </summary>
    Time,

    /// <summary>
    ///     表示绝对 URI。
    /// </summary>
    Uri,

    /// <summary>
    ///     表示连字符分隔的 UUID 文本。
    /// </summary>
    Uuid
}

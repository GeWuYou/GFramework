// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     表示一个一对多发布的通知消息。
///     通知不要求返回值，允许被零个或多个处理器消费。
/// </summary>
public interface INotification;

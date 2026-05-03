// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     UI切换处理器执行选项
/// </summary>
public record UiTransitionHandlerOptions(int TimeoutMs = 0, bool ContinueOnError = true);
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Utility.Numeric;

/// <summary>
/// 数值缩写阈值定义。
/// </summary>
/// <param name="Divisor">缩写除数，例如 1000、1000000。</param>
/// <param name="Suffix">缩写后缀，例如 K、M。</param>
public readonly record struct NumericSuffixThreshold(decimal Divisor, string Suffix);
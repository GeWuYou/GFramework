// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.SourceGenerators.Common.Info;

/// <summary>
///     表示泛型信息的数据结构
/// </summary>
/// <param name="Parameters">泛型参数字符串</param>
/// <param name="Constraints">泛型约束列表</param>
public record struct GenericInfo(
    string Parameters,
    IReadOnlyList<string> Constraints
);
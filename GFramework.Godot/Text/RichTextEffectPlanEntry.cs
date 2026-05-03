// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     描述一条纯托管的富文本效果计划项。
///     控制器与测试替身只关心效果键和启用状态，不需要依赖 Godot 资源对象本身。
/// </summary>
/// <param name="Key">效果键。</param>
/// <param name="Enabled">该效果项是否启用。</param>
internal readonly record struct RichTextEffectPlanEntry(string Key, bool Enabled = true);

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Asset;
using Godot;

namespace GFramework.Godot.UI;

/// <summary>
///     Godot UI注册表接口，用于管理PackedScene类型的UI资源注册和管理
///     继承自通用UI注册表接口，专门针对Godot引擎的PackedScene资源类型
/// </summary>
public interface IGodotUiRegistry : IAssetRegistry<PackedScene>;
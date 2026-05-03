// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Asset;
using Godot;

namespace GFramework.Godot.Scene;

/// <summary>
///     Godot场景注册表接口，用于管理PackedScene资源的注册和访问
/// </summary>
public interface IGodotSceneRegistry : IAssetRegistry<PackedScene>;
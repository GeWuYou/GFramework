using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.scene;

/// <summary>
/// Godot场景注册表接口，用于管理PackedScene资源的注册和访问
/// </summary>
public interface IGodotSceneRegistry : IAssetRegistry<PackedScene>;
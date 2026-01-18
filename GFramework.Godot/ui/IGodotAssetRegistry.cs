using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot UI注册表接口，用于管理PackedScene类型的UI资源注册和管理
/// 继承自通用UI注册表接口，专门针对Godot引擎的PackedScene资源类型
/// </summary>
public interface IGodotAssetRegistry : IAssetRegistry<PackedScene>;
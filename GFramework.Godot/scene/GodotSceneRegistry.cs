using GFramework.Core.Abstractions.registries;
using Godot;

namespace GFramework.Godot.scene;

/// <summary>
/// Godot场景注册表类
/// 继承自KeyValueRegistryBase，使用字符串作为键，PackedScene作为值进行存储
/// 实现IUiRegistry接口，提供UI场景的注册和管理功能
/// </summary>
public class GodotSceneRegistry()
    : KeyValueRegistryBase<string, PackedScene>(StringComparer.Ordinal), IGodotSceneRegistry;
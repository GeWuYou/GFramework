using GFramework.Game.Abstractions.registry;

namespace GFramework.Game.Abstractions.scene;

/// <summary>
///     游戏场景注册表接口，用于管理游戏场景的注册和查找
/// </summary>
/// <typeparam name="T">场景类型，表示注册表中存储的具体场景对象类型</typeparam>
public interface IGameSceneRegistry<T> : IRegistry<string, T>;
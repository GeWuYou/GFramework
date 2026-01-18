using GFramework.Core.Abstractions.utility;
using GFramework.Game.Abstractions.registry;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// 资源注册表接口，用于管理指定类型T的资源注册和查找
/// </summary>
/// <typeparam name="T">资源的类型</typeparam>
public interface IAssetRegistry<T> : IUtility, IRegistry<string, T>;
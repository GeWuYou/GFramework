using GFramework.Core.Abstractions.utility;
using GFramework.Game.Abstractions.registry;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI注册表接口，用于根据UI键获取对应的UI实例
/// </summary>
/// <typeparam name="T">UI实例的类型参数，使用协变修饰符out</typeparam>
public interface IUiRegistry<T> : IUtility, IRegistry<string, T>;
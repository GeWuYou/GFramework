using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI注册表接口，用于根据UI键获取对应的UI实例
/// </summary>
/// <typeparam name="T">UI实例的类型参数，使用协变修饰符out</typeparam>
public interface IUiRegistry<out T> : IUtility
{
    /// <summary>
    /// 根据指定的UI键获取对应的UI实例
    /// </summary>
    /// <param name="uiKey">UI的唯一标识键</param>
    /// <returns>与指定键关联的UI实例</returns>
    T Get(string uiKey);
}
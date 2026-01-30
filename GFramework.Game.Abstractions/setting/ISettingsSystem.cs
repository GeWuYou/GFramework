using GFramework.Core.Abstractions.system;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     定义设置系统的接口，提供应用各种设置的方法
/// </summary>
public interface ISettingsSystem : ISystem
{
    /// <summary>
    ///     应用所有可应用的设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    Task ApplyAll();

    /// <summary>
    ///     应用指定类型的设置（泛型版本）
    /// </summary>
    /// <typeparam name="T">设置类型，必须是class且实现IResetApplyAbleSettings接口</typeparam>
    /// <returns>表示异步操作的任务</returns>
    Task Apply<T>() where T : class, IResetApplyAbleSettings;

    /// <summary>
    ///     保存所有设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    Task SaveAll();

    /// <summary>
    ///     重置指定类型的设置
    /// </summary>
    /// <typeparam name="T">设置类型，必须继承自class并实现IPersistentApplyAbleSettings接口</typeparam>
    /// <returns>表示异步操作的任务</returns>
    Task Reset<T>() where T : class, ISettingsData, IResetApplyAbleSettings, new();

    /// <summary>
    ///     重置所有设置
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    Task ResetAll();
}
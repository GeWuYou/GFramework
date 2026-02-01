using GFramework.Core.Abstractions.model;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     设置模型接口：
///     - 管理 Settings Data 的生命周期
///     - 管理并编排 Settings Applicator
///     - 管理 Settings Migration
/// </summary>
public interface ISettingsModel : IModel
{
    // =========================
    // Data
    // =========================

    /// <summary>
    ///     获取指定类型的设置数据（唯一实例）
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须继承自ISettingsData并具有无参构造函数</typeparam>
    /// <returns>指定类型的设置数据实例</returns>
    T GetData<T>() where T : class, ISettingsData, new();

    /// <summary>
    ///     获取所有已创建的设置数据
    /// </summary>
    /// <returns>所有已创建的设置数据集合</returns>
    IEnumerable<ISettingsData> AllData();


    // =========================
    // Applicator
    // =========================

    /// <summary>
    ///     注册设置应用器
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现IResetApplyAbleSettings接口且具有无参构造函数</typeparam>
    /// <param name="applicator">要注册的设置应用器</param>
    /// <returns>当前设置模型实例，支持链式调用</returns>
    ISettingsModel RegisterApplicator<T>(T applicator) where T : class, IResetApplyAbleSettings;


    /// <summary>
    ///     获取指定类型的设置应用器
    /// </summary>
    /// <typeparam name="T">要获取的设置应用器类型，必须继承自IResetApplyAbleSettings</typeparam>
    /// <returns>设置应用器实例，如果不存在则返回null</returns>
    T? GetApplicator<T>() where T : class, IResetApplyAbleSettings;


    /// <summary>
    ///     获取所有设置应用器
    /// </summary>
    /// <returns>所有设置应用器的集合</returns>
    IEnumerable<IResetApplyAbleSettings> AllApplicators();


    // =========================
    // Migration
    // =========================

    /// <summary>
    ///     注册设置迁移器
    /// </summary>
    /// <param name="migration">要注册的设置迁移器</param>
    /// <returns>当前设置模型实例，支持链式调用</returns>
    ISettingsModel RegisterMigration(ISettingsMigration migration);


    // =========================
    // Lifecycle
    // =========================

    /// <summary>
    ///     初始化所有设置数据（加载 + 迁移）
    /// </summary>
    /// <returns>异步操作任务</returns>
    Task InitializeAsync();

    /// <summary>
    ///     保存所有设置数据
    /// </summary>
    /// <returns>异步操作任务</returns>
    Task SaveAllAsync();

    /// <summary>
    ///     应用所有设置
    /// </summary>
    /// <returns>异步操作任务</returns>
    Task ApplyAllAsync();

    /// <summary>
    ///     重置指定类型的设置
    /// </summary>
    /// <typeparam name="T">要重置的设置类型，必须实现IResettable接口并具有无参构造函数</typeparam>
    void Reset<T>() where T : class, ISettingsData, new();


    /// <summary>
    ///     重置所有设置数据与应用器
    /// </summary>
    void ResetAll();
}
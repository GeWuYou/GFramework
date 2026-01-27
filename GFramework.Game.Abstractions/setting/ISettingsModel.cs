using GFramework.Core.Abstractions.model;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     定义设置模型的接口，提供获取特定类型设置节的功能
/// </summary>
public interface ISettingsModel : IModel
{
    /// <summary>
    ///     获取或创建数据设置（自动创建）
    /// </summary>
    /// <typeparam name="T">设置数据的类型，必须继承自class、ISettingsData且具有无参构造函数</typeparam>
    /// <returns>指定类型的设置数据实例</returns>
    T GetData<T>() where T : class, ISettingsData, new();

    /// <summary>
    ///     尝试获取指定类型的设置节实例
    /// </summary>
    /// <param name="type">要获取的设置节类型</param>
    /// <param name="section">输出参数，如果成功则包含找到的设置节实例，否则为null</param>
    /// <returns>如果找到指定类型的设置节则返回true，否则返回false</returns>
    bool TryGet(Type type, out ISettingsSection section);

    /// <summary>
    ///     获取已注册的可应用设置
    /// </summary>
    /// <typeparam name="T">可应用设置的类型，必须继承自class和IApplyAbleSettings</typeparam>
    /// <returns>指定类型的可应用设置实例，如果不存在则返回null</returns>
    T? GetApplicator<T>() where T : class, IApplyAbleSettings;

    /// <summary>
    ///     获取所有设置节的集合
    /// </summary>
    /// <returns>包含所有设置节的可枚举集合</returns>
    IEnumerable<ISettingsSection> All();

    /// <summary>
    ///     注册可应用设置（必须手动注册）
    /// </summary>
    /// <typeparam name="T">可应用设置的类型，必须继承自class和IApplyAbleSettings</typeparam>
    /// <param name="applicator">要注册的可应用设置实例</param>
    /// <returns>返回当前设置模型实例，支持链式调用</returns>
    ISettingsModel RegisterApplicator<T>(T applicator) where T : class, IApplyAbleSettings;
}
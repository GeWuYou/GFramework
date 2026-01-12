using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.model;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 定义设置模型的接口，提供获取特定类型设置节的功能
/// </summary>
public interface ISettingsModel : IModel
{
    /// <summary>
    /// 获取指定类型的设置节实例
    /// </summary>
    /// <typeparam name="T">设置节的类型，必须是class、实现ISettingsSection接口且具有无参构造函数</typeparam>
    /// <returns>指定类型的设置节实例</returns>
    T Get<T>() where T : class, ISettingsSection, new();

    /// <summary>
    /// 尝试获取指定类型的设置节实例
    /// </summary>
    /// <param name="type">要获取的设置节类型</param>
    /// <param name="section">输出参数，如果成功则包含找到的设置节实例，否则为null</param>
    /// <returns>如果找到指定类型的设置节则返回true，否则返回false</returns>
    bool TryGet(Type type, out ISettingsSection section);

    /// <summary>
    /// 获取所有设置节的集合
    /// </summary>
    /// <returns>包含所有设置节的可枚举集合</returns>
    IEnumerable<ISettingsSection> All();

    /// <summary>
    /// 注册一个可应用的设置对象
    /// </summary>
    /// <param name="applyAble">要注册的可应用设置对象</param>
    void Register(IApplyAbleSettings applyAble);
}
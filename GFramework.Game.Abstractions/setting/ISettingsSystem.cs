using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.system;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 定义设置系统的接口，提供应用各种设置的方法
/// </summary>
public interface ISettingsSystem : ISystem
{
    /// <summary>
    /// 应用所有可应用的设置
    /// </summary>
    Task ApplyAll();

    /// <summary>
    /// 应用指定类型的设置
    /// </summary>
    Task Apply(Type settingsType);

    /// <summary>
    /// 应用指定类型的设置（泛型版本）
    /// </summary>
    Task Apply<T>() where T : class, ISettingsSection;

    /// <summary>
    /// 批量应用多个设置类型
    /// </summary>
    Task Apply(IEnumerable<Type> settingsTypes);
}
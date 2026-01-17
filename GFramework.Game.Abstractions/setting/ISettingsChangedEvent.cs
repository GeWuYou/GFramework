using System;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 设置变更事件基类
/// 定义了设置变更事件的基本结构和属性
/// </summary>
public interface ISettingsChangedEvent
{
    /// <summary>
    /// 获取变更的设置类型
    /// </summary>
    Type SettingsType { get; }

    /// <summary>
    /// 获取变更的设置实例
    /// </summary>
    ISettingsSection Settings { get; }

    /// <summary>
    /// 获取变更发生的时间
    /// </summary>
    DateTime ChangedAt { get; }
}
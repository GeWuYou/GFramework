using System.Collections.Generic;

namespace GFramework.Core.Abstractions.events;

/// <summary>
///     提供统一注销功能的接口，用于管理需要注销的对象列表
/// </summary>
public interface IUnRegisterList
{
    /// <summary>
    ///     获取需要注销的对象列表
    /// </summary>
    IList<IUnRegister> UnregisterList { get; }
}
using System;
using GFramework.Core.Abstractions.system;

namespace GFramework.Game.Abstractions.assets;

/// <summary>
///     资源工厂系统接口，用于获取指定类型的资源创建函数
/// </summary>
public interface IResourceFactorySystem : ISystem
{
    /// <summary>
    ///     根据指定键名获取指定类型T的资源创建函数
    /// </summary>
    /// <typeparam name="T">要获取创建函数的资源类型</typeparam>
    /// <param name="key">用于标识资源的键名</param>
    /// <returns>返回一个创建T类型实例的函数委托</returns>
    Func<T> GetFactory<T>(string key);

    /// <summary>
    ///     根据资产目录映射获取指定类型T的资源创建函数
    /// </summary>
    /// <typeparam name="T">要获取创建函数的资源类型</typeparam>
    /// <param name="mapping">资产目录映射信息</param>
    /// <returns>返回一个创建T类型实例的函数委托</returns>
    Func<T> GetFactory<T>(AssetCatalog.AssetCatalogMapping mapping);
}
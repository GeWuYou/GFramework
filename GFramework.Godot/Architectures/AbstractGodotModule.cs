// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Architectures;
using Godot;

namespace GFramework.Godot.Architectures;

/// <summary>
///     抽象的Godot模块基类，用于定义Godot框架中的模块行为
/// </summary>
public abstract class AbstractGodotModule : IGodotModule
{
    /// <summary>
    ///     获取模块关联的Godot节点
    /// </summary>
    public abstract Node Node
    {
        get;
    }

    /// <summary>
    ///     安装模块到指定架构中
    /// </summary>
    /// <param name="services">The current service descriptions.</param>
    public abstract void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services);

    /// <summary>
    ///     当模块从架构中分离时调用此方法
    /// </summary>
    public virtual void OnDetach()
    {
    }

    /// <summary>
    ///     当模块被附加到架构时调用此方法
    /// </summary>
    /// <param name="architecture">被附加到的架构实例</param>
    public virtual void OnAttach(Architecture architecture)
    {
    }

}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Logging;
using GFramework.Core.Rule;

namespace GFramework.Core.Utility;

/// <summary>
///     抽象上下文工具类，提供上下文相关的通用功能实现
///     继承自ContextAwareBase并实现IContextUtility接口
/// </summary>
public abstract class AbstractContextUtility : ContextAwareBase, IContextUtility
{
    /// <summary>
    ///  日志记录器
    /// </summary>
    protected ILogger Logger = null!;

    /// <summary>
    ///     初始化上下文工具类
    /// </summary>
    public void Initialize()
    {
        var name = GetType().Name;
        // 获取上下文中的日志记录器
        Logger = CreateInstanceLogger(name);
        if (Logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            Logger.LogDebug("Initializing Context Utility: {UtilityName}", name);
        }

        // 执行子类实现的初始化逻辑
        OnInit();

        // 记录初始化完成信息
        if (Logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
        {
            Logger.LogInformation("Context Utility initialized: {UtilityName}", name);
        }
    }

    /// <summary>
    ///     销毁上下文工具类实例
    /// </summary>
    public void Destroy()
    {
        Logger ??= CreateInstanceLogger(GetType().Name);
        var name = GetType().Name;
        if (Logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            Logger.LogDebug("Destroying Context Utility: {UtilityName}", name);
        }
        OnDestroy();
        if (Logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
        {
            Logger.LogInformation("Context Utility destroyed: {UtilityName}", name);
        }
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    ///     虚拟销毁方法，可由子类重写以实现自定义销毁逻辑
    /// </summary>
    protected virtual void OnDestroy()
    {
    }
    /// <summary>绑定架构时通过其实例工厂解析标准日志工厂；未绑定时使用空输出。</summary>
    /// <param name="name">日志类别名。</param>
    /// <returns>当前组件日志。</returns>
    private ILogger CreateInstanceLogger(string name)
    {
        var factory = Context == null
            ? NullLoggerFactory.Instance
            : Context.GetService<ILoggerFactory>();
        return factory.CreateLogger(name);
    }

}

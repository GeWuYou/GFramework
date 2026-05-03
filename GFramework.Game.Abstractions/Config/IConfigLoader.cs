// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Utility;

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     定义配置加载器契约。
///     具体实现负责从文件系统、资源包或其他配置源加载文本配置，并将解析结果注册到配置注册表。
/// </summary>
public interface IConfigLoader : IUtility
{
    /// <summary>
    ///     执行配置加载并将结果写入注册表。
    ///     实现应在同一次加载过程中保证注册结果的一致性，避免只加载部分配置后就暴露给运行时消费。
    /// </summary>
    /// <param name="registry">用于接收配置表的注册表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步加载流程的任务。</returns>
    /// <exception cref="ConfigLoadException">
    ///     当配置文件、schema、反序列化或跨表引用校验失败时抛出。
    ///     调用方可以通过 <see cref="ConfigLoadException.Diagnostic" /> 读取稳定的结构化字段。
    /// </exception>
    Task LoadAsync(IConfigRegistry registry, CancellationToken cancellationToken = default);
}
// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs;

/// <summary>
///     为 generated request invoker provider 暴露可枚举描述符集合的内部辅助契约。
/// </summary>
/// <remarks>
///     registrar 在激活 generated registry 后，会通过该接口读取当前程序集声明的 request invoker 描述符，
///     并把它们登记到 dispatcher 的进程级弱缓存中。
///     该接口不改变公开分发语义，只服务于 generated invoker 元数据的运行时接线。
/// </remarks>
public interface IEnumeratesCqrsRequestInvokerDescriptors
{
    /// <summary>
    ///     返回当前 provider 可声明的全部 request invoker 描述符条目。
    /// </summary>
    /// <returns>按 provider 定义顺序枚举的描述符条目集合。</returns>
    IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors();
}

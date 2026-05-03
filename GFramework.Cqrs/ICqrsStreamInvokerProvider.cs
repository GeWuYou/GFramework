// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs;

/// <summary>
///     定义由源码生成器或手写注册器提供的 stream invoker 元数据契约。
/// </summary>
/// <remarks>
///     该 seam 允许运行时在首次创建 stream dispatch binding 时，
///     直接复用编译期已知的流式请求/响应类型映射，而不是总是通过反射闭合泛型方法生成调用委托。
///     当当前程序集没有提供匹配项时，dispatcher 仍会回退到既有的反射 binding 创建路径。
///     当前默认 runtime 通过 <see cref="IEnumeratesCqrsStreamInvokerDescriptors" /> 在注册阶段一次性读取并缓存
///     provider 暴露的描述符；<see cref="TryGetDescriptor(Type, Type, out CqrsStreamInvokerDescriptor?)" />
///     主要用于 provider 自检、测试和显式调用场景，而不是 dispatcher 在建流热路径上的二次回调入口。
/// </remarks>
public interface ICqrsStreamInvokerProvider
{
    /// <summary>
    ///     尝试为指定流式请求/响应类型对提供运行时元数据。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">命中时返回的 stream invoker 元数据。</param>
    /// <returns>若当前 provider 可处理该流式请求/响应类型对则返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    /// <remarks>
    ///     若 provider 希望被默认 runtime 自动接线到 dispatcher 的 generated invoker 缓存中，
    ///     还必须同时实现 <see cref="IEnumeratesCqrsStreamInvokerDescriptors" />，以便 registrar 在注册阶段枚举全部描述符。
    /// </remarks>
    bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out CqrsStreamInvokerDescriptor? descriptor);
}

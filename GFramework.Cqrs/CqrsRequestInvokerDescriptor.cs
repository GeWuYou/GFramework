// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs;

/// <summary>
///     描述单个 request/response 类型对在运行时分发时需要复用的元数据。
/// </summary>
/// <param name="handlerType">当前请求处理器在容器中的服务类型。</param>
/// <param name="invokerMethod">
///     执行单个请求处理器的开放静态方法。
///     dispatcher 会在首次创建 request binding 时，把该方法绑定成内部使用的强类型委托。
/// </param>
/// <remarks>
///     dispatcher 会继续自行构造 pipeline behavior 服务类型并负责上下文注入；
///     该描述符只前移请求处理器服务类型与直接调用方法元数据。
/// </remarks>
public sealed class CqrsRequestInvokerDescriptor(
    Type handlerType,
    MethodInfo invokerMethod)
{
    private static readonly string NonStaticInvokerMessage =
        "CQRS request invoker descriptors require an open static invoker method so generated metadata can be bound deterministically.";

    /// <summary>
    ///     获取请求处理器在容器中的服务类型。
    /// </summary>
    public Type HandlerType { get; } = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

    /// <summary>
    ///     获取执行请求处理器的开放静态方法。
    /// </summary>
    public MethodInfo InvokerMethod { get; } = ValidateInvokerMethod(invokerMethod);

    /// <summary>
    ///     在描述符构造阶段拒绝实例方法，避免非法 generated metadata 延迟到首次分发时才暴露。
    /// </summary>
    /// <param name="invokerMethod">待验证的 generated invoker 方法。</param>
    /// <returns>通过校验的静态方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="invokerMethod" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="invokerMethod" /> 不是静态方法时抛出。</exception>
    private static MethodInfo ValidateInvokerMethod(MethodInfo invokerMethod)
    {
        ArgumentNullException.ThrowIfNull(invokerMethod);

        if (!invokerMethod.IsStatic)
            throw new ArgumentException(NonStaticInvokerMessage, nameof(invokerMethod));

        return invokerMethod;
    }
}

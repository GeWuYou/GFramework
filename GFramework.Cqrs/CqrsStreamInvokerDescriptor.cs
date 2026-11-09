// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace GFramework.Cqrs;

/// <summary>Describes a stream handler invocation without requiring generated method lookup.</summary>
/// <remarks>Generated consumers use Create with a typed static delegate. The MethodInfo constructor is reserved
/// for explicitly discovered runtime metadata; it is not used by the generated registration path.</remarks>
public sealed class CqrsStreamInvokerDescriptor
{
    /// <summary>Describes a runtime-discovered static method for the explicit reflection path.</summary>
    /// <param name="handlerType">The canonical handler service type.</param>
    /// <param name="invokerMethod">The runtime-discovered open static method.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">The method is not static.</exception>
    public CqrsStreamInvokerDescriptor(Type handlerType, MethodInfo invokerMethod)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(invokerMethod);
        if (!invokerMethod.IsStatic)
            throw new ArgumentException("CQRS invoker metadata requires an open static method.", nameof(invokerMethod));
        HandlerType = handlerType;
        InvokerMethod = invokerMethod;
    }

    /// <summary>Captures a single stateless delegate; process-wide metadata must never retain scoped services.</summary>
    private CqrsStreamInvokerDescriptor(Type handlerType, Delegate invoker)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(invoker);
        if (invoker.Target is not null || invoker.GetInvocationList().Length != 1)
            throw new ArgumentException("CQRS generated invokers require a single static method group.", nameof(invoker));
        HandlerType = handlerType;
        Invoker = invoker;
        // This is the delegate's existing metadata, not a reflective name lookup or dynamic activation.
        InvokerMethod = invoker.Method;
    }

    /// <summary>The handler service type resolved from the current provider for every invocation.</summary>
    public Type HandlerType
    {
        get;
    }

    /// <summary>The original static method metadata, retained for explicit dynamic consumers and diagnostics.</summary>
    public MethodInfo InvokerMethod
    {
        get;
    }

    /// <summary>The compiled invocation delegate, or null for explicitly reflected metadata.</summary>
    public Delegate? Invoker
    {
        get;
    }

    /// <summary>Creates generated metadata from a typed method group without reflective binding.</summary>
    /// <typeparam name="TResponse">The response element type.</typeparam>
    /// <param name="handlerType">The canonical handler service type.</param>
    /// <param name="invoker">A static method group; it must not capture application services.</param>
    /// <returns>Immutable invocation metadata.</returns>
    public static CqrsStreamInvokerDescriptor Create<TResponse>(Type handlerType,
        Func<object, object, CancellationToken, IAsyncEnumerable<TResponse>> invoker) => new(handlerType, invoker);
}

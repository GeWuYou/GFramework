// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Core.Query;

/// <summary>
///     异步查询抽象基类，提供异步查询的基本框架和执行机制
///     继承自ContextAwareBase并实现IAsyncQuery&lt;TResult&gt;接口
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public abstract class AbstractAsyncQuery<TResult> : ContextAwareBase, IAsyncQuery<TResult>
{
    /// <summary>
    ///     执行异步查询操作
    /// </summary>
    /// <returns>返回查询结果的异步任务</returns>
    public Task<TResult> DoAsync()
    {
        return OnDoAsync();
    }

    /// <summary>
    ///     抽象方法，用于实现具体的异步查询逻辑
    /// </summary>
    /// <returns>返回查询结果的异步任务</returns>
    protected abstract Task<TResult> OnDoAsync();
}

/// <summary>
///     抽象异步查询基类，为需要输入参数的异步查询提供统一执行骨架。
/// </summary>
/// <typeparam name="TInput">查询输入类型，必须实现 <see cref="IQueryInput" /> 接口。</typeparam>
/// <typeparam name="TResult">查询结果类型。</typeparam>
/// <param name="input">查询输入参数。</param>
public abstract class AbstractAsyncQuery<TInput, TResult>(TInput input)
    : ContextAwareBase, IAsyncQuery<TResult>
    where TInput : IQueryInput
{
    /// <summary>
    ///     执行异步查询操作。
    /// </summary>
    /// <returns>返回查询结果的异步任务。</returns>
    public Task<TResult> DoAsync()
    {
        return OnDoAsync(input);
    }

    /// <summary>
    ///     抽象方法，用于实现具体的异步查询逻辑。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>返回查询结果的异步任务。</returns>
    protected abstract Task<TResult> OnDoAsync(TInput input);
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Query;
using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Core.Query;

/// <summary>
///     抽象查询类，提供查询操作的基础实现
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public abstract class AbstractQuery<TResult> : ContextAwareBase, GFramework.Core.Abstractions.Query.IQuery<TResult>

{
    /// <summary>
    ///     执行查询操作
    /// </summary>
    /// <returns>查询结果，类型为TResult</returns>
    public TResult Do()
    {
        // 调用抽象方法执行具体的查询逻辑
        return OnDo();
    }

    /// <summary>
    ///     抽象方法，用于实现具体的查询逻辑
    /// </summary>
    /// <returns>查询结果，类型为TResult</returns>
    protected abstract TResult OnDo();
}

/// <summary>
///     抽象查询类，为需要输入参数的同步查询提供基础实现。
/// </summary>
/// <typeparam name="TInput">查询输入参数的类型，必须实现 <see cref="IQueryInput" /> 接口。</typeparam>
/// <typeparam name="TResult">查询结果的类型。</typeparam>
/// <param name="input">查询输入参数。</param>
public abstract class AbstractQuery<TInput, TResult>(TInput input)
    : ContextAwareBase, GFramework.Core.Abstractions.Query.IQuery<TResult>
    where TInput : IQueryInput
{
    /// <summary>
    ///     执行查询操作。
    /// </summary>
    /// <returns>查询结果，类型为 <typeparamref name="TResult" />。</returns>
    public TResult Do()
    {
        return OnDo(input);
    }

    /// <summary>
    ///     抽象方法，用于实现具体的查询逻辑。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>查询结果。</returns>
    protected abstract TResult OnDo(TInput input);
}

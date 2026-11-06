// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Godot.SourceGenerators.Tests.Core;

/// <summary>
///     生成器测试校验器：在默认校验行为之上兼容 Roslyn 编译器默认生成文件校验和算法的变化。
/// </summary>
/// <remarks>
///     测试框架对期望生成文档硬编码 <see cref="SourceHashAlgorithm.Sha1"/>，
///     而 Roslyn 5.x 起编译器为生成文件默认产出 <see cref="SourceHashAlgorithm.Sha256"/>。
///     本仓库生成器测试只验证生成内容本身，不把校验和算法作为契约，
///     因此对这一处已知的算法差异放行；其余校验全部保持默认严格行为。
/// </remarks>
public sealed class GeneratorTestVerifier : DefaultVerifier
{
    /// <summary>
    ///     测试入口使用的默认实例。
    /// </summary>
    public GeneratorTestVerifier()
    {
    }

    /// <summary>
    ///     供 <see cref="PushContext" /> 携带上下文栈创建新实例的私有构造函数。
    /// </summary>
    /// <param name="context">当前校验上下文栈。</param>
    private GeneratorTestVerifier(ImmutableStack<string> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override void Equal<T>(T expected, T actual, string? message = null)
    {
        if (message is not null
            && message.StartsWith("checksum algorithm of", StringComparison.Ordinal)
            && expected is SourceHashAlgorithm expectedAlgorithm
            && actual is SourceHashAlgorithm actualAlgorithm
            && expectedAlgorithm == SourceHashAlgorithm.Sha1
            && actualAlgorithm == SourceHashAlgorithm.Sha256)
        {
            return;
        }

        base.Equal(expected, actual, message);
    }

    /// <inheritdoc />
    public override IVerifier PushContext(string context)
    {
        // 基类实现拒绝派生类型调用 base.PushContext，因此这里自行携带上下文栈创建新实例。
        return new GeneratorTestVerifier(Context.Push(context));
    }
}

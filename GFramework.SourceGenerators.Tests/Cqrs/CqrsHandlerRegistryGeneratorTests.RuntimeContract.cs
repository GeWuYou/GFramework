// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.SourceGenerators.Tests.Cqrs;

/// <summary>Verifies rejection of runtime packages that do not implement the current generation contract.</summary>
public partial class CqrsHandlerRegistryGeneratorTests
{
    /// <summary>An unsupported runtime is rejected with a deliberate diagnostic instead of a silently degraded registry.</summary>
    [TestCase(false, "missing")]
    [TestCase(true, "missing")]
    [TestCase(false, "private")]
    [TestCase(true, "private")]
    [TestCase(false, "instance")]
    [TestCase(true, "instance")]
    [TestCase(false, "delegate")]
    [TestCase(true, "delegate")]
    [TestCase(false, "constraint")]
    [TestCase(true, "constraint")]
    public void UnsupportedRuntimeIsRejectedWithoutPartialRegistry(bool stream, string contract)
    {
        var source = stream ? StreamInvokerProviderSource : RequestInvokerProviderSource;
        var descriptor = stream ? "CqrsStreamInvokerDescriptor" : "CqrsRequestInvokerDescriptor";
        source = contract switch
        {
            "missing" => source.Replace(" Create<TResponse>(", " LegacyCreate<TResponse>(", StringComparison.Ordinal),
            "private" => source.Replace("public static " + descriptor + " Create", "private static " + descriptor + " Create", StringComparison.Ordinal),
            "instance" => source.Replace("public static " + descriptor + " Create", "public " + descriptor + " Create", StringComparison.Ordinal),
            "delegate" => source.Replace("System.Func<object, object,", "System.Func<string, object,", StringComparison.Ordinal),
            "constraint" => source.Replace(" invoker) =>", " invoker) where TResponse : class =>", StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(contract)),
        };
        var execution = ExecuteGenerator(source);
        Assert.Multiple(() =>
        {
            Assert.That(execution.GeneratorDiagnostics.Select(static item => item.Id), Is.EqualTo(["GF_Cqrs_002"]));
            Assert.That(execution.GeneratorDiagnostics.Single().Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(execution.InputCompilationDiagnostics.Where(static item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(execution.GeneratedSources, Is.Empty);
        });
    }

}

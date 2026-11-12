// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Godot.UI;
using Microsoft.CodeAnalysis.CSharp;

namespace GFramework.Godot.SourceGenerators.Tests.Adoption;

/// <summary>Compiles adoption inputs against the actual Godot and framework contracts without starting native code.</summary>
internal static class AdoptionCompilation
{
    /// <summary>Creates a consumer compilation using the test host's resolved dependency graph.</summary>
    /// <param name="source">Expanded template or generator consumer source.</param>
    /// <returns>A compilation with actual runtime and attribute metadata.</returns>
    internal static CSharpCompilation Create(string source)
    {
        var assemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(typeof(IGodotUiLifecycle).Assembly.Location)
            .Append(typeof(global::Godot.Node).Assembly.Location)
            .Append(typeof(global::GFramework.Godot.SourceGenerators.Abstractions.GetNodeAttribute).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create("AdoptionProbe", [CSharpSyntaxTree.ParseText(source)], assemblies,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>Emits the explicit, consumer-owned registration entrypoint.</summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    /// <summary>Emits a consumer-owned factory so normal startup never discovers or activates a registry.</summary>
    /// <param name="builder">The generated source.</param>
    /// <param name="requiresFallback">Whether complete registration requires explicit assembly scanning.</param>
    private static void AppendDirectRegistryFactory(StringBuilder builder, bool requiresFallback)
    {
        builder.AppendLine();
        builder.AppendLine("/// <summary>Creates this consumer assembly's generated registration without assembly scanning.</summary>");
        builder.AppendLine("internal static class CqrsRegistration");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Returns the complete generated registry; use explicit assembly registration when fallback is required.</summary>");
        builder.AppendLine("    public static global::GFramework.Cqrs.ICqrsHandlerRegistry CreateRegistry() =>");
        builder.AppendLine(requiresFallback
            ? "        throw new global::System.InvalidOperationException(\"This assembly requires explicit AddCqrsHandlersFromAssembly registration for fallback handlers.\");"
            : "        new __GFrameworkGeneratedCqrsHandlerRegistry();");
        builder.AppendLine("}");
    }
}

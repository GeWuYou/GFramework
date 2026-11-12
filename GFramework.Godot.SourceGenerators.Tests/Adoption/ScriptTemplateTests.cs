// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace GFramework.Godot.SourceGenerators.Tests.Adoption;

/// <summary>Checks the shipped editor templates after the same substitutions made when creating a script.</summary>
[TestFixture]
public sealed class ScriptTemplateTests
{
    /// <summary>New default scripts compile using their real native base and framework dependencies.</summary>
    /// <param name="filename">The shipped template.</param>
    /// <param name="baseType">The intended Godot base.</param>
    [TestCase("Node/ControllerTemplate.cs", "Node")]
    [TestCase("Node/ControllerTemplate.cs", "Control")]
    [TestCase("Control/PageControllerTemplate.cs", "Control")]
    public void ExpandedTemplateCompiles(string filename, string baseType)
    {
        var source = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptTemplates", filename))
            .Replace("_CLASS_", "ExpandedTemplate", StringComparison.Ordinal)
            .Replace("_BASE_", baseType, StringComparison.Ordinal);
        var errors = AdoptionCompilation.Create(source).GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
    }
}

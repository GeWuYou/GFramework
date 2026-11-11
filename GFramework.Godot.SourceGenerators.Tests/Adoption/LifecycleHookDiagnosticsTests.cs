// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.CodeAnalysis.CSharp;

namespace GFramework.Godot.SourceGenerators.Tests.Adoption;

/// <summary>Exercises handwritten lifecycle diagnostics through both generators and real consumer metadata.</summary>
[TestFixture]
public sealed class LifecycleHookDiagnosticsTests
{
    /// <summary>Only supported current-instance calls reachable from the lifecycle satisfy its hook requirement.</summary>
    /// <param name="body">Lifecycle body using a hook placeholder.</param>
    /// <param name="helpers">Additional source members.</param>
    /// <param name="warn">Whether the call graph misses the hook.</param>
    [TestCase("$HOOK();", "", false)]
    [TestCase("this.$HOOK();", "", false)]
    [TestCase("(this).$HOOK();", "", false)]
    [TestCase("Setup();", "private void Setup() => $HOOK();", false)]
    [TestCase("Setup(); void Setup() { $HOOK(); }", "", false)]
    [TestCase("Setup();", "private void Setup() { Cycle(); } private void Cycle() { Setup(); $HOOK(); }", false)]
    [TestCase("Cycle();", "private void Cycle() { Cycle(); }", true)]
    [TestCase("other.$HOOK();", "private Hud other = null!;", true)]
    [TestCase("void Setup() { $HOOK(); }", "", true)]
    [TestCase("Action later = () => $HOOK();", "", true)]
    [TestCase("Action $HOOK = () => { }; $HOOK();", "", true)]
    [TestCase("Setup();", "", false)]
    public void RecognizesCurrentInstanceHook(string body, string helpers, bool warn)
    {
        Verify(signals: false, body, helpers, warn);
        Verify(signals: true, body, helpers, warn);
    }

    /// <summary>Unbinding uses the same current-instance analysis as startup binding.</summary>
    [Test]
    public void OtherInstanceUnbindDoesNotSatisfyExitHook()
    {
        Verify(signals: true, "$HOOK();", "private Hud other = null!;", warn: false,
            "other.__UnbindNodeSignals_Generated();", expectedExitWarning: true);
    }

    private static void Verify(bool signals, string body, string helpers, bool warn,
        string exit = "__UnbindNodeSignals_Generated();", bool expectedExitWarning = false)
    {
        var hook = signals ? "__BindNodeSignals_Generated" : "__InjectGetNodes_Generated";
        var declaration = signals
            ? "[BindNodeSignal(nameof(button), nameof(Button.Pressed))] private void OnPressed() { }"
            : string.Empty;
        var attribute = signals ? string.Empty : "[GetNode(\"%Action\")]";
        var source = $$"""
            using System;
            using Godot;
            using GFramework.Godot.SourceGenerators.Abstractions;
            namespace AdoptionProbe;
            public partial class Hud : Node
            {
                {{attribute}} private Button button = null!;
                {{declaration}}
                public override void _Ready() { {{body.Replace("$HOOK", hook, StringComparison.Ordinal)}} }
                public override void _ExitTree() { {{(signals ? exit : string.Empty)}} }
                {{helpers.Replace("$HOOK", hook, StringComparison.Ordinal)}}
            }
            public partial class Hud
            {
                {{(string.Equals(body, "Setup();", StringComparison.Ordinal) && helpers.Length == 0 ? "private void Setup() => " + hook + "();" : string.Empty)}}
            }
            """;
        IIncrementalGenerator generator = signals ? new BindNodeSignalGenerator() : new GetNodeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(AdoptionCompilation.Create(source), out var output, out _);
        var diagnostics = driver.GetRunResult().Diagnostics;
        var readyId = signals ? "GF_Godot_BindNodeSignal_008" : "GF_Godot_GetNode_006";
        Assert.That(diagnostics.Count(item => string.Equals(item.Id, readyId, StringComparison.Ordinal)), Is.EqualTo(warn ? 1 : 0));
        Assert.That(diagnostics.Count(item => string.Equals(item.Id, "GF_Godot_BindNodeSignal_009", StringComparison.Ordinal)), Is.EqualTo(expectedExitWarning ? 1 : 0));
        Assert.That(output.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.UI;
using Godot;

namespace GFramework.Godot.Tests.UI;

/// <summary>Checks the effective view policy independently from native Control construction.</summary>
[TestFixture]
public sealed class GodotUiViewPausePolicyTests
{
    /// <summary>Ordinary inherited views must not adopt the UI mount's Always policy.</summary>
    [TestCase(Node.ProcessModeEnum.Inherit, Node.ProcessModeEnum.Pausable)]
    [TestCase(Node.ProcessModeEnum.Pausable, Node.ProcessModeEnum.Pausable)]
    [TestCase(Node.ProcessModeEnum.WhenPaused, Node.ProcessModeEnum.WhenPaused)]
    [TestCase(Node.ProcessModeEnum.Always, Node.ProcessModeEnum.Always)]
    [TestCase(Node.ProcessModeEnum.Disabled, Node.ProcessModeEnum.Disabled)]
    public void FalsePolicy_ResolvesInheritedModeWithoutReplacingExplicitMode(Node.ProcessModeEnum authored, Node.ProcessModeEnum expected)
    {
        Assert.That(GodotUiView.ResolveProcessMode(authored, continueWhenPaused: false), Is.EqualTo(expected));
    }

    /// <summary>Explicit continue-on-pause route policy takes precedence over the authored root mode.</summary>
    [TestCase(Node.ProcessModeEnum.Inherit)]
    [TestCase(Node.ProcessModeEnum.Pausable)]
    [TestCase(Node.ProcessModeEnum.Disabled)]
    public void ContinuePolicy_UsesAlways(Node.ProcessModeEnum authored)
    {
        Assert.That(GodotUiView.ResolveProcessMode(authored, continueWhenPaused: true), Is.EqualTo(Node.ProcessModeEnum.Always));
    }
}

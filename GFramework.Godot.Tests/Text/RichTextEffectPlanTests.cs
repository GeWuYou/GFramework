// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Tests.Text;

/// <summary>
///     <see cref="RichTextEffectPlan" /> 的纯托管测试。
/// </summary>
[TestFixture]
public sealed class RichTextEffectPlanTests
{
    /// <summary>
    ///     验证默认内置计划会暴露完整的第一阶段效果键集合。
    /// </summary>
    [Test]
    public void CreateBuiltInDefault_Should_Contain_The_First_Phase_Effect_Keys()
    {
        var plan = RichTextEffectPlan.CreateBuiltInDefault();

        Assert.That(plan.Effects.Select(static entry => entry.Key), Is.EqualTo(new[]
        {
            "green",
            "red",
            "gold",
            "blue",
            "fade_in",
            "sine",
            "jitter",
            "fly_in"
        }));
    }
}

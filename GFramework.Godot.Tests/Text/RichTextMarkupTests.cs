// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Godot.Text;

namespace GFramework.Godot.Tests.Text;

/// <summary>
///     <see cref="RichTextMarkup" /> 的测试。
/// </summary>
[TestFixture]
public sealed class RichTextMarkupTests
{
    /// <summary>
    ///     验证颜色快捷方法会输出预期标签。
    /// </summary>
    [Test]
    public void Green_Should_Wrap_Text_With_Green_Tag()
    {
        var result = RichTextMarkup.Green("Ready");

        Assert.That(result, Is.EqualTo("[green]Ready[/green]"));
    }

    /// <summary>
    ///     验证效果方法会按稳定顺序拼接环境参数。
    /// </summary>
    [Test]
    public void Effect_Should_Sort_Environment_Parameters_By_Key()
    {
        var env = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tick"] = 0.1f,
            ["speed"] = 4
        };

        var result = RichTextMarkup.Effect("Hello", "fade_in", env);

        Assert.That(result, Is.EqualTo("[fade_in speed=4 tick=0.1]Hello[/fade_in]"));
    }

    /// <summary>
    ///     验证非法标签 token 会被拒绝，避免生成损坏的 BBCode。
    /// </summary>
    [Test]
    public void Effect_Should_Reject_Invalid_Tag_Tokens()
    {
        var exception = Assert.Throws<ArgumentException>(() => RichTextMarkup.Effect("Hello", "fade=in"));

        Assert.That(exception!.ParamName, Is.EqualTo("tag"));
    }

    /// <summary>
    ///     验证非法环境参数键会被拒绝，避免注入无效的 BBCode token。
    /// </summary>
    [Test]
    public void Effect_Should_Reject_Invalid_Environment_Key_Tokens()
    {
        var env = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["bad key"] = 1
        };

        var exception = Assert.Throws<ArgumentException>(() => RichTextMarkup.Effect("Hello", "fade_in", env));

        Assert.That(exception!.ParamName, Is.EqualTo("env"));
    }
}

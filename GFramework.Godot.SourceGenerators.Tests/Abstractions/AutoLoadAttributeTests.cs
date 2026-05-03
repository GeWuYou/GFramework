// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.SourceGenerators.Abstractions;

namespace GFramework.Godot.SourceGenerators.Tests.Abstractions;

/// <summary>
///     验证 <see cref="AutoLoadAttribute" /> 的参数约束。
/// </summary>
[TestFixture]
public class AutoLoadAttributeTests
{
    /// <summary>
    ///     验证构造函数会保留合法的 AutoLoad 名称。
    /// </summary>
    [Test]
    public void Constructor_Should_Store_Name_When_Name_Is_Valid()
    {
        var attribute = new AutoLoadAttribute("GameServices");

        Assert.That(attribute.Name, Is.EqualTo("GameServices"));
    }

    /// <summary>
    ///     验证构造函数会拒绝空引用。
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Name_Is_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AutoLoadAttribute(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("name"));
    }

    /// <summary>
    ///     验证构造函数会拒绝空字符串与仅空白字符串。
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    public void Constructor_Should_Throw_When_Name_Is_Empty_Or_Whitespace(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new AutoLoadAttribute(name));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.ParamName, Is.EqualTo("name"));
            Assert.That(exception.Message, Does.Contain("empty or whitespace"));
        });
    }
}

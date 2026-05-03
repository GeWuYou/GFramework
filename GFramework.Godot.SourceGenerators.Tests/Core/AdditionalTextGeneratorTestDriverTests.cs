// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.SourceGenerators.Tests.Core;

/// <summary>
///     验证 <see cref="AdditionalTextGeneratorTestDriver" /> 的文本规范化行为。
/// </summary>
[TestFixture]
public class AdditionalTextGeneratorTestDriverTests
{
    /// <summary>
    ///     验证不同平台换行最终都会被统一为 LF。
    /// </summary>
    [Test]
    public void NormalizeLineEndings_Should_Convert_All_Line_Endings_To_Lf()
    {
        const string content = "line1\r\nline2\rline3\nline4";

        var normalized = AdditionalTextGeneratorTestDriver.NormalizeLineEndings(content);

        Assert.That(normalized, Is.EqualTo("line1\nline2\nline3\nline4"));
    }
}

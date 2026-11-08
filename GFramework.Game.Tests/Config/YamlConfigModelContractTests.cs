// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证内部 schema 运行时模型会在构造阶段拒绝无效状态，
///     避免调用方把不一致的约束对象继续传入加载器和校验器。
/// </summary>
[TestFixture]
public sealed partial class YamlConfigModelContractTests
{
    /// <summary>Provides the non-null regex used to reject an unmatched pattern/regex pair.</summary>
    /// <value>The same value pattern and one-second timeout as the constructor fixture.</value>
    [GeneratedRegex("value", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ValuePatternRegex
    {
        get;
    }

    /// <summary>
    ///     验证枚举允许值模型会拒绝空白比较键。
    /// </summary>
    [Test]
    public void AllowedValue_Should_Reject_Whitespace_Comparable_Value()
    {
        Assert.Throws<ArgumentException>(() => new YamlConfigAllowedValue(" ", "visible"));
    }

    /// <summary>
    ///     验证枚举允许值模型会保留空对象等合法结构产生的空比较键。
    /// </summary>
    [Test]
    public void AllowedValue_Should_Accept_Empty_Comparable_Value()
    {
        var allowedValue = new YamlConfigAllowedValue(string.Empty, "{}");

        Assert.That(allowedValue.ComparableValue, Is.Empty);
    }

    /// <summary>
    ///     验证常量约束模型会拒绝空白比较键。
    /// </summary>
    [Test]
    public void ConstantValue_Should_Reject_Whitespace_Comparable_Value()
    {
        Assert.Throws<ArgumentException>(() => new YamlConfigConstantValue(" ", "\"visible\""));
    }

    /// <summary>
    ///     验证常量约束模型会保留空对象等合法结构产生的空比较键。
    /// </summary>
    [Test]
    public void ConstantValue_Should_Accept_Empty_Comparable_Value()
    {
        var constantValue = new YamlConfigConstantValue(string.Empty, "{}");

        Assert.That(constantValue.ComparableValue, Is.Empty);
    }

    /// <summary>
    ///     验证 contains 约束模型会在构造阶段拦截负值和反向区间。
    /// </summary>
    [Test]
    public void ArrayContainsConstraints_Should_Reject_Invalid_Bounds()
    {
        var itemNode = CreateStringNode();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new YamlConfigArrayContainsConstraints(itemNode, -1, maxContains: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new YamlConfigArrayContainsConstraints(itemNode, minContains: null, -1));
            Assert.Throws<ArgumentException>(() => new YamlConfigArrayContainsConstraints(itemNode, 3, 2));
        });
    }

    /// <summary>
    ///     验证数组约束模型会在构造阶段拦截负值和反向区间。
    /// </summary>
    [Test]
    public void ArrayConstraints_Should_Reject_Invalid_Bounds()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new YamlConfigArrayConstraints(-1, maxItems: null, uniqueItems: false, containsConstraints: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new YamlConfigArrayConstraints(minItems: null, -1, uniqueItems: false, containsConstraints: null));
            Assert.Throws<ArgumentException>(() => new YamlConfigArrayConstraints(4, 3, uniqueItems: false, containsConstraints: null));
        });
    }

    /// <summary>
    ///     验证对象约束模型会在构造阶段拦截负值和反向区间。
    /// </summary>
    [Test]
    public void ObjectConstraints_Should_Reject_Invalid_Bounds()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new YamlConfigObjectConstraints(-1, maxProperties: null, dependentRequired: null, dependentSchemas: null, allOfSchemas: null, conditionalSchemas: null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new YamlConfigObjectConstraints(minProperties: null, -1, dependentRequired: null, dependentSchemas: null, allOfSchemas: null, conditionalSchemas: null));
            Assert.Throws<ArgumentException>(() =>
                new YamlConfigObjectConstraints(5, 4, dependentRequired: null, dependentSchemas: null, allOfSchemas: null, conditionalSchemas: null));
        });
    }

    /// <summary>
    ///     验证字符串约束模型要求正则原文与预编译正则成对出现。
    /// </summary>
    [Test]
    public void StringConstraints_Should_Require_Pattern_And_Regex_To_Be_Paired()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new YamlConfigStringConstraints(minLength: null, maxLength: null, "value", patternRegex: null, formatConstraint: null));
            Assert.Throws<ArgumentException>(() =>
                new YamlConfigStringConstraints(
minLength: null,
maxLength: null,
pattern: null,
                    ValuePatternRegex,
formatConstraint: null));
        });
    }

    /// <summary>
    ///     验证 schema 模型会复制引用表集合，避免外部可变集合继续污染内部状态。
    /// </summary>
    [Test]
    public void Schema_Should_Copy_Referenced_Table_Names()
    {
        var referencedTableNames = new List<string> { "item" };
        var schema = new YamlConfigSchema("monster.schema.json", CreateStringNode(), referencedTableNames);

        referencedTableNames.Add("weapon");

        Assert.Multiple(() =>
        {
            Assert.That(schema.ReferencedTableNames, Is.EqualTo(["item"]));
            Assert.That(schema.ReferencedTableNames, Is.Not.SameAs(referencedTableNames));
        });
    }

    private static YamlConfigSchemaNode CreateStringNode()
    {
        return YamlConfigSchemaNode.CreateScalar(
            YamlConfigSchemaPropertyType.String,
            referenceTableName: null,
            allowedValues: null,
            constraints: null,
            schemaPathHint: "tests.schema.json");
    }
}

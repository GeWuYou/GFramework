// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 <see cref="CqrsReflectionFallbackAttribute" /> 公开构造器的归一化合同，
///     以固定 runtime 读取程序集级 fallback 元数据时可依赖的可观察语义。
/// </summary>
[TestFixture]
internal sealed class CqrsReflectionFallbackAttributeTests
{
    /// <summary>
    ///     验证无参构造器会保留旧版 marker 语义，并暴露空的 fallback 集合。
    /// </summary>
    [Test]
    public void Constructor_Without_Arguments_Should_Expose_Empty_Fallback_Collections()
    {
        var attribute = new CqrsReflectionFallbackAttribute();

        Assert.Multiple(() =>
        {
            Assert.That(attribute.FallbackHandlerTypeNames, Is.Empty);
            Assert.That(attribute.FallbackHandlerTypes, Is.Empty);
        });
    }

    /// <summary>
    ///     验证字符串名称重载会过滤空白项，并按序号稳定去重排序，
    ///     确保 runtime 后续读取到的名称清单不依赖调用端输入顺序。
    /// </summary>
    [Test]
    public void Constructor_With_Type_Names_Should_Normalize_By_Filtering_Deduplicating_And_Sorting()
    {
        var attribute = new CqrsReflectionFallbackAttribute(
            "Zeta.Handler",
            "  ",
            "Alpha.Handler",
            "Zeta.Handler",
            string.Empty,
            "Beta.Handler",
            "Alpha.Handler");

        Assert.Multiple(() =>
        {
            Assert.That(
                attribute.FallbackHandlerTypeNames,
                Is.EqualTo(["Alpha.Handler", "Beta.Handler", "Zeta.Handler"]));
            Assert.That(attribute.FallbackHandlerTypes, Is.Empty);
        });
    }

    /// <summary>
    ///     验证字符串名称重载收到 <see langword="null" /> 参数数组时会立即拒绝，
    ///     避免 runtime 在读取程序集元数据时延迟暴露无效状态。
    /// </summary>
    [Test]
    public void Constructor_With_Null_Type_Name_Array_Should_Throw_ArgumentNullException()
    {
        Assert.That(
            () => _ = new CqrsReflectionFallbackAttribute((string[])null!),
            Throws.ArgumentNullException);
    }

    /// <summary>
    ///     验证 <see cref="Type" /> 重载会过滤空引用，并按稳定名称顺序去重，
    ///     确保后续 fallback 补扫不会因为重复输入或反射枚举顺序产生非确定性。
    /// </summary>
    [Test]
    public void Constructor_With_Types_Should_Normalize_By_Filtering_Deduplicating_And_Sorting()
    {
        var attribute = new CqrsReflectionFallbackAttribute(
            typeof(string),
            null!,
            typeof(Uri),
            typeof(string),
            typeof(Version));

        // 这里按 FullName 的 Ordinal 顺序断言，固定该 attribute 对 runtime 暴露的元数据排序合同。
        Assert.Multiple(() =>
        {
            Assert.That(
                attribute.FallbackHandlerTypes,
                Is.EqualTo([typeof(string), typeof(Uri), typeof(Version)]));
            Assert.That(attribute.FallbackHandlerTypeNames, Is.Empty);
        });
    }

    /// <summary>
    ///     验证 <see cref="Type" /> 重载收到 <see langword="null" /> 参数数组时会立即拒绝，
    ///     从而维持 attribute 元数据的最小有效性边界。
    /// </summary>
    [Test]
    public void Constructor_With_Null_Type_Array_Should_Throw_ArgumentNullException()
    {
        Assert.That(
            () => _ = new CqrsReflectionFallbackAttribute((Type[])null!),
            Throws.ArgumentNullException);
    }
}

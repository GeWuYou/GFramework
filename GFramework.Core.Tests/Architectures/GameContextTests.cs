// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     GameContext类的单元测试
///     测试内容包括：
///     - ArchitectureReadOnlyDictionary在启动时为空
///     - Bind方法添加上下文到字典
///     - Bind重复类型时抛出异常
///     - GetByType返回正确的上下文
///     - GetByType未找到时抛出异常
///     - Get泛型方法返回正确的上下文
///     - TryGet方法在找到时返回true
///     - TryGet方法在未找到时返回false
///     - GetFirstArchitectureContext在存在时返回
///     - GetFirstArchitectureContext为空时抛出异常
///     - Unbind移除上下文
///     - Clear移除所有上下文
/// </summary>
[TestFixture]
public class GameContextTests
{
    /// <summary>
    ///     测试初始化方法，在每个测试方法执行前清空GameContext
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     测试清理方法，在每个测试方法执行后清空GameContext
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     测试ArchitectureReadOnlyDictionary在启动时返回空字典
    /// </summary>
    [Test]
    public void ArchitectureReadOnlyDictionary_Should_Return_Empty_At_Start()
    {
        var dict = GameContext.ArchitectureReadOnlyDictionary;

        Assert.That(dict.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试Bind方法是否正确将上下文添加到字典中
    /// </summary>
    [Test]
    public void Bind_Should_Add_Context_To_Dictionary()
    {
        var context = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context);

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试Bind方法在绑定重复类型时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void Bind_WithDuplicateType_Should_ThrowInvalidOperationException()
    {
        var context1 = new TestArchitectureContext();
        var context2 = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context1);

        Assert.Throws<InvalidOperationException>(() =>
            GameContext.Bind(typeof(TestArchitecture), context2));
    }

    /// <summary>
    ///     测试GetByType方法是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetByType_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetByType(typeof(TestArchitecture));

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试GetByType方法在未找到对应类型时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void GetByType_Should_Throw_When_Not_Found()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetByType(typeof(TestArchitecture)));
    }

    /// <summary>
    ///     测试Get泛型方法是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetGeneric_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.Get<TestArchitectureContext>();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试TryGet方法在找到上下文时是否返回true并正确设置输出参数
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnTrue_When_Found()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    /// <summary>
    ///     测试TryGet方法在未找到上下文时是否返回false且输出参数为null
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnFalse_When_Not_Found()
    {
        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.False);
        Assert.That(foundContext, Is.Null);
    }

    /// <summary>
    ///     测试GetFirstArchitectureContext方法在存在上下文时是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetFirstArchitectureContext_Should_Return_When_Exists()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetFirstArchitectureContext();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试GetFirstArchitectureContext方法在没有上下文时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void GetFirstArchitectureContext_Should_Throw_When_Empty()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetFirstArchitectureContext());
    }

    /// <summary>
    ///     测试Unbind方法是否正确移除指定类型的上下文
    /// </summary>
    [Test]
    public void Unbind_Should_Remove_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        GameContext.Unbind(typeof(TestArchitecture));

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试Clear方法是否正确移除所有上下文
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Contexts()
    {
        GameContext.Bind(typeof(TestArchitecture), new TestArchitectureContext());
        GameContext.Bind(typeof(TestArchitectureContext), new TestArchitectureContext());

        GameContext.Clear();

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }
}

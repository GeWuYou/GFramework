// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     GameContext 类的单元测试
///     测试内容包括：
///     - 初始状态为空
///     - 绑定后可通过架构类型和上下文类型回查
///     - 不允许并存绑定两个不同上下文实例
///     - 清理和解绑会同步更新当前活动上下文
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
    ///     测试绑定第二个不同的上下文实例时会被拒绝。
    /// </summary>
    [Test]
    public void Bind_WithDifferentContextInstance_Should_ThrowInvalidOperationException()
    {
        var firstContext = new TestArchitectureContext();
        var secondContext = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), firstContext);

        Assert.Throws<InvalidOperationException>(() =>
            GameContext.Bind(typeof(AnotherTestArchitectureContext), secondContext));
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
    ///     测试 GetByType 支持按当前活动上下文的具体类型回查。
    /// </summary>
    [Test]
    public void GetByType_Should_Return_Current_Context_When_Requested_By_Context_Type()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetByType(typeof(TestArchitectureContext));

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试 Get 泛型方法在仅绑定架构类型时也能返回当前上下文
    /// </summary>
    [Test]
    public void GetGeneric_Should_Return_Current_Context_When_Bound_By_Architecture_Type()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.Get<TestArchitectureContext>();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试 TryGet 方法在仅绑定架构类型时也能找到当前上下文
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnTrue_When_Bound_By_Architecture_Type()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    /// <summary>
    ///     测试 TryGet 方法在未找到上下文时是否返回 false 且输出参数为 null
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
    ///     测试 Unbind 方法在移除最后一个别名时会清空当前活动上下文
    /// </summary>
    [Test]
    public void Unbind_Should_Remove_Context_When_Last_Alias_Is_Removed()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        GameContext.Unbind(typeof(TestArchitecture));

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试 Unbind 方法在仍有其他别名时保留当前活动上下文
    /// </summary>
    [Test]
    public void Unbind_Should_Keep_Current_Context_When_Another_Alias_Remains()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);
        GameContext.Bind(typeof(TestArchitectureContext), context);

        GameContext.Unbind(typeof(TestArchitecture));

        Assert.That(GameContext.GetFirstArchitectureContext(), Is.SameAs(context));
        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试 Clear 方法是否正确移除所有上下文
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Contexts()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);
        GameContext.Bind(typeof(TestArchitectureContext), context);

        GameContext.Clear();

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() => GameContext.GetFirstArchitectureContext());
    }
}

using System.ComponentModel;
using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Architectures;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Core.Ioc;

namespace GFramework.Core.Tests.Cqrs;

/// <summary>
///     锁定历史 Mediator 兼容入口的正式弃用策略。
///     这些测试确保旧 API 不仅保留行为兼容，还会通过编译期提示和 IntelliSense 隐藏引导调用方迁移到新的 CQRS 命名。
/// </summary>
[TestFixture]
public class MediatorCompatibilityDeprecationTests
{
    /// <summary>
    ///     验证公开兼容方法仍可用，但已被显式标记为未来移除的旧别名。
    /// </summary>
    [Test]
    public void Legacy_Public_Methods_Should_Be_Obsolete_And_Hidden_From_Editor_Browsing()
    {
        AssertLegacyMethod(typeof(IArchitecture), nameof(IArchitecture.RegisterMediatorBehavior));
        AssertLegacyMethod(typeof(IIocContainer), nameof(IIocContainer.RegisterMediatorBehavior));
        AssertLegacyMethod(typeof(Architecture), nameof(Architecture.RegisterMediatorBehavior));
        AssertLegacyMethod(typeof(MicrosoftDiContainer), nameof(MicrosoftDiContainer.RegisterMediatorBehavior));
    }

    /// <summary>
    ///     验证历史扩展类型会把迁移目标写入弃用说明，并从 IntelliSense 主路径隐藏。
    /// </summary>
    [Test]
    public void Legacy_Extension_Types_Should_Be_Obsolete_And_Hidden_From_Editor_Browsing()
    {
        AssertLegacyType(
            typeof(ContextAwareMediatorExtensions),
            "Use GFramework.Core.Cqrs.Extensions.ContextAwareCqrsExtensions instead.");
        AssertLegacyType(
            typeof(ContextAwareMediatorCommandExtensions),
            "Use GFramework.Core.Cqrs.Extensions.ContextAwareCqrsCommandExtensions instead.");
        AssertLegacyType(
            typeof(ContextAwareMediatorQueryExtensions),
            "Use GFramework.Core.Cqrs.Extensions.ContextAwareCqrsQueryExtensions instead.");
        AssertLegacyType(
            typeof(MediatorCoroutineExtensions),
            "Use GFramework.Core.Cqrs.Extensions.CqrsCoroutineExtensions instead.");
    }

    /// <summary>
    ///     断言方法级兼容 API 具备统一的弃用元数据。
    /// </summary>
    /// <param name="declaringType">声明该方法的类型。</param>
    /// <param name="methodName">方法名称。</param>
    private static void AssertLegacyMethod(Type declaringType, string methodName)
    {
        var method = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.Name == methodName);

        var obsoleteAttribute = method.GetCustomAttribute<ObsoleteAttribute>();
        var editorBrowsableAttribute = method.GetCustomAttribute<EditorBrowsableAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(obsoleteAttribute, Is.Not.Null);
            Assert.That(
                obsoleteAttribute!.Message,
                Does.Contain("Use RegisterCqrsPipelineBehavior<TBehavior>() instead."));
            Assert.That(
                obsoleteAttribute.Message,
                Does.Contain("removed in a future major version"));
            Assert.That(editorBrowsableAttribute, Is.Not.Null);
            Assert.That(editorBrowsableAttribute!.State, Is.EqualTo(EditorBrowsableState.Never));
        });
    }

    /// <summary>
    ///     断言类型级兼容扩展具备统一的弃用元数据。
    /// </summary>
    /// <param name="type">兼容扩展类型。</param>
    /// <param name="expectedReplacementHint">期望的迁移提示。</param>
    private static void AssertLegacyType(Type type, string expectedReplacementHint)
    {
        var obsoleteAttribute = type.GetCustomAttribute<ObsoleteAttribute>();
        var editorBrowsableAttribute = type.GetCustomAttribute<EditorBrowsableAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(obsoleteAttribute, Is.Not.Null);
            Assert.That(obsoleteAttribute!.Message, Does.Contain(expectedReplacementHint));
            Assert.That(
                obsoleteAttribute.Message,
                Does.Contain("removed in a future major version"));
            Assert.That(editorBrowsableAttribute, Is.Not.Null);
            Assert.That(editorBrowsableAttribute!.State, Is.EqualTo(EditorBrowsableState.Never));
        });
    }
}

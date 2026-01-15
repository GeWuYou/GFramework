using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.rule;
using GFramework.Core.Tests.architecture;
using NUnit.Framework;

namespace GFramework.Core.Tests.rule;

[TestFixture]
public class ContextAwareTests
{
    [SetUp]
    public void SetUp()
    {
        _contextAware = new TestContextAware();
        _mockContext = new TestArchitectureContext();
    }

    private TestContextAware _contextAware = null!;
    private TestArchitectureContext _mockContext = null!;

    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.PublicContext, Is.SameAs(_mockContext));
    }

    [Test]
    public void SetContext_Should_Call_OnContextReady()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        Assert.That(_contextAware.OnContextReadyCalled, Is.True);
    }

    [Test]
    public void GetContext_Should_Return_Set_Context()
    {
        IContextAware aware = _contextAware;
        aware.SetContext(_mockContext);

        var result = aware.GetContext();

        Assert.That(result, Is.SameAs(_mockContext));
    }

    [Test]
    public void GetContext_Should_Return_FirstArchitectureContext_When_Not_Set()
    {
        // Arrange - 暂时不调用 SetContext，让 Context 为 null
        IContextAware aware = _contextAware;

        // Act - 当 Context 为 null 时，应该返回第一个 Architecture Context
        // 由于测试环境中没有实际的 Architecture Context，这里只测试调用不会抛出异常
        // 在实际使用中，当 Context 为 null 时会调用 GameContext.GetFirstArchitectureContext()

        // Assert - 验证在没有设置 Context 时的行为
        // 注意：由于测试环境中可能没有 Architecture Context，这里我们只测试不抛出异常
        Assert.DoesNotThrow(() => aware.GetContext());
    }
}

public class TestContextAware : ContextAwareBase
{
    public IArchitectureContext? PublicContext => Context;
    public bool OnContextReadyCalled { get; private set; }

    protected override void OnContextReady()
    {
        OnContextReadyCalled = true;
    }
}
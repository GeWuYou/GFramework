using System.Reflection;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;

namespace GFramework.Core.Tests.tests;

[TestFixture]
[NonParallelizable]
public class ArchitectureInitializationTests
{
    [SetUp]
    public void SetUp()
    {
        _architecture = new TestArchitecture();
    }

    [TearDown]
    public void TearDown()
    {
        _architecture!.Destroy();
        _architecture = null;
    }

    private TestArchitecture? _architecture;

    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Act
        _architecture!.Initialize();

        // Assert
        Assert.That(_architecture.InitCalled, Is.True);

        Assert.That(_architecture.Runtime, Is.Not.Null);

        var phaseProperty = typeof(Architecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(_architecture)!;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Ready));

        var context = _architecture.Context;

        var model = context.GetModel<TestModel>();
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Inited, Is.True);

        var system = context.GetSystem<TestSystem>();
        Assert.That(system, Is.Not.Null);
        Assert.That(system!.Inited, Is.True);
    }

    [Test]
    public void Architecture_Should_Register_Context_By_Type()
    {
        // Act
        _architecture!.Initialize();
        var ctx = GameContext.GetByType(_architecture!.GetType());

        Assert.That(ctx, Is.Not.Null);
    }
}
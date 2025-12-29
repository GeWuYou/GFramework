using System.Reflection;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.Tests.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;
using NUnit.Framework;

namespace GFramework.Core.Tests.tests;

[TestFixture]
public class ArchitectureInitializationTests
{
    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Arrange
        var architecture = new TestArchitecture();

        // Act
        architecture.Initialize();

        // Assert - Init() 被调用
        Assert.That(architecture.InitCalled, Is.True, "Architecture.Init() should be called");

        // Assert - Runtime 已创建
        Assert.That(architecture.Runtime, Is.Not.Null, "ArchitectureRuntime should be created");

        // Assert - Phase 已进入 Ready
        var phaseProperty = typeof(Architecture)
            .GetProperty("CurrentPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        var phase = (ArchitecturePhase)phaseProperty!.GetValue(architecture)!;
        Assert.That(phase, Is.EqualTo(ArchitecturePhase.Ready), "Architecture should be in Ready phase");

        // Assert - Model 初始化
        var context = architecture.Context;
        var model = context.GetModel<TestModel>();
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Inited, Is.True, "Model should be initialized");

        // Assert - System 初始化
        var system = context.GetSystem<TestSystem>();
        Assert.That(system, Is.Not.Null);
        Assert.That(system.Inited, Is.True, "System should be initialized");
    }
}
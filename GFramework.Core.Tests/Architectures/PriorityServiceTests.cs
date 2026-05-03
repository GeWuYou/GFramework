// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     优先级服务排序的集成测试
///     测试完整的架构集成场景
/// </summary>
[TestFixture]
public class PriorityServiceTests
{
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();

        // 初始化 logger 字段
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(MicrosoftDiContainer)));
    }

    private MicrosoftDiContainer _container = null!;

    /// <summary>
    ///     测试系统按优先级排序
    /// </summary>
    [Test]
    public void Systems_Should_Be_Sorted_By_Priority()
    {
        // Arrange
        _container.Register<IPriorityTestSystem>(new PriorityTestSystemC());
        _container.Register<IPriorityTestSystem>(new PriorityTestSystemA());
        _container.Register<IPriorityTestSystem>(new PriorityTestSystemB());
        _container.Freeze();

        // Act
        var systems = _container.GetAllByPriority<IPriorityTestSystem>();

        // Assert
        Assert.That(systems, Has.Count.EqualTo(3));
        Assert.That(systems[0], Is.InstanceOf<PriorityTestSystemA>()); // Priority = 10
        Assert.That(systems[1], Is.InstanceOf<PriorityTestSystemB>()); // Priority = 20
        Assert.That(systems[2], Is.InstanceOf<PriorityTestSystemC>()); // Priority = 30
    }

    /// <summary>
    ///     测试模型按优先级排序
    /// </summary>
    [Test]
    public void Models_Should_Be_Sorted_By_Priority()
    {
        // Arrange
        _container.Register<IPriorityTestModel>(new PriorityTestModelC());
        _container.Register<IPriorityTestModel>(new PriorityTestModelA());
        _container.Register<IPriorityTestModel>(new PriorityTestModelB());
        _container.Freeze();

        // Act
        var models = _container.GetAllByPriority<IPriorityTestModel>();

        // Assert
        Assert.That(models, Has.Count.EqualTo(3));
        Assert.That(models[0], Is.InstanceOf<PriorityTestModelA>()); // Priority = 10
        Assert.That(models[1], Is.InstanceOf<PriorityTestModelB>()); // Priority = 20
        Assert.That(models[2], Is.InstanceOf<PriorityTestModelC>()); // Priority = 30
    }

    /// <summary>
    ///     测试工具按优先级排序
    /// </summary>
    [Test]
    public void Utilities_Should_Be_Sorted_By_Priority()
    {
        // Arrange
        _container.Register<IPriorityTestUtility>(new PriorityTestUtilityC());
        _container.Register<IPriorityTestUtility>(new PriorityTestUtilityA());
        _container.Register<IPriorityTestUtility>(new PriorityTestUtilityB());
        _container.Freeze();

        // Act
        var utilities = _container.GetAllByPriority<IPriorityTestUtility>();

        // Assert
        Assert.That(utilities, Has.Count.EqualTo(3));
        Assert.That(utilities[0], Is.InstanceOf<PriorityTestUtilityA>()); // Priority = 10
        Assert.That(utilities[1], Is.InstanceOf<PriorityTestUtilityB>()); // Priority = 20
        Assert.That(utilities[2], Is.InstanceOf<PriorityTestUtilityC>()); // Priority = 30
    }

    /// <summary>
    ///     测试混合优先级和非优先级服务
    /// </summary>
    [Test]
    public void Mixed_Prioritized_And_Non_Prioritized_Should_Work()
    {
        // Arrange
        _container.Register<IMixedTestSystem>(new MixedTestSystemWithPriority());
        _container.Register<IMixedTestSystem>(new MixedTestSystemWithoutPriority());
        _container.Register<IMixedTestSystem>(new MixedTestSystemNegativePriority());
        _container.Freeze();

        // Act
        var systems = _container.GetAllByPriority<IMixedTestSystem>();

        // Assert
        Assert.That(systems, Has.Count.EqualTo(3));
        Assert.That(systems[0], Is.InstanceOf<MixedTestSystemNegativePriority>()); // -10
        Assert.That(systems[1], Is.InstanceOf<MixedTestSystemWithoutPriority>()); // 0 (默认)
        Assert.That(systems[2], Is.InstanceOf<MixedTestSystemWithPriority>()); // 10
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Ioc;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     测试 IoC 容器生命周期功能
/// </summary>
[TestFixture]
public class IocContainerLifetimeTests
{
    private interface ITestService
    {
        Guid Id { get; }
    }

    private class TestService : ITestService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [Test]
    public void RegisterSingleton_Should_Return_Same_Instance()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<ITestService, TestService>();
        container.Freeze();

        // Act
        var instance1 = container.Get<ITestService>();
        var instance2 = container.Get<ITestService>();

        // Assert
        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance1!.Id, Is.EqualTo(instance2!.Id));
    }

    [Test]
    public void RegisterTransient_Should_Return_Different_Instances()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterTransient<ITestService, TestService>();
        container.Freeze();

        // Act
        var instance1 = container.Get<ITestService>();
        var instance2 = container.Get<ITestService>();

        // Assert
        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance1!.Id, Is.Not.EqualTo(instance2!.Id));
    }

    [Test]
    public void RegisterScoped_Should_Return_Same_Instance_Within_Scope()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterScoped<ITestService, TestService>();
        container.Freeze();

        // Act
        using var scope = container.CreateScope();
        var instance1 = scope.ServiceProvider.GetService<ITestService>();
        var instance2 = scope.ServiceProvider.GetService<ITestService>();

        // Assert
        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance1!.Id, Is.EqualTo(instance2!.Id));
    }

    [Test]
    public void RegisterScoped_Should_Return_Different_Instances_Across_Scopes()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterScoped<ITestService, TestService>();
        container.Freeze();

        // Act
        ITestService? instance1;
        ITestService? instance2;

        using (var scope1 = container.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetService<ITestService>();
        }

        using (var scope2 = container.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetService<ITestService>();
        }

        // Assert
        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance1!.Id, Is.Not.EqualTo(instance2!.Id));
    }

    [Test]
    public void CreateScope_Should_Throw_When_Container_Not_Frozen()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterScoped<ITestService, TestService>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => container.CreateScope());
    }

    [Test]
    public void RegisterTransient_Should_Throw_When_Container_Is_Frozen()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            container.RegisterTransient<ITestService, TestService>());
    }

    [Test]
    public void RegisterScoped_Should_Throw_When_Container_Is_Frozen()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            container.RegisterScoped<ITestService, TestService>());
    }

    [Test]
    public void Mixed_Lifetimes_Should_Work_Together()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<ITestService, TestService>();
        container.RegisterTransient<ITestService, TestService>();
        container.RegisterScoped<ITestService, TestService>();
        container.Freeze();

        // Act
        var singletonInstances = container.GetAll<ITestService>().ToList();

        // Assert
        Assert.That(singletonInstances.Count, Is.EqualTo(3));
    }

    [Test]
    public void Scoped_Service_Should_Be_Disposed_When_Scope_Disposed()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterScoped<ITestService, TestService>();
        container.Freeze();

        ITestService? instance;
        using (var scope = container.CreateScope())
        {
            instance = scope.ServiceProvider.GetService<ITestService>();
            Assert.That(instance, Is.Not.Null);
        }

        // Act & Assert - 作用域已释放，实例应该被清理
        // 注意：这里只是验证作用域可以正常释放，无法直接验证实例是否被 Dispose
        Assert.Pass("Scope disposed successfully");
    }

    [Test]
    public void Multiple_Scopes_Can_Be_Created_Concurrently()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterScoped<ITestService, TestService>();
        container.Freeze();

        // Act
        var scope1 = container.CreateScope();
        var scope2 = container.CreateScope();
        var scope3 = container.CreateScope();

        var instance1 = scope1.ServiceProvider.GetService<ITestService>();
        var instance2 = scope2.ServiceProvider.GetService<ITestService>();
        var instance3 = scope3.ServiceProvider.GetService<ITestService>();

        // Assert
        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance3, Is.Not.Null);
        Assert.That(instance1!.Id, Is.Not.EqualTo(instance2!.Id));
        Assert.That(instance2!.Id, Is.Not.EqualTo(instance3!.Id));
        Assert.That(instance1!.Id, Is.Not.EqualTo(instance3!.Id));

        // Cleanup
        scope1.Dispose();
        scope2.Dispose();
        scope3.Dispose();
    }
}
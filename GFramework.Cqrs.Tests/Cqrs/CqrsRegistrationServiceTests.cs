// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Tests.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS 程序集注册协调器在程序集键去重层面的可观察行为。
/// </summary>
[TestFixture]
internal sealed class CqrsRegistrationServiceTests
{
    /// <summary>
    ///     验证空程序集输入不会触发底层注册，也不会产生重复跳过日志。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Not_Invoke_Registrar_When_Assemblies_Are_Empty()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Never);
        Assert.That(logger.Logs, Has.Count.EqualTo(0));
    }

    /// <summary>
    ///     验证同一次调用内出现重复程序集键时，底层注册器只会接收到一次注册请求。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Duplicate_Assembly_Key_Only_Once_Per_Call()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var duplicateAssemblyA = CreateAssembly("GFramework.Cqrs.Tests.DuplicateAssembly, Version=1.0.0.0");
        var duplicateAssemblyB = CreateAssembly("GFramework.Cqrs.Tests.DuplicateAssembly, Version=1.0.0.0");
        var expectedAssembly = duplicateAssemblyA.Object;
        IEnumerable<Assembly>? registeredAssemblies = null;

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = [.. assemblies]);

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([duplicateAssemblyA.Object, duplicateAssemblyB.Object]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(registeredAssemblies, Is.Not.Null);
            Assert.That(registeredAssemblies, Is.EqualTo([expectedAssembly]));
            Assert.That(logger.Logs, Has.Count.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证跨两次调用重复程序集键时，协调器会跳过重复注册并写入 debug 日志。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Skip_Already_Registered_Assembly_Key_Across_Calls_And_Log_Debug_Message()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var firstAssembly = CreateAssembly("GFramework.Cqrs.Tests.RegisteredAssembly, Version=1.0.0.0");
        var secondAssembly = CreateAssembly("GFramework.Cqrs.Tests.RegisteredAssembly, Version=1.0.0.0");
        IEnumerable<Assembly>? registeredAssemblies = null;

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = [.. assemblies]);

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([firstAssembly.Object]);
        service.RegisterHandlers([secondAssembly.Object]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(registeredAssemblies, Is.EqualTo([firstAssembly.Object]));
            var debugMessages = logger.Logs
                .Where(static log => log.Level == LogLevel.Debug)
                .Select(static log => log.Message)
                .ToArray();
            Assert.That(debugMessages, Has.Length.EqualTo(1));
            Assert.That(
                debugMessages[0],
                Does.Contain("Skipping CQRS handler registration for assembly"));
            Assert.That(
                debugMessages[0],
                Does.Contain("GFramework.Cqrs.Tests.RegisteredAssembly, Version=1.0.0.0"));
            Assert.That(debugMessages[0], Does.Contain("already registered"));
        });
    }

    /// <summary>
    ///     验证协调器会忽略空项，并按稳定程序集键排序后仅注册当前调用内的唯一程序集。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Ignore_Null_Entries_And_Register_Unique_Assemblies_In_Stable_Key_Order()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var assemblyC = CreateAssembly("GFramework.Cqrs.Tests.Sorting.C, Version=1.0.0.0");
        var assemblyA = CreateAssembly("GFramework.Cqrs.Tests.Sorting.A, Version=1.0.0.0");
        var duplicateAssemblyA = CreateAssembly("GFramework.Cqrs.Tests.Sorting.A, Version=1.0.0.0");
        var assemblyB = CreateAssembly("GFramework.Cqrs.Tests.Sorting.B, Version=1.0.0.0");
        var registeredAssemblies = new List<Assembly>();

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies.AddRange(assemblies));

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([assemblyC.Object, null!, assemblyA.Object, duplicateAssemblyA.Object, assemblyB.Object, null!]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Exactly(3));
        Assert.Multiple(() =>
        {
            Assert.That(
                registeredAssemblies,
                Is.EqualTo([assemblyA.Object, assemblyB.Object, assemblyC.Object]));
            Assert.That(logger.Logs, Has.Count.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证跨调用遇到已注册程序集键时，协调器会跳过重复项，同时继续按稳定程序集键顺序处理剩余新程序集。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Skip_Previously_Registered_Keys_And_Keep_Stable_Order_For_Remaining_Assemblies()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var firstAssembly = CreateAssembly("GFramework.Cqrs.Tests.Sorting.B, Version=1.0.0.0");
        var duplicateAssembly = CreateAssembly("GFramework.Cqrs.Tests.Sorting.B, Version=1.0.0.0");
        var assemblyC = CreateAssembly("GFramework.Cqrs.Tests.Sorting.C, Version=1.0.0.0");
        var assemblyA = CreateAssembly("GFramework.Cqrs.Tests.Sorting.A, Version=1.0.0.0");
        var registeredAssemblies = new List<Assembly>();

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies.AddRange(assemblies));

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([firstAssembly.Object]);
        service.RegisterHandlers([assemblyC.Object, duplicateAssembly.Object, assemblyA.Object]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Exactly(3));
        Assert.Multiple(() =>
        {
            Assert.That(
                registeredAssemblies,
                Is.EqualTo([firstAssembly.Object, assemblyA.Object, assemblyC.Object]));
            var debugMessages = logger.Logs
                .Where(static log => log.Level == LogLevel.Debug)
                .Select(static log => log.Message)
                .ToArray();
            Assert.That(debugMessages, Has.Length.EqualTo(1));
            Assert.That(
                debugMessages[0],
                Does.Contain("GFramework.Cqrs.Tests.Sorting.B, Version=1.0.0.0"));
        });
    }

    /// <summary>
    ///     验证当 <see cref="Assembly.FullName" /> 缺失时，协调器会退化到 <see cref="AssemblyName.Name" /> 作为稳定程序集键。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Fallback_To_Simple_Name_When_Full_Name_Is_Missing()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        var firstAssembly = CreateAssembly(
            assemblyFullName: null,
            assemblySimpleName: "GFramework.Cqrs.Tests.SimpleNameFallback",
            assemblyDisplayName: "DisplayName-A");
        var secondAssembly = CreateAssembly(
            assemblyFullName: null,
            assemblySimpleName: "GFramework.Cqrs.Tests.SimpleNameFallback",
            assemblyDisplayName: "DisplayName-B");
        IEnumerable<Assembly>? registeredAssemblies = null;

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = [.. assemblies]);

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([firstAssembly.Object]);
        service.RegisterHandlers([secondAssembly.Object]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(registeredAssemblies, Is.EqualTo([firstAssembly.Object]));
            var debugMessages = logger.Logs
                .Where(static log => log.Level == LogLevel.Debug)
                .Select(static log => log.Message)
                .ToArray();
            Assert.That(debugMessages, Has.Length.EqualTo(1));
            Assert.That(debugMessages[0], Does.Contain("GFramework.Cqrs.Tests.SimpleNameFallback"));
            Assert.That(debugMessages[0], Does.Not.Contain("DisplayName-B"));
        });
    }

    /// <summary>
    ///     验证当 <see cref="Assembly.FullName" /> 与 <see cref="AssemblyName.Name" /> 均缺失时，
    ///     协调器会退化到 <see cref="object.ToString" /> 结果作为稳定程序集键。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Fallback_To_ToString_When_Full_Name_And_Simple_Name_Are_Missing()
    {
        var logger = new TestLogger("DefaultCqrsRegistrationService", LogLevel.Debug);
        var registrar = new Mock<ICqrsHandlerRegistrar>(MockBehavior.Strict);
        const string assemblyDisplayName = "GFramework.Cqrs.Tests.ToStringFallback";
        var firstAssembly = CreateAssembly(
            assemblyFullName: null,
            assemblySimpleName: null,
            assemblyDisplayName: assemblyDisplayName);
        var secondAssembly = CreateAssembly(
            assemblyFullName: null,
            assemblySimpleName: null,
            assemblyDisplayName: assemblyDisplayName);
        IEnumerable<Assembly>? registeredAssemblies = null;

        registrar
            .Setup(static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()))
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = [.. assemblies]);

        var service = CqrsRuntimeFactory.CreateRegistrationService(registrar.Object, logger);

        service.RegisterHandlers([firstAssembly.Object]);
        service.RegisterHandlers([secondAssembly.Object]);

        registrar.Verify(
            static currentRegistrar => currentRegistrar.RegisterHandlers(It.IsAny<IEnumerable<Assembly>>()),
            Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(registeredAssemblies, Is.EqualTo([firstAssembly.Object]));
            var debugMessages = logger.Logs
                .Where(static log => log.Level == LogLevel.Debug)
                .Select(static log => log.Message)
                .ToArray();
            Assert.That(debugMessages, Has.Length.EqualTo(1));
            Assert.That(debugMessages[0], Does.Contain(assemblyDisplayName));
            Assert.That(debugMessages[0], Does.Contain("already registered"));
        });
    }

    /// <summary>
    ///     创建一个带稳定程序集键的程序集 mock，用于模拟不同 <see cref="Assembly" /> 实例表示同一程序集的场景。
    /// </summary>
    /// <param name="assemblyFullName">要返回的程序集完整名称。</param>
    /// <returns>配置好完整名称的程序集 mock。</returns>
    private static Mock<Assembly> CreateAssembly(string assemblyFullName)
    {
        return CreateAssembly(assemblyFullName, assemblySimpleName: null, assemblyDisplayName: assemblyFullName);
    }

    /// <summary>
    ///     创建一个可配置程序集元数据退化路径的程序集 mock，用于验证稳定程序集键的回退顺序。
    /// </summary>
    /// <param name="assemblyFullName">要返回的程序集完整名称；为 <see langword="null" /> 时模拟缺失完整名称。</param>
    /// <param name="assemblySimpleName">要返回的程序集简单名称；为 <see langword="null" /> 时模拟缺失简单名称。</param>
    /// <param name="assemblyDisplayName">当需要退化到 <see cref="object.ToString" /> 时返回的显示名称。</param>
    /// <returns>配置好程序集元数据的程序集 mock。</returns>
    private static Mock<Assembly> CreateAssembly(string? assemblyFullName, string? assemblySimpleName, string assemblyDisplayName)
    {
        var assembly = new Mock<Assembly>();
        var assemblyName = new AssemblyName();
        assembly
            .SetupGet(static currentAssembly => currentAssembly.FullName)
            .Returns(assemblyFullName);
        assemblyName.Name = assemblySimpleName;
        assembly
            .Setup(static currentAssembly => currentAssembly.GetName())
            .Returns(assemblyName);
        assembly
            .Setup(static currentAssembly => currentAssembly.ToString())
            .Returns(assemblyDisplayName);

        return assembly;
    }
}

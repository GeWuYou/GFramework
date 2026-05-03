// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Tests.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS 程序集注册协调器在程序集键去重层面的可观察行为。
/// </summary>
[TestFixture]
internal sealed class CqrsRegistrationServiceTests
{
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
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = assemblies.ToArray());

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
            .Callback<IEnumerable<Assembly>>(assemblies => registeredAssemblies = assemblies.ToArray());

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
    ///     创建一个带稳定程序集键的程序集 mock，用于模拟不同 <see cref="Assembly" /> 实例表示同一程序集的场景。
    /// </summary>
    /// <param name="assemblyFullName">要返回的程序集完整名称。</param>
    /// <returns>配置好完整名称的程序集 mock。</returns>
    private static Mock<Assembly> CreateAssembly(string assemblyFullName)
    {
        var assembly = new Mock<Assembly>();
        assembly
            .SetupGet(static currentAssembly => currentAssembly.FullName)
            .Returns(assemblyFullName);

        return assembly;
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;
using GFramework.Godot.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Godot.Tests.Architectures;

/// <summary>Checks module declarations without pretending to exercise the native scene tree.</summary>
[TestFixture]
public sealed class AbstractArchitectureModuleInstallationTests
{
    /// <summary>Only configuration describes services; startup attachment remains a separate host operation.</summary>
    [Test]
    public async Task ModuleDescriptionsDoNotTouchNativeNodes()
    {
        var module = new RecordingModule();
        await using var architecture = new ManagedArchitecture(module);
        await architecture.InitializeAsync();
        Assert.That(module.Configured, Is.True);
        Assert.That(module.NodeReads, Is.Zero);
    }

    /// <summary>Rejected late attachment cannot execute a module's configuration callback.</summary>
    [Test]
    public async Task LateModuleRegistrationFailsBeforeSideEffects()
    {
        var module = new RecordingModule();
        await using var architecture = new ManagedArchitecture(new RecordingModule());
        await architecture.InitializeAsync();
        Assert.Throws<InvalidOperationException>(() => architecture.LateInstall(module));
        Assert.That(module.Configured, Is.False);
    }

    /// <summary>A host fixture that excludes native attachment from this managed contract test.</summary>
    private sealed class ManagedArchitecture(RecordingModule module) : AbstractArchitecture
    {
        /// <inheritdoc />
        protected override void InstallModules(IServiceCollection services) => InstallGodotModule(services, module);
        /// <inheritdoc />
        protected override ValueTask OnStartedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        /// <summary>Attempts the production guard after the configuration window.</summary>
        public void LateInstall(RecordingModule other) => InstallGodotModule(new ServiceCollection(), other);
    }

    /// <summary>Records declaration without creating any native Godot objects.</summary>
    private sealed class RecordingModule : AbstractGodotModule
    {
        /// <summary>Whether descriptions were requested.</summary>
        public bool Configured
        {
            get; private set;
        }
        /// <summary>Whether native attachment attempted node access.</summary>
        public int NodeReads
        {
            get; private set;
        }
        /// <inheritdoc />
        public override global::Godot.Node Node
        {
            get
            {
                NodeReads++;
                throw new InvalidOperationException("Native node access is outside this test.");
            }
        }
        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services) => Configured = true;
    }
}

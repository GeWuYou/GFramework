// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Architectures;
using GFramework.Godot.UI;
using Godot;

namespace GodotIntegration;

public partial class DemoRoot
{
    /// <summary>Checks inherited startup hooks and native pause inheritance through the actual Godot host.</summary>
    private async Task VerifyHistoricalHostContractsAsync()
    {
        var architecture = new StartupHookArchitecture();
        await using (architecture.ConfigureAwait(true))
        {
            await architecture.InitializeAsync(cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
            Require(architecture.StartCount == 1 && architecture.HostReady && architecture.ModuleAttached,
                "Godot startup skipped the inherited synchronous hook or ran it before native attachment");
        }
        var mount = new Control { Name = "PausedUiMountProbe", ProcessMode = ProcessModeEnum.Always };
        var control = new Control();
        var view = new GodotUiView(control, new UiRouteOptions { ContinueProcessingWhenPaused = false });
        AddChild(mount);
        mount.AddChild(control);
        var wasPaused = GetTree().Paused;
        try
        {
            await view.OpenAsync(cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
            GetTree().Paused = true;
            Require(!control.CanProcess(), "A normal inherited UI view continued processing below an Always mount");
        }
        finally
        {
            GetTree().Paused = wasPaused;
            await view.DisposeAsync().ConfigureAwait(true);
            mount.QueueFree();
        }
        GD.Print("GFRAMEWORK_SMOKE_CHECKS history-startup-hook,ui-pause-policy: PASS");
    }

    /// <summary>Observes the exact ordering of native module attachment and the inherited startup extension point.</summary>
    private sealed class StartupHookArchitecture : AbstractArchitecture
    {
        private readonly StartupHookModule _module = new();
        private readonly int _hostThread = System.Environment.CurrentManagedThreadId;
        /// <summary>Number of synchronous startup hook calls.</summary>
        public int StartCount
        {
            get; private set;
        }
        /// <summary>Whether the hook observed its ready native root on the original Godot thread.</summary>
        public bool HostReady
        {
            get; private set;
        }
        /// <summary>Whether the hook saw module attachment already completed.</summary>
        public bool ModuleAttached
        {
            get; private set;
        }
        /// <inheritdoc />
        protected override void InstallModules() => InstallGodotModule(_module);
        /// <inheritdoc />
        protected override void OnStarted()
        {
            StartCount++;
            HostReady = ArchitectureRoot.IsInsideTree() && System.Environment.CurrentManagedThreadId == _hostThread;
            ModuleAttached = _module.Attached;
        }
    }

    /// <summary>Supplies a real native child whose attachment must precede inherited host setup.</summary>
    private sealed class StartupHookModule : IGodotModule
    {
        /// <inheritdoc />
        public Node Node { get; } = new();
        /// <summary>Whether the production OnAttach callback has run.</summary>
        public bool Attached
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Install(IArchitecture architecture)
        {
        }
        /// <inheritdoc />
        public void OnAttach(Architecture architecture) => Attached = true;
        /// <inheritdoc />
        public void OnDetach() => Attached = false;
    }
}

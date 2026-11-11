// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Runtime;
using GFramework.Game.UI;
using GFramework.Godot.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GodotIntegration.GettingStarted;

/// <summary>Composes ordinary services, explicitly ordered startup and the borrowed native host.</summary>
/// <remarks>All container access stays here. Native attachment follows resource validation; detachment follows UI stop.</remarks>
/// <param name="root">The borrowed scene and resource owner.</param>
/// <param name="dispatcher">The owning Godot thread.</param>
internal sealed class QuickStartArchitecture(QuickStartRoot root, IGameDispatcher dispatcher) : Architecture, IAsyncArchitectureHost
{
    /// <summary>Describes dependencies and startup participants before any service is activated.</summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(root);
        services.AddSingleton(dispatcher);
        services.AddComponent<QuickStartResources>(order: -100);
        services.AddSingleton(provider => CreateRoutes(provider.GetRequiredService<QuickStartResources>()));
        services.AddModule(new GameRuntimeModule(dispatcher, runtime =>
        {
            runtime.AddSingleton<IUiService>(provider => new UiService(provider.GetRequiredService<UiRegistry>(),
                root.UiRoot, dispatcher));
        }));
    }

    private UiRegistry CreateRoutes(QuickStartResources resources)
    {
        var routes = new UiRegistry();
        routes.RegisterGodotScene(QuickStartRoot.PageRoute, root.PageScene, (control, _, _) =>
            ((QuickStartPage)control).Configure(resources.Get("welcome"), root.RequestConfirmation));
        routes.RegisterGodotDialog(QuickStartRoot.ConfirmRoute, (question, context) =>
        {
            var node = root.ConfirmScene.Instantiate();
            try
            {
                var confirm = (QuickStartConfirm)node;
                confirm.Configure(question, context, root.RunResponse);
                return confirm;
            }
            catch { node.Free(); throw; }
        });
        return routes;
    }

    /// <inheritdoc />
    ValueTask IAsyncArchitectureHost.StartHostAsync(CancellationToken cancellationToken) =>
        new(dispatcher.InvokeAsync(() => root.AttachUiAsync(Services.GetRequiredService<IUiService>()), cancellationToken));

    /// <inheritdoc />
    protected override ValueTask OnStoppingAsync() => new(dispatcher.InvokeAsync(root.StopActionsAsync));

    /// <inheritdoc />
    protected override ValueTask OnStoppedAsync() => new(dispatcher.InvokeAsync(root.DetachUiAsync));
}

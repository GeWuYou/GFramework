// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Runtime;
using GFramework.Game.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Game.Tests.Runtime;

/// <summary>Exercises the same game service through standard DI and optional Architecture lifecycle integration.</summary>
[TestFixture]
public sealed class GameRuntimeAdoptionTests
{
    /// <summary>Typed arguments reach an ordinary view and provider disposal releases that view once.</summary>
    [Test]
    public async Task StandardDi_UsesTypedArguments_AndOwnsCleanup()
    {
        var route = new UiRoute<int>("inventory");
        var view = new AdoptionView();
        var received = 0;
        var routes = new UiRegistry().Register(route, (args, _, _) =>
        {
            received = args;
            return Task.FromResult<IUiView>(view);
        });
        var services = new ServiceCollection();
        services.AddGamePause().AddGameUi(routes, new AdoptionHost(), new InlineGameDispatcher());
        await using var provider = services.BuildServiceProvider();
        var ui = provider.GetRequiredService<IUiService>();
        await ui.PushAsync(route, 42).ConfigureAwait(false);

        await provider.DisposeAsync().ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.EqualTo(42));
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.DisposeCount, Is.EqualTo(1));
            Assert.That(ui.Snapshot.Views, Is.Empty);
        });
    }

    /// <summary>The module joins UI business cleanup while the native host is still available.</summary>
    [Test]
    public async Task Architecture_StopsUiBeforeHostDetachment()
    {
        var route = new UiRoute<string>("menu");
        var host = new AdoptionHost();
        var view = new AdoptionView { BeforeClose = () => Assert.That(host.Released, Is.False) };
        var routes = new UiRegistry().Register(route, (_, _, _) => Task.FromResult<IUiView>(view));
        var architecture = new AdoptionArchitecture(routes, host);
        await architecture.InitializeAsync().ConfigureAwait(false);
        var ui = architecture.Context.GetService<IUiService>();
        await ui.PushAsync(route, "Main menu").ConfigureAwait(false);

        await architecture.DestroyAsync().ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(host.Released, Is.True);
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.DisposeCount, Is.EqualTo(1));
        });
    }

    /// <summary>Independent containers have distinct game services and one container can stop independently.</summary>
    [Test]
    public async Task SeparateProviders_DoNotShareUiState()
    {
        var route = new UiRoute<int>("counter");
        var routes = new UiRegistry().Register(route, (_, _, _) => Task.FromResult<IUiView>(new AdoptionView()));
        await using var first = new ServiceCollection().AddGameUi(routes, new AdoptionHost(), new InlineGameDispatcher()).BuildServiceProvider();
        await using var second = new ServiceCollection().AddGameUi(routes, new AdoptionHost(), new InlineGameDispatcher()).BuildServiceProvider();
        var firstUi = first.GetRequiredService<IUiService>();
        var secondUi = second.GetRequiredService<IUiService>();
        await firstUi.PushAsync(route, 1).ConfigureAwait(false);
        await secondUi.PushAsync(route, 2).ConfigureAwait(false);

        await first.DisposeAsync().ConfigureAwait(false);

        Assert.That(firstUi.Snapshot.Views, Is.Empty);
        Assert.That(secondUi.Snapshot.Views, Has.Count.EqualTo(1));
    }

    /// <summary>Supplies a host boundary without referencing any native engine assembly.</summary>
    private sealed class AdoptionHost : IUiHost
    {
        /// <summary>Gets or sets whether host detachment has happened.</summary>
        public bool Released
        {
            get; set;
        }
        /// <inheritdoc />
        public void Attach(IUiView view, int layer, long order) => Assert.That(Released, Is.False);
        /// <inheritdoc />
        public void Detach(IUiView view) => Assert.That(Released, Is.False);
        /// <inheritdoc />
        public object? CaptureFocus() => null;
        /// <inheritdoc />
        public void Focus(IUiView view)
        {
        }
        /// <inheritdoc />
        public void RestoreFocus(object? bookmark)
        {
        }
    }

    /// <summary>Records business and resource cleanup separately.</summary>
    private sealed class AdoptionView : IUiView
    {
        /// <summary>Gets the close callback count.</summary>
        public int CloseCount
        {
            get; private set;
        }
        /// <summary>Gets the native lease release count.</summary>
        public int DisposeCount
        {
            get; private set;
        }
        /// <summary>Gets the assertion run while the host must still be available.</summary>
        public Action? BeforeClose
        {
            get; init;
        }
        /// <inheritdoc />
        public bool IsAlive => DisposeCount == 0;
        /// <inheritdoc />
        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            BeforeClose?.Invoke();
            CloseCount++;
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public void SetVisible(bool visible)
        {
        }
        /// <inheritdoc />
        public void SetActive(bool active)
        {
        }
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Uses only the service module to integrate ordinary services into Architecture.</summary>
    private sealed class AdoptionArchitecture(UiRegistry routes, AdoptionHost host) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services) =>
            RegisterServiceModule(new GameRuntimeModule(new InlineGameDispatcher(), collection =>
                collection.AddGamePause().AddGameUi(routes, host, new InlineGameDispatcher())));
        /// <inheritdoc />
        protected override ValueTask OnStoppedAsync()
        {
            host.Released = true;
            return ValueTask.CompletedTask;
        }
    }
}

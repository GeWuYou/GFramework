// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Game.Runtime;

namespace GFramework.Game.Tests.UI;

/// <summary>Checks managed host-context propagation through real UI operations and asynchronous business callbacks.</summary>
[TestFixture]
public sealed class UiServiceDispatcherTests
{
    /// <summary>Factories, opening, presentation and cleanup always return to the explicitly configured host context.</summary>
    [Test]
    public async Task BackgroundCaller_ExecutesEveryViewPhaseOnHostContext()
    {
        var hostContext = new MarkerSynchronizationContext();
        var route = new UiRoute<string>("page");
        var view = CreateView(hostContext);
        var registry = new UiRegistry().Register(route, (_, _, _) =>
        {
            AssertHostContext(hostContext);
            return Task.FromResult(view.Object);
        });
        var host = CreateHost(hostContext);
        var service = new UiService(registry, host.Object, new SynchronizationContextGameDispatcher(hostContext));

        await Task.Run(() => service.PushAsync(route, "args")).WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Run(service.StopAsync).WaitAsync(TimeSpan.FromSeconds(5));

        view.Verify(item => item.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        view.Verify(item => item.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
        view.Verify(item => item.DisposeAsync(), Times.Once);
        host.Verify(item => item.Detach(view.Object), Times.Once);
    }

    /// <summary>A dialog's action may leave the host context while awaiting business work and still close safely.</summary>
    [Test]
    public async Task DialogAction_AfterContextFreeAwait_DispatchesOwnCompletionBackToHost()
    {
        var hostContext = new MarkerSynchronizationContext();
        var route = new UiDialogRoute<string, int>("confirm", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            Modal = true,
            CapturedActions = UiInputActionMask.Confirm,
        });
        var ready = NewCompletion();
        var entered = NewCompletion();
        var release = NewCompletion();
        var view = CreateView(hostContext);
        var action = view.As<IUiActionTarget>();
        var registry = new UiRegistry().Register(route, (_, context, _) =>
        {
            action.Setup(item => item.HandleActionAsync(UiInputAction.Confirm, It.IsAny<CancellationToken>()))
                .Returns(async (UiInputAction _, CancellationToken token) =>
                {
                    AssertHostContext(hostContext);
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.That(SynchronizationContext.Current, Is.Not.SameAs(hostContext));
                    await context.CompleteAsync(9, token).ConfigureAwait(false);
                    return true;
                });
            ready.SetResult();
            return Task.FromResult(view.Object);
        });
        await using var service = new UiService(registry, CreateHost(hostContext).Object,
            new SynchronizationContextGameDispatcher(hostContext));
        var result = service.ShowDialogAsync(route, "question");
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var dispatch = service.DispatchActionAsync(UiInputAction.Confirm);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.SetResult();

        Assert.That(await dispatch.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That((await result.WaitAsync(TimeSpan.FromSeconds(5))).Value, Is.EqualTo(9));
        view.Verify(item => item.DisposeAsync(), Times.Once);
    }

    /// <summary>Creates a view whose native-facing methods assert their dispatcher context.</summary>
    private static Mock<IUiView> CreateView(SynchronizationContext expected)
    {
        var view = new Mock<IUiView>();
        view.SetupGet(item => item.IsAlive).Returns(() =>
        {
            AssertHostContext(expected);
            return true;
        });
        view.Setup(item => item.OpenAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            AssertHostContext(expected);
            await Task.Yield();
            AssertHostContext(expected);
        });
        view.Setup(item => item.CloseAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            AssertHostContext(expected);
            return Task.CompletedTask;
        });
        view.Setup(item => item.SetVisible(It.IsAny<bool>())).Callback(() => AssertHostContext(expected));
        view.Setup(item => item.SetActive(It.IsAny<bool>())).Callback(() => AssertHostContext(expected));
        view.Setup(item => item.DisposeAsync()).Returns(() =>
        {
            AssertHostContext(expected);
            return ValueTask.CompletedTask;
        });
        return view;
    }

    /// <summary>Creates a native-host substitute that verifies mounting, ordering and focus operations.</summary>
    private static Mock<IUiHost> CreateHost(SynchronizationContext expected)
    {
        var host = new Mock<IUiHost>();
        host.Setup(item => item.Attach(It.IsAny<IUiView>(), It.IsAny<int>(), It.IsAny<long>()))
            .Callback(() => AssertHostContext(expected));
        host.Setup(item => item.Detach(It.IsAny<IUiView>())).Callback(() => AssertHostContext(expected));
        host.Setup(item => item.CaptureFocus()).Returns(() =>
        {
            AssertHostContext(expected);
            return null;
        });
        host.Setup(item => item.Focus(It.IsAny<IUiView>())).Callback(() => AssertHostContext(expected));
        host.Setup(item => item.RestoreFocus(It.IsAny<object?>())).Callback(() => AssertHostContext(expected));
        return host;
    }

    /// <summary>Checks context identity without assuming a specific physical thread in this managed test host.</summary>
    private static void AssertHostContext(SynchronizationContext expected) =>
        Assert.That(SynchronizationContext.Current, Is.SameAs(expected));

    /// <summary>Creates a rendezvous that cannot resume its waiter inline.</summary>
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Restores a host context while executing posted managed callbacks.</summary>
    private sealed class MarkerSynchronizationContext : SynchronizationContext
    {
        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var previous = Current;
                try
                {
                    SetSynchronizationContext(this);
                    callback(state);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            });
        }
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Pause;
using GFramework.Game.Runtime;

namespace GFramework.Game.Tests.UI;

/// <summary>Exercises typed UI adoption through observable host, lifecycle and result behavior.</summary>
[TestFixture]
public sealed class UiServiceTests
{
    /// <summary>Navigation hides and later activates an existing page without reopening it.</summary>
    [Test]
    public async Task PushAndBack_KeepPageHistoryAndRestoreInteraction()
    {
        var menu = new UiRoute<string>("menu");
        var settings = new UiRoute<int>("settings");
        var first = new TestView();
        var second = new TestView();
        var registry = new UiRegistry()
            .Register(menu, (args, _, _) =>
            {
                Assert.That(args, Is.EqualTo("hello"));
                return Task.FromResult<IUiView>(first);
            })
            .Register(settings, (_, _, _) => Task.FromResult<IUiView>(second));
        await using var service = CreateService(registry);

        await service.PushAsync(menu, "hello");
        await service.PushAsync(settings, 2);
        Assert.Multiple(() =>
        {
            Assert.That(first.Visible, Is.False);
            Assert.That(first.Active, Is.False);
            Assert.That(second.Active, Is.True);
        });
        await service.BackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.Visible, Is.True);
            Assert.That(first.Active, Is.True);
            Assert.That(first.OpenCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(1));
            Assert.That(service.Snapshot.Views.Select(static view => view.RouteKey), Is.EqualTo(["menu"]));
        });
    }

    /// <summary>The same typed page route can represent different navigation arguments in separate history entries.</summary>
    [Test]
    public async Task Push_SamePageWithDifferentArguments_CreatesHistoryEntries()
    {
        var route = new UiRoute<int>("item-details");
        var seen = new List<int>();
        var registry = new UiRegistry().Register(route, (args, _, _) =>
        {
            seen.Add(args);
            return Task.FromResult<IUiView>(new TestView());
        });
        await using var service = CreateService(registry);

        var first = await service.PushAsync(route, 1);
        var second = await service.PushAsync(route, 2);
        Assert.That(service.Snapshot.Views, Has.Count.EqualTo(2));
        await service.BackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EqualTo([1, 2]));
            Assert.That(first.Handle, Is.Not.EqualTo(second.Handle));
            Assert.That(service.Snapshot.Views.Single().Handle, Is.EqualTo(first.Handle));
            Assert.That(service.Snapshot.Views.Single().Active, Is.True);
        });
    }

    /// <summary>Window operations cannot overtake an asynchronous page preparation.</summary>
    [Test]
    public async Task WindowDuringPageOpen_WaitsForTheSameMutationQueue()
    {
        var page = new UiRoute<string>("page");
        var window = Window("window");
        var entered = NewCompletion();
        var release = NewCompletion();
        var windowCreated = false;
        var registry = new UiRegistry()
            .Register(page, (_, _, _) => Task.FromResult<IUiView>(new TestView
            {
                OpenAction = async _ => { entered.SetResult(); await release.Task.ConfigureAwait(false); },
            }))
            .Register(window, (_, _, _) =>
            {
                windowCreated = true;
                return Task.FromResult<IUiView>(new TestView());
            });
        await using var service = CreateService(registry);

        var first = service.PushAsync(page, "");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.ShowAsync(window, "");
        Assert.That(second.IsCompleted, Is.False);
        Assert.That(windowCreated, Is.False);
        release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(service.Snapshot.Views.Select(static view => view.RouteKey), Is.EqualTo(["page", "window"]));
    }

    /// <summary>Window singleton behavior returns the same identity and never invokes its factory twice.</summary>
    [Test]
    public async Task Show_SingleInstance_ReturnsExistingHandle()
    {
        var route = Window("inventory");
        var created = 0;
        var registry = new UiRegistry().Register(route, (_, _, _) =>
        {
            created++;
            return Task.FromResult<IUiView>(new TestView());
        });
        await using var service = CreateService(registry);

        var first = await service.ShowAsync(route, "one");
        var second = await service.ShowAsync(route, "two");

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.EqualTo(1));
            Assert.That(first.Status, Is.EqualTo(UiOperationStatus.Applied));
            Assert.That(second.Status, Is.EqualTo(UiOperationStatus.NoOp));
            Assert.That(second.Handle, Is.EqualTo(first.Handle));
        });
    }

    /// <summary>Reusing a singleton reorders the mounted view without repeating its opening lifecycle.</summary>
    [Test]
    public async Task Show_ExistingSingleton_ReordersHostWithoutReopening()
    {
        var firstRoute = Window("first");
        var secondRoute = Window("second");
        var first = new TestView();
        var second = new TestView();
        var host = new TestHost();
        var registry = new UiRegistry()
            .Register(firstRoute, (_, _, _) => Task.FromResult<IUiView>(first))
            .Register(secondRoute, (_, _, _) => Task.FromResult<IUiView>(second));
        await using var service = CreateService(registry, host);
        await service.ShowAsync(firstRoute, "initial");
        await service.ShowAsync(secondRoute, "other");

        await service.ShowAsync(firstRoute, "reuse");

        Assert.Multiple(() =>
        {
            Assert.That(host.Mounted, Is.EqualTo(new IUiView[] { second, first }));
            Assert.That(service.Snapshot.Views.Select(static view => view.RouteKey), Is.EqualTo(["second", "first"]));
            Assert.That(first.OpenCount, Is.EqualTo(1));
            Assert.That(first.CloseCount, Is.Zero);
            Assert.That(first.DisposeCount, Is.Zero);
        });
    }

    /// <summary>Input controls created during native readiness remain disabled beneath a higher modal.</summary>
    [Test]
    public async Task Open_BeneathModal_DisablesInputCreatedDuringOpening()
    {
        var top = new UiRoute<string>("modal", new UiRouteOptions { Kind = UiRouteKind.Window, Layer = 20, Modal = true });
        var lower = new UiRoute<string>("lower", new UiRouteOptions { Kind = UiRouteKind.Window, Layer = 5 });
        var lowerView = new TestView { CreatesInputOnOpen = true };
        var registry = new UiRegistry()
            .Register(top, (_, _, _) => Task.FromResult<IUiView>(new TestView()))
            .Register(lower, (_, _, _) => Task.FromResult<IUiView>(lowerView));
        await using var service = CreateService(registry);
        await service.ShowAsync(top, "");

        await service.ShowAsync(lower, "");

        Assert.Multiple(() =>
        {
            Assert.That(lowerView.Visible, Is.True);
            Assert.That(lowerView.Active, Is.False);
            Assert.That(lowerView.ActivationCount, Is.Zero);
            Assert.That(lowerView.DeactivationCount, Is.EqualTo(1));
        });
    }

    /// <summary>Explicit multi-instance registration creates independent leases and closes only the selected one.</summary>
    [Test]
    public async Task Show_MultipleInstances_HaveIndependentIdentity()
    {
        var route = new UiRoute<string>("toast", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            AllowMultiple = true,
            CapturedActions = UiInputActionMask.None,
        });
        var registry = new UiRegistry().Register(route, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = CreateService(registry);

        var first = await service.ShowAsync(route, "one");
        var second = await service.ShowAsync(route, "two");
        await service.CloseAsync(first.Handle!.Value);

        Assert.Multiple(() =>
        {
            Assert.That(first.Handle, Is.Not.EqualTo(second.Handle));
            Assert.That(service.Snapshot.Views.Single().Handle, Is.EqualTo(second.Handle));
        });
    }

    /// <summary>Back ignores a passive HUD and still returns from the current page.</summary>
    [Test]
    public async Task Back_SkipsNonBackableHud()
    {
        var page = new UiRoute<string>("page");
        var hud = new UiRoute<string>("hud", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            Layer = 100,
            AllowBack = false,
            CapturedActions = UiInputActionMask.None,
        });
        var registry = new UiRegistry()
            .Register(page, (_, _, _) => Task.FromResult<IUiView>(new TestView()))
            .Register(hud, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = CreateService(registry);
        await service.PushAsync(page, "");
        await service.ShowAsync(hud, "");

        await service.BackAsync();

        Assert.That(service.Snapshot.Views.Select(static view => view.RouteKey), Is.EqualTo(["hud"]));
    }

    /// <summary>A non-backable modal consumes back without closing views behind it.</summary>
    [Test]
    public async Task Back_NonBackableModal_IsBarrier()
    {
        var page = new UiRoute<string>("page");
        var modal = new UiRoute<string>("busy", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            Layer = 10,
            Modal = true,
            AllowBack = false,
        });
        var registry = new UiRegistry()
            .Register(page, (_, _, _) => Task.FromResult<IUiView>(new TestView()))
            .Register(modal, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = CreateService(registry);
        await service.PushAsync(page, "");
        await service.ShowAsync(modal, "");

        var result = await service.BackAsync();

        Assert.That(result.Status, Is.EqualTo(UiOperationStatus.NoOp));
        Assert.That(service.Snapshot.Views, Has.Count.EqualTo(2));
    }

    /// <summary>Dialog result waiting leaves the queue free for another window and completes after release.</summary>
    [Test]
    public async Task Dialog_WaitsOutsideQueue_AndReturnsTypedResultAfterCleanup()
    {
        var route = new UiDialogRoute<string, int>("confirm");
        var hud = Window("hud");
        var ready = NewCompletion();
        UiDialogContext<int>? context = null;
        var dialogView = new TestView();
        var registry = new UiRegistry()
            .Register(route, (_, viewContext, _) =>
            {
                context = viewContext;
                ready.SetResult();
                return Task.FromResult<IUiView>(dialogView);
            })
            .Register(hud, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = CreateService(registry);

        var resultTask = service.ShowDialogAsync(route, "question");
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.ShowAsync(hud, "status").WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(resultTask.IsCompleted, Is.False);
        await context!.CompleteAsync(42);
        var result = await resultTask.WaitAsync(TimeSpan.FromSeconds(5));
        var repeated = await context.CompleteAsync(99);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(dialogView.DisposeCount, Is.EqualTo(1));
            Assert.That(repeated.Status, Is.EqualTo(UiOperationStatus.NoOp));
        });
    }

    /// <summary>Caller cancellation reclaims the dialog before publishing cancellation to its result waiter.</summary>
    [Test]
    public async Task Dialog_Cancellation_ClosesAndCancelsAfterCleanup()
    {
        var route = new UiDialogRoute<string, int>("confirm");
        var hud = Window("hud");
        var ready = NewCompletion();
        var view = new TestView();
        var registry = new UiRegistry()
            .Register(route, (_, _, _) =>
            {
                ready.SetResult();
                return Task.FromResult<IUiView>(view);
            })
            .Register(hud, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = CreateService(registry);
        using var cancellation = new CancellationTokenSource();

        var result = service.ShowDialogAsync(route, "question", cancellation.Token);
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await service.ShowAsync(hud, "status", cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => result.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(view.DisposeCount, Is.EqualTo(1));
            Assert.That(service.Snapshot.Views.Any(static entry => string.Equals(entry.RouteKey, "confirm", StringComparison.Ordinal)), Is.False);
        });
    }

    /// <summary>A failed opening leaves the old page usable and releases the partially mounted target once.</summary>
    [Test]
    public async Task Replace_OpenFailure_PreservesPreviousPage()
    {
        var original = new UiRoute<string>("original");
        var failing = new UiRoute<string>("failing");
        var previous = new TestView();
        var target = new TestView { OpenAction = _ => throw new InvalidOperationException("open failed") };
        var host = new TestHost();
        var registry = new UiRegistry()
            .Register(original, (_, _, _) => Task.FromResult<IUiView>(previous))
            .Register(failing, (_, _, _) => Task.FromResult<IUiView>(target));
        await using var service = CreateService(registry, host);
        await service.PushAsync(original, "old");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplaceAsync(failing, "new"));

        Assert.Multiple(() =>
        {
            Assert.That(previous.Active, Is.True);
            Assert.That(previous.Visible, Is.True);
            Assert.That(previous.DisposeCount, Is.Zero);
            Assert.That(target.CloseCount, Is.EqualTo(1));
            Assert.That(target.DisposeCount, Is.EqualTo(1));
            Assert.That(host.Mounted, Is.EqualTo([previous]));
        });
    }

    /// <summary>Cancellation during preparation preserves old state and retains standard cancellation semantics.</summary>
    [Test]
    public async Task Push_CancelledOpening_ReclaimsTargetAndPreservesPage()
    {
        var original = new UiRoute<string>("original");
        var pending = new UiRoute<string>("pending");
        var entered = NewCompletion();
        var target = new TestView
        {
            OpenAction = async token =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            },
        };
        var registry = new UiRegistry()
            .Register(original, (_, _, _) => Task.FromResult<IUiView>(new TestView()))
            .Register(pending, (_, _, _) => Task.FromResult<IUiView>(target));
        await using var service = CreateService(registry);
        await service.PushAsync(original, "old");
        using var cancellation = new CancellationTokenSource();

        var operation = service.PushAsync(pending, "new", cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: global::System.Threading.CancellationToken.None);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => operation);

        Assert.Multiple(() =>
        {
            Assert.That(target.DisposeCount, Is.EqualTo(1));
            Assert.That(service.Snapshot.Views.Single().RouteKey, Is.EqualTo("original"));
            Assert.That(service.Snapshot.Views.Single().Active, Is.True);
        });
    }

    /// <summary>Guard refusal is a normal rejected outcome and cannot create an instance.</summary>
    [Test]
    public async Task GuardRefusal_LeavesStateUnchanged()
    {
        var route = Window("guarded");
        var factory = 0;
        var registry = new UiRegistry().Register(route, (_, _, _) =>
        {
            factory++;
            return Task.FromResult<IUiView>(new TestView());
        });
        var guard = new TestGuard { CanOpen = false };
        await using var service = new UiService(registry, new TestHost(), new InlineGameDispatcher(), guards: [guard]);

        var result = await service.ShowAsync(route, "args");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(UiOperationStatus.Rejected));
            Assert.That(factory, Is.Zero);
            Assert.That(service.Snapshot.Views, Is.Empty);
        });
    }

    /// <summary>Clear evaluates every guard before removing any instance.</summary>
    [Test]
    public async Task Clear_WhenOneCloseGuardRefuses_PreservesAllViews()
    {
        var first = Window("first");
        var second = Window("second");
        var registry = new UiRegistry()
            .Register(first, (_, _, _) => Task.FromResult<IUiView>(new TestView()))
            .Register(second, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        var guard = new TestGuard { RefusedCloseKey = second.Key };
        await using var service = new UiService(registry, new TestHost(), new InlineGameDispatcher(), guards: [guard]);
        await service.ShowAsync(first, "");
        await service.ShowAsync(second, "");

        var result = await service.ClearAsync();

        Assert.That(result.Status, Is.EqualTo(UiOperationStatus.Rejected));
        Assert.That(service.Snapshot.Views, Has.Count.EqualTo(2));
    }

    /// <summary>Nested pause owners retain gameplay pause until their individual views are closed.</summary>
    [Test]
    public async Task NestedModals_OwnIndependentPauseRequests()
    {
        var pause = new PauseStackManager();
        var first = new UiRoute<string>("pause", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            Layer = 10,
            Modal = true,
            PauseWhileVisible = true,
            BlocksWorldActionInput = true,
            BlocksWorldPointerInput = true,
        });
        var second = new UiRoute<string>("settings", first.Options);
        var firstView = new TestView();
        var registry = new UiRegistry()
            .Register(first, (_, _, _) => Task.FromResult<IUiView>(firstView))
            .Register(second, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var service = new UiService(registry, new TestHost(), new InlineGameDispatcher(), pause);

        await service.ShowAsync(first, "");
        await service.ShowAsync(second, "");
        Assert.That(pause.GetPauseDepth(), Is.EqualTo(2));
        Assert.That(firstView.Active, Is.False);
        await service.BackAsync();
        Assert.That(pause.GetPauseDepth(), Is.EqualTo(1));
        Assert.That(firstView.Active, Is.True);
        Assert.That(service.Snapshot.BlocksWorldActionInput, Is.True);
        await service.BackAsync();
        Assert.That(pause.GetPauseDepth(), Is.Zero);
    }

    /// <summary>A view can complete its own dialog from an action callback without waiting behind itself.</summary>
    [Test]
    public async Task ConfirmAction_CanCompleteOwnDialog()
    {
        var route = new UiDialogRoute<string, int>("confirm", new UiRouteOptions
        {
            Kind = UiRouteKind.Window,
            Modal = true,
            CapturedActions = UiInputActionMask.Confirm | UiInputActionMask.Cancel,
        });
        var ready = NewCompletion();
        var registry = new UiRegistry().Register(route, (_, context, _) =>
        {
            ready.SetResult();
            return Task.FromResult<IUiView>(new TestView
            {
                Action = async (_, token) =>
                {
                    await context.CompleteAsync(7, token).ConfigureAwait(false);
                    return true;
                },
            });
        });
        await using var service = CreateService(registry);
        var result = service.ShowDialogAsync(route, "");
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(await service.DispatchActionAsync(UiInputAction.Confirm), Is.True);
        Assert.That((await result.WaitAsync(TimeSpan.FromSeconds(5))).Value, Is.EqualTo(7));
    }

    /// <summary>Lifecycle navigation reports a precise error instead of deadlocking the operation chain.</summary>
    [Test]
    public async Task LifecycleNavigation_RejectsReentrantAwait()
    {
        var outer = Window("outer");
        var nested = Window("nested");
        UiService? service = null;
        var registry = new UiRegistry()
            .Register(outer, (_, _, _) => Task.FromResult<IUiView>(new TestView
            {
                OpenAction = async token => await service!.ShowAsync(nested, "", token).ConfigureAwait(false),
            }))
            .Register(nested, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var owned = service = CreateService(registry);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShowAsync(outer, ""));
        Assert.That(service.Snapshot.Views, Is.Empty);
    }

    /// <summary>A handle from another service is rejected even if both services have instance 1.</summary>
    [Test]
    public async Task Close_RejectsCrossHostHandle()
    {
        var route = Window("window");
        var registry = new UiRegistry().Register(route, (_, _, _) => Task.FromResult<IUiView>(new TestView()));
        await using var first = CreateService(registry);
        await using var second = CreateService(registry);
        var opened = await first.ShowAsync(route, "");

        Assert.Throws<ArgumentException>(() => { _ = second.CloseAsync(opened.Handle!.Value); });
        Assert.That(first.Snapshot.Views, Has.Count.EqualTo(1));
    }

    /// <summary>Shutdown joins an accepted opening and closes its view once; later operations are refused.</summary>
    [Test]
    public async Task Stop_JoinsAcceptedWork_AndSharesCleanup()
    {
        var route = Window("window");
        var entered = NewCompletion();
        var release = NewCompletion();
        var view = new TestView { OpenAction = async _ => { entered.SetResult(); await release.Task.ConfigureAwait(false); } };
        var registry = new UiRegistry().Register(route, (_, _, _) => Task.FromResult<IUiView>(view));
        var service = CreateService(registry);

        var opening = service.ShowAsync(route, "");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = service.StopAsync();
        Assert.That(stop.IsCompleted, Is.False);
        Assert.That(service.Snapshot.IsStopping, Is.True);
        Assert.Throws<ObjectDisposedException>(() => { _ = service.ShowAsync(route, ""); });
        release.SetResult();
        await Task.WhenAll(opening, stop).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(service.StopAsync(), Is.SameAs(stop));
        Assert.That(view.DisposeCount, Is.EqualTo(1));
        Assert.That(service.Snapshot.Views, Is.Empty);
    }

    /// <summary>One cleanup failure cannot prevent detachment and disposal of any other instance.</summary>
    [Test]
    public async Task Stop_CleanupFailure_StillReclaimsEveryLease()
    {
        var route = new UiRoute<string>("window", new UiRouteOptions { Kind = UiRouteKind.Window, AllowMultiple = true });
        var views = new List<TestView>();
        var host = new TestHost();
        var registry = new UiRegistry().Register(route, (_, _, _) =>
        {
            var view = new TestView { CloseAction = _ => throw new InvalidOperationException("close failed") };
            views.Add(view);
            return Task.FromResult<IUiView>(view);
        });
        var service = CreateService(registry, host);
        await service.ShowAsync(route, "one");
        await service.ShowAsync(route, "two");

        await Assert.ThrowsAsync<AggregateException>(() => service.StopAsync());

        Assert.Multiple(() =>
        {
            Assert.That(views.TrueForAll(static view => view.DisposeCount == 1), Is.True);
            Assert.That(host.Mounted, Is.Empty);
            Assert.That(service.Snapshot.Views, Is.Empty);
        });
    }

    /// <summary>Creates a common single-instance window route.</summary>
    private static UiRoute<string> Window(string key) => new(key, new UiRouteOptions { Kind = UiRouteKind.Window });
    /// <summary>Creates an engine-independent host with explicit dispatcher ownership.</summary>
    private static UiService CreateService(UiRegistry registry, TestHost? host = null) =>
        new(registry, host ?? new TestHost(), new InlineGameDispatcher());
    /// <summary>Creates a bounded-test rendezvous without running continuations inline.</summary>
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Records the observable lease lifecycle and optional asynchronous work.</summary>
    private sealed class TestView : IUiView, IUiActionTarget
    {
        /// <summary>Gets optional opening work.</summary>
        public Func<CancellationToken, Task>? OpenAction
        {
            get; init;
        }
        /// <summary>Gets optional closing work.</summary>
        public Func<CancellationToken, Task>? CloseAction
        {
            get; init;
        }
        /// <summary>Gets optional semantic action work.</summary>
        public Func<UiInputAction, CancellationToken, Task<bool>>? Action
        {
            get; init;
        }
        /// <summary>Gets whether opening simulates native children with authored input enabled.</summary>
        public bool CreatesInputOnOpen
        {
            get; init;
        }
        /// <summary>Gets whether the view remains alive.</summary>
        public bool IsAlive => DisposeCount == 0;
        /// <summary>Gets the opening count.</summary>
        public int OpenCount
        {
            get; private set;
        }
        /// <summary>Gets the closing count.</summary>
        public int CloseCount
        {
            get; private set;
        }
        /// <summary>Gets the disposal count.</summary>
        public int DisposeCount
        {
            get; private set;
        }
        /// <summary>Gets current native visibility.</summary>
        public bool Visible
        {
            get; private set;
        }
        /// <summary>Gets current native interaction state.</summary>
        public bool Active
        {
            get; private set;
        }
        /// <summary>Gets explicit activation notifications.</summary>
        public int ActivationCount
        {
            get; private set;
        }
        /// <summary>Gets explicit deactivation notifications.</summary>
        public int DeactivationCount
        {
            get; private set;
        }
        /// <inheritdoc />
        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            OpenCount++;
            if (CreatesInputOnOpen)
                Active = true;
            return OpenAction?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }
        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            CloseCount++;
            return CloseAction?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }
        /// <inheritdoc />
        public void SetVisible(bool visible) => Visible = visible;
        /// <inheritdoc />
        public void SetActive(bool active)
        {
            Active = active;
            if (active)
                ActivationCount++;
            else
                DeactivationCount++;
        }
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
        /// <inheritdoc />
        public Task<bool> HandleActionAsync(UiInputAction action, CancellationToken cancellationToken = default) =>
            Action?.Invoke(action, cancellationToken) ?? Task.FromResult(false);
    }

    /// <summary>Records mounting and allows service logic to be tested without native engine bindings.</summary>
    private sealed class TestHost : IUiHost
    {
        /// <summary>Gets current mounted view leases.</summary>
        public List<IUiView> Mounted { get; } = [];
        /// <inheritdoc />
        public void Attach(IUiView view, int layer, long order)
        {
            Mounted.Remove(view);
            Mounted.Add(view);
        }
        /// <inheritdoc />
        public void Detach(IUiView view) => Mounted.Remove(view);
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

    /// <summary>Exposes explicit guard outcomes without introducing a dependency on Architecture.</summary>
    private sealed class TestGuard : IUiGuard
    {
        /// <summary>Gets the opening decision.</summary>
        public bool CanOpen { get; init; } = true;
        /// <summary>Gets an optional route refused during close.</summary>
        public string? RefusedCloseKey
        {
            get; init;
        }
        /// <inheritdoc />
        public Task<bool> CanOpenAsync(string routeKey, CancellationToken cancellationToken = default) => Task.FromResult(CanOpen);
        /// <inheritdoc />
        public Task<bool> CanCloseAsync(UiViewSnapshot view, CancellationToken cancellationToken = default) =>
            Task.FromResult(!string.Equals(view.RouteKey, RefusedCloseKey, StringComparison.Ordinal));
    }
}

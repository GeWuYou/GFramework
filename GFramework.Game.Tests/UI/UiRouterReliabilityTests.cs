// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.Tests.UI;

/// <summary>Exercises navigation races and cleanup through public UI router operations.</summary>
[TestFixture]
public sealed class UiRouterReliabilityTests
{
    /// <summary>A second click rechecks the stack after the first navigation completes.</summary>
    [Test]
    public async Task ConcurrentPush_SameKey_CreatesOnePage()
    {
        var root = new Mock<IUiRoot>();
        var router = CreateRouter(root);
        var entered = NewCompletion();
        var release = NewCompletion();
        BlockBeforeChange(router, entered, release);
        var page = CreatePage("menu");

        var first = router.PushAsync(page.Object).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = router.PushAsync(CreatePage("menu").Object).AsTask();
        Assert.That(second.IsCompleted, Is.False);
        release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(router.Count, Is.EqualTo(1));
        root.Verify(host => host.AddUiPage(It.IsAny<IUiPageBehavior>()), Times.Once);
        await router.DestroyAsync();
    }

    /// <summary>Shutdown waits for navigation and releases stack, hidden and floating pages once.</summary>
    [Test]
    public async Task DestroyAsync_WaitsForNavigation_AndCleansAllOwnedPages()
    {
        var root = new Mock<IUiRoot>();
        var router = CreateRouter(root);
        var hidden = CreatePage("hidden");
        await router.PushAsync(hidden.Object);
        await router.PopAsync(UiPopPolicy.Suspend);
        var floating = CreatePage("modal");
        router.Show(floating.Object, UiLayer.Modal);

        var entered = NewCompletion();
        var release = NewCompletion();
        BlockBeforeChange(router, entered, release);
        var page = CreatePage("game");
        var navigation = router.PushAsync(page.Object).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = router.DestroyAsync().AsTask();
        Assert.That(stop.IsCompleted, Is.False);
        release.SetResult();
        await Task.WhenAll(navigation, stop).WaitAsync(TimeSpan.FromSeconds(5));
        await router.DestroyAsync();

        foreach (var owned in new[] { hidden, floating, page })
        {
            owned.Verify(item => item.OnExit(), Times.Once);
            root.Verify(host => host.RemoveUiPage(owned.Object), Times.Once);
        }
        Assert.Throws<ObjectDisposedException>(() => router.Show(CreatePage("late").Object, UiLayer.Modal));
    }

    /// <summary>A failing lifecycle callback cannot prevent other pages or root nodes from being cleaned.</summary>
    [Test]
    public async Task DestroyAsync_WhenExitThrows_StillRemovesAllPages()
    {
        var root = new Mock<IUiRoot>();
        var router = CreateRouter(root);
        var first = CreatePage("first");
        var second = CreatePage("second");
        first.Setup(page => page.OnExit()).Throws(new InvalidOperationException("exit failed"));
        await router.PushAsync(first.Object);
        await router.PushAsync(second.Object);

        await Assert.ThrowsAsync<AggregateException>(() => router.DestroyAsync().AsTask());
        await Assert.ThrowsAsync<AggregateException>(() => router.DestroyAsync().AsTask());
        root.Verify(host => host.RemoveUiPage(first.Object), Times.Once);
        root.Verify(host => host.RemoveUiPage(second.Object), Times.Once);
        Assert.That(router.Count, Is.Zero);
    }

    /// <summary>A failed page enter removes the new page and restores the previous page.</summary>
    [Test]
    public async Task Push_WhenEnterThrows_RestoresPreviousPage()
    {
        var root = new Mock<IUiRoot>();
        var router = CreateRouter(root);
        var previous = CreatePage("previous");
        await router.PushAsync(previous.Object);
        var failing = CreatePage("failing");
        failing.Setup(page => page.OnEnter(It.IsAny<IUiPageEnterParam?>()))
            .Throws(new InvalidOperationException("enter failed"));

        await Assert.ThrowsAsync<AggregateException>(() => router.PushAsync(failing.Object).AsTask());

        Assert.That(router.Current, Is.SameAs(previous.Object));
        root.Verify(host => host.RemoveUiPage(failing.Object), Times.Once);
        previous.Verify(page => page.OnShow(), Times.Exactly(2));
        await router.DestroyAsync();
    }

    /// <summary>Awaiting the same router inside a handler reports an error instead of deadlocking.</summary>
    [Test]
    public async Task Push_WhenHandlerAwaitsNestedNavigation_RejectsReentrancy()
    {
        var router = CreateRouter(new Mock<IUiRoot>());
        var handler = new Mock<IUiTransitionHandler>();
        handler.SetupGet(item => item.Phases).Returns(UiTransitionPhases.BeforeChange);
        handler.Setup(item => item.ShouldHandle(It.IsAny<UiTransitionEvent>(), It.IsAny<UiTransitionPhases>()))
            .Returns(true);
        handler.Setup(item => item.HandleAsync(It.IsAny<UiTransitionEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() => router.PushAsync(CreatePage("nested").Object).AsTask());
        router.RegisterHandler(handler.Object, new UiTransitionHandlerOptions(ContinueOnError: false));

        await Assert.ThrowsAsync<InvalidOperationException>(() => router.PushAsync(CreatePage("outer").Object).AsTask());
    }

    /// <summary>A factory-owned target is reclaimed if old-page cleanup prevents replacement.</summary>
    [Test]
    public async Task Replace_WhenOldExitFails_CleansPreparedTarget()
    {
        var root = new Mock<IUiRoot>();
        var factory = new Mock<IUiFactory>();
        var target = CreatePage("target");
        factory.Setup(value => value.Create("target")).Returns(target.Object);
        var router = CreateRouter(root, factory.Object);
        var previous = CreatePage("previous");
        previous.Setup(value => value.OnExit()).Throws(new InvalidOperationException("exit failed"));
        await router.PushAsync(previous.Object);

        await Assert.ThrowsAsync<AggregateException>(() => router.ReplaceAsync("target").AsTask());

        target.Verify(value => value.OnExit(), Times.Once);
        root.Verify(value => value.RemoveUiPage(target.Object), Times.Once);
        await router.DestroyAsync();
        target.Verify(value => value.OnExit(), Times.Once);
    }

    /// <summary>Reusing a retained page as a floating page transfers ownership without a second shutdown exit.</summary>
    [Test]
    public async Task RetainedPage_ReusedAsLayer_IsReleasedOnce()
    {
        var root = new Mock<IUiRoot>();
        var router = CreateRouter(root);
        var page = CreatePage("reused");
        await router.PushAsync(page.Object);
        await router.PopAsync(UiPopPolicy.Suspend);
        var handle = router.Show(page.Object, UiLayer.Overlay);
        router.Hide(handle, UiLayer.Overlay, destroy: true);

        await router.DestroyAsync();

        page.Verify(value => value.OnExit(), Times.Once);
        root.Verify(value => value.RemoveUiPage(page.Object), Times.Once);
    }

    /// <summary>Creates a router with explicit managed host ownership and no architecture dependency.</summary>
    private static TestRouter CreateRouter(Mock<IUiRoot> root, IUiFactory? factory = null)
    {
        var router = new TestRouter();
        if (factory is not null)
        {
            var context = new Mock<IArchitectureContext>();
            context.Setup(value => value.GetUtility<IUiFactory>()).Returns(factory);
            context.Setup(value => value.GetService<ILoggerFactory>()).Returns(NullLoggerFactory.Instance);
            ((IContextAware)router).SetContext(context.Object);
            router.Initialize();
        }
        router.BindRoot(root.Object);
        return router;
    }

    /// <summary>Creates a minimal page with a stable identity.</summary>
    private static Mock<IUiPageBehavior> CreatePage(string key)
    {
        var page = new Mock<IUiPageBehavior>();
        page.SetupGet(item => item.Key).Returns(key);
        page.SetupGet(item => item.IsAlive).Returns(true);
        page.SetupGet(item => item.InteractionProfile).Returns(new UiInteractionProfile());
        return page;
    }

    /// <summary>Creates an asynchronous completion source so test ordering does not depend on inline continuations.</summary>
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Suspends a real transition handler while an independent navigation or shutdown is submitted.</summary>
    private static void BlockBeforeChange(TestRouter router, TaskCompletionSource entered, TaskCompletionSource release)
    {
        var handler = new Mock<IUiTransitionHandler>();
        handler.SetupGet(item => item.Phases).Returns(UiTransitionPhases.BeforeChange);
        handler.Setup(item => item.ShouldHandle(It.IsAny<UiTransitionEvent>(), It.IsAny<UiTransitionPhases>()))
            .Returns(true);
        handler.Setup(item => item.HandleAsync(It.IsAny<UiTransitionEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                entered.TrySetResult();
                return release.Task;
            });
        router.RegisterHandler(handler.Object);
    }

    /// <summary>Uses instance-page navigation without resolving the optional factory.</summary>
    private sealed class TestRouter : UiRouterBase
    {
        /// <summary>No handlers are installed automatically in these tests.</summary>
        protected override void RegisterHandlers()
        {
        }
    }
}

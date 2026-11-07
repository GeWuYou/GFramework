// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Game.Input;

namespace GFramework.Game.Tests.Input;

/// <summary>Checks action mapping, asynchronous completion and cancellation through the public input bridge.</summary>
[TestFixture]
public sealed class UiInputDispatcherTests
{
    /// <summary>Mapped actions retain the caller's cancellation token and the service's captured outcome.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task DispatchAsync_MappedAction_ForwardsToService(bool captured)
    {
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IUiService>();
        service.Setup(value => value.DispatchActionAsync(UiInputAction.Cancel, cancellation.Token))
            .ReturnsAsync(captured);
        var dispatcher = new UiInputDispatcher(new UiInputActionMap(), service.Object);

        var result = await dispatcher.DispatchAsync("ui_cancel", cancellation.Token);

        Assert.That(result, Is.EqualTo(captured));
        service.Verify(value => value.DispatchActionAsync(UiInputAction.Cancel, cancellation.Token), Times.Once);
    }

    /// <summary>An unmapped logical action never enters the UI mutation queue.</summary>
    [Test]
    public async Task DispatchAsync_UnmappedAction_ReturnsFalse()
    {
        var service = new Mock<IUiService>();
        var dispatcher = new UiInputDispatcher(new UiInputActionMap(), service.Object);

        Assert.That(await dispatcher.DispatchAsync("inventory_toggle"), Is.False);
        service.Verify(value => value.DispatchActionAsync(It.IsAny<UiInputAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Cancellation before dispatch cannot invoke a service operation.</summary>
    [Test]
    public void DispatchAsync_PreCancelled_DoesNotDispatch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new Mock<IUiService>();
        var dispatcher = new UiInputDispatcher(new UiInputActionMap(), service.Object);

        Assert.Throws<OperationCanceledException>(() => { _ = dispatcher.DispatchAsync("ui_cancel", cancellation.Token); });
        service.Verify(value => value.DispatchActionAsync(It.IsAny<UiInputAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Callers can await native or lifecycle dispatch failures at the input boundary.</summary>
    [Test]
    public async Task DispatchAsync_ServiceFailure_Propagates()
    {
        var failure = new InvalidOperationException("native dispatch failed");
        var service = new Mock<IUiService>();
        service.Setup(value => value.DispatchActionAsync(UiInputAction.Cancel, It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<bool>(failure));
        var dispatcher = new UiInputDispatcher(new UiInputActionMap(), service.Object);

        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("ui_cancel"));

        Assert.That(observed, Is.SameAs(failure));
    }
}

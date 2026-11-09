// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Events;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Runtime;
using GFramework.Game.Setting.Events;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Setting;

/// <summary>
/// Applies and persists settings through an explicitly supplied model.
/// Notifications are optional constructor dependencies; no architecture or global context is consulted.
/// </summary>
public sealed class SettingsSystem : ISettingsSystem
{
    private readonly ISettingsModel _model;
    private readonly Action<object>? _publishEvent;
    private readonly IGameDispatcher _dispatcher;
    private readonly ILogger? _observerLogger;

    /// <summary>Creates the coordinator without starting I/O or applying settings.</summary>
    /// <param name="model">Settings data and applicator registry.</param>
    /// <param name="publishEvent">Optional notification callback, invoked after the corresponding operation.</param>
    /// <param name="dispatcher">Host callback dispatcher; defaults to direct execution for thread-neutral settings.</param>
    /// <param name="observerLogger">Notification diagnostics; defaults to Trace.</param>
    public SettingsSystem(ISettingsModel model, Action<object>? publishEvent = null, IGameDispatcher? dispatcher = null,
        ILogger? observerLogger = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _observerLogger = observerLogger;
        _publishEvent = publishEvent;
        _dispatcher = dispatcher ?? new InlineGameDispatcher();
    }

    /// <inheritdoc />
    public async Task ApplyAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<Exception>();
        foreach (var applicator in _model.AllApplicators())
        {
            var error = await TryApplyAsync(applicator, cancellationToken).ConfigureAwait(false);
            if (error is not null)
                errors.Add(error);
        }
        if (errors.Count != 0)
            throw new AggregateException("One or more settings applicators failed.", errors);
    }

    /// <inheritdoc />
    public async Task ApplyAsync<T>(CancellationToken cancellationToken = default) where T : class, IResetApplyAbleSettings
    {
        cancellationToken.ThrowIfCancellationRequested();
        var applicator = _model.GetApplicator<T>();
        if (applicator is null)
            return;
        var error = await TryApplyAsync(applicator, cancellationToken).ConfigureAwait(false);
        if (error is not null)
            throw new AggregateException("Settings application failed.", error);
    }

    /// <inheritdoc />
    public Task SaveAllAsync(CancellationToken cancellationToken = default) => _model.SaveAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _model.ResetAll();
        await ApplyAllAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ISettingsData, IResetApplyAbleSettings, new()
    {
        cancellationToken.ThrowIfCancellationRequested();
        _model.Reset<T>();
        await ApplyAsync<T>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes application results while allowing independent applicators to continue after failure.</summary>
    /// <param name="section">Settings applicator.</param>
    /// <param name="cancellationToken">Cancels admission or a cancellable applicator.</param>
    private async Task<Exception?> TryApplyAsync(ISettingsSection section, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (section is not IApplyAbleSettings applicator)
            return null;
        ObserverNotification.Notify(_publishEvent, (object)new SettingsApplyingEvent<ISettingsSection>(section), _observerLogger);
        try
        {
            await _dispatcher.InvokeAsync(() => applicator.ApplyAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
            ObserverNotification.Notify(_publishEvent, (object)new SettingsAppliedEvent<ISettingsSection>(section, success: true), _observerLogger);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ObserverNotification.Notify(_publishEvent, (object)new SettingsAppliedEvent<ISettingsSection>(section, success: false, exception), _observerLogger);
            return exception;
        }
    }
}

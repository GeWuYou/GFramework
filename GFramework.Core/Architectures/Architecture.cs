// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Events;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>Owns one DI provider and optionally orchestrates explicitly registered lifecycle participants.</summary>
/// <remarks>
/// Configuration only records descriptions. Startup is single-use, and failure/cancellation cleans partial startup
/// before completing callers. Stop is a shared, idempotent result; business stopping precedes DI resource release.
/// Ordinary services need no framework base class, role marker, or architecture reference.
/// </remarks>
public abstract class Architecture : IArchitecture
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _operationLock = new();
#else
    private readonly object _operationLock = new();
#endif
    private readonly List<object> _started = [];
    private readonly AsyncLocal<bool> _insideCallback = new();
    private readonly AsyncLocal<int> _cleanupAccess = new();
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IServiceProvider _serviceAccess;
    private ServiceProvider? _provider;
    private Task? _startupTask;
    private Task? _shutdownTask;
    private Task? _cleanupTask;
    private CancellationTokenSource? _startupCancellation;
    private int _phase;
    private volatile bool _stopping;

    /// <summary>Creates an instance owner without constructing or activating application services.</summary>
    /// <param name="configuration">Optional logging configuration; an injected factory remains caller-owned.</param>
    protected Architecture(IArchitectureConfiguration? configuration = null)
    {
        _loggerFactory = configuration?.LoggerProperties.LoggerFactory;
        _serviceAccess = new ArchitectureServiceAccess(this);
    }

    /// <summary>Standard service access for the composition root; unavailable before build or during external stop.</summary>
    public IServiceProvider Services => _serviceAccess;

    /// <summary>The current operation state; observation does not grant permission to add registrations.</summary>
    public ArchitecturePhase CurrentPhase => (ArchitecturePhase)Volatile.Read(ref _phase);

    /// <summary>Whether startup completed and new work is still accepted.</summary>
    public bool IsReady => CurrentPhase == ArchitecturePhase.Ready && !_stopping;

    /// <summary>Observes state changes with this architecture as sender.</summary>
    public event EventHandler<ArchitecturePhaseChangedEventArgs>? PhaseChanged;

    /// <summary>Records application descriptions; this hook must not perform IO or resolve services.</summary>
    /// <param name="services">The one collection that will build this instance's provider.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>Connects a synchronous host after participants have started.</summary>
    protected virtual void OnStarted()
    {
    }

    /// <summary>Connects an asynchronous host after participants have started.</summary>
    /// <param name="cancellationToken">The startup lifetime.</param>
    /// <returns>Completion of host attachment.</returns>
    protected virtual ValueTask OnStartedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable MA0042 // This adapter invokes the synchronous hook rather than recursively invoking itself.
        OnStarted();
#pragma warning restore MA0042
        return ValueTask.CompletedTask;
    }

    /// <summary>Stops host admission before participant cleanup, preserving dependencies needed to stop them.</summary>
    /// <returns>Completion of host admission shutdown.</returns>
    protected virtual ValueTask OnStoppingAsync() => ValueTask.CompletedTask;

    /// <summary>Detaches host resources after business stop and before the provider is disposed.</summary>
    /// <returns>Completion of host detachment.</returns>
    protected virtual ValueTask OnStoppedAsync() => ValueTask.CompletedTask;

    /// <summary>Starts only synchronous participants; asynchronous work must use InitializeAsync.</summary>
    public void Initialize() => StartInitialization(asyncMode: false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Builds once, starts participants in explicit order, then attaches the host.</summary>
    /// <param name="cancellationToken">Cancels startup and waits for partial cleanup.</param>
    /// <returns>The unique startup result.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default) => StartInitialization(asyncMode: true, cancellationToken);

    /// <summary>Waits for readiness or for failed/cancelled startup cleanup to finish.</summary>
    /// <returns>The shared readiness result.</returns>
    public Task WaitUntilReadyAsync() => _ready.Task;

    /// <summary>Resolves through the valid instance window; cleanup callbacks alone retain access while stopping.</summary>
    /// <param name="serviceType">The requested standard DI key.</param>
    /// <returns>The registered service, or null for an unknown key.</returns>
    internal object? ResolveService(Type serviceType)
    {
        if (_stopping && _cleanupAccess.Value == 0)
            throw new ObjectDisposedException(nameof(Architecture));
        var provider = _provider ?? throw new InvalidOperationException("Services are unavailable during configuration. Inject dependencies into components.");
        return provider.GetService(serviceType);
    }

    /// <summary>Publishes the startup operation before executing any user callbacks.</summary>
    private Task StartInitialization(bool asyncMode, CancellationToken cancellationToken)
    {
        TaskCompletionSource completion;
        CancellationToken token;
        lock (_operationLock)
        {
            if (_startupTask is not null || _shutdownTask is not null)
                throw new InvalidOperationException("Architecture is single-use; create a new instance.");
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _startupTask = completion.Task;
            _startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            token = _startupCancellation.Token;
        }
        _ = CompleteInitializationAsync(completion, asyncMode, token, cancellationToken);
        return completion.Task;
    }

    /// <summary>Publishes success only after the final host callback; failures wait for shared cleanup first.</summary>
    private async Task CompleteInitializationAsync(TaskCompletionSource completion, bool asyncMode,
        CancellationToken token, CancellationToken callerToken)
    {
        _insideCallback.Value = true;
        try
        {
            await ConfigureAndStartAsync(asyncMode, token).ConfigureAwait(false);
            _ready.TrySetResult();
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            _stopping = true;
            Exception failure = error;
            try
            {
                await EnsureCleanupAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupError) { failure = new AggregateException("Startup and cleanup failed.", error, cleanupError); }
            if (PublishTerminalPhase(ArchitecturePhase.Faulted) is { } phaseError)
                failure = new AggregateException("Startup failed and a terminal observer also failed.", failure, phaseError);
            if (failure is OperationCanceledException && callerToken.IsCancellationRequested)
            {
                _ready.TrySetCanceled(callerToken);
                completion.TrySetCanceled(callerToken);
            }
            else if (failure is OperationCanceledException cancelled)
            {
                _ready.TrySetCanceled(cancelled.CancellationToken);
                completion.TrySetCanceled(cancelled.CancellationToken);
            }
            else
            {
                _ready.TrySetException(failure);
                completion.TrySetException(failure);
            }
        }
        finally
        {
            CancellationTokenSource? cancellation;
            lock (_operationLock)
            {
                cancellation = _startupCancellation;
                _startupCancellation = null;
            }
            cancellation?.Dispose();
            _insideCallback.Value = false;
        }
    }

    /// <summary>Builds standard DI once; the provider owns type/factory services and never owns passed instances.</summary>
    private async Task ConfigureAndStartAsync(bool asyncMode, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        SetPhase(ArchitecturePhase.Configuring);
        var descriptions = new ServiceCollection();
        if (_loggerFactory is null)
        {
            // Keep standard AddLogging configuration effective; DI creates and owns the default factory.
            descriptions.AddLogging(builder => builder.AddConsole());
        }
        else
        {
            // An explicitly supplied factory is borrowed and keeps its caller-owned configuration.
            descriptions.AddSingleton(_loggerFactory);
            descriptions.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }
        descriptions.TryAddSingleton<IEventBus, EventBus>();
        ConfigureServices(descriptions);
        token.ThrowIfCancellationRequested();
        _provider = descriptions.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        SetPhase(ArchitecturePhase.Initializing);
        var participants = _provider.GetServices<ComponentRegistration>().OrderBy(item => item.Order).ToArray();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var registration in participants)
        {
            token.ThrowIfCancellationRequested();
            var component = _provider.GetRequiredService(registration.ServiceType);
            if (!seen.Add(component))
                continue;
            if (!asyncMode && component is IAsyncInitializable)
                throw new InvalidOperationException("An asynchronous participant requires InitializeAsync().");
            // Record before calling user code: partial initialization is still owed business cleanup.
            _started.Add(component);
            if (component is IAsyncInitializable asynchronous)
                await asynchronous.InitializeAsync(token).ConfigureAwait(false);
            else if (component is IInitializable synchronous)
                synchronous.Initialize();
            token.ThrowIfCancellationRequested();
        }
        if (asyncMode)
            await OnStartedAsync(token).ConfigureAwait(false);
        else
        {
            var hook = GetType().GetMethod(nameof(OnStartedAsync), BindingFlags.Instance | BindingFlags.NonPublic);
            if (hook?.DeclaringType != typeof(Architecture))
                throw new InvalidOperationException("An asynchronous host requires InitializeAsync().");
#pragma warning disable MA0042 // The synchronous entry deliberately rejects overridden async host hooks.
            OnStarted();
#pragma warning restore MA0042
        }
        token.ThrowIfCancellationRequested();
        SetPhase(ArchitecturePhase.Ready);
        token.ThrowIfCancellationRequested();
    }

    /// <summary>Cancels and joins startup, then waits for one shared reverse-stop and provider disposal result.</summary>
    /// <returns>The shared terminal cleanup result.</returns>
    public ValueTask DestroyAsync()
    {
        // An ExecutionContext captured by a callback can outlive that operation. Its old AsyncLocal
        // marker must not prohibit a later legitimate shutdown after the operation has completed.
        if (_insideCallback.Value && (_startupTask is { IsCompleted: false } || _cleanupTask is { IsCompleted: false } || _shutdownTask is { IsCompleted: false }))
            throw new InvalidOperationException("A lifecycle callback cannot await destruction of its own architecture.");
        TaskCompletionSource completion;
        Task? startup;
        CancellationTokenSource? cancellation;
        lock (_operationLock)
        {
            if (_shutdownTask is not null)
                return new ValueTask(_shutdownTask);
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _shutdownTask = completion.Task;
            startup = _startupTask;
            cancellation = _startupCancellation;
            _startupCancellation = null;
            _stopping = true;
        }
        _ = CompleteDestructionAsync(completion, startup, cancellation);
        return new ValueTask(completion.Task);
    }

    /// <summary>Waits for startup's own failure cleanup, then reuses that result instead of running stop twice.</summary>
    private async Task CompleteDestructionAsync(TaskCompletionSource completion, Task? startup, CancellationTokenSource? cancellation)
    {
        var errors = new List<Exception>();
        try
        {
#pragma warning disable MA0042 // Cancellation callbacks must execute inline before joining startup.
            try
            {
                cancellation?.Cancel();
            }
            catch (Exception error) { errors.Add(error); }
#pragma warning restore MA0042
            if (startup is not null)
            {
                try
                {
                    await startup.ConfigureAwait(false);
                }
                catch (Exception) { /* Startup already delivered its own failure after cleanup. */ }
            }
            try
            {
                await EnsureCleanupAsync().ConfigureAwait(false);
            }
            catch (Exception error) { errors.Add(error); }
            if (startup is null)
                _ready.TrySetException(new ObjectDisposedException(nameof(Architecture)));
        }
        finally { cancellation?.Dispose(); }
        if (errors.Count == 0 && CurrentPhase != ArchitecturePhase.Faulted &&
            PublishTerminalPhase(ArchitecturePhase.Stopped) is { } stoppedObserverError)
            errors.Add(stoppedObserverError);
        if (errors.Count == 0)
        {
            GC.SuppressFinalize(this);
            completion.TrySetResult();
        }
        else
        {
            if (PublishTerminalPhase(ArchitecturePhase.Faulted) is { } failedObserverError)
                errors.Add(failedObserverError);
            completion.TrySetException(new AggregateException("Architecture shutdown failed.", errors));
        }
    }

    /// <summary>Shares standard async disposal with business shutdown.</summary>
    /// <returns>The same terminal task as DestroyAsync.</returns>
    public ValueTask DisposeAsync() => DestroyAsync();

    /// <summary>Publishes one cleanup task without executing user code while holding the publication lock.</summary>
    private Task EnsureCleanupAsync()
    {
        TaskCompletionSource completion;
        lock (_operationLock)
        {
            if (_cleanupTask is not null)
                return _cleanupTask;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _cleanupTask = completion.Task;
        }
        _ = CompleteCleanupAsync(completion);
        return completion.Task;
    }

    /// <summary>Attempts every cleanup phase and aggregates failures after the provider and logger are handled.</summary>
    private async Task CompleteCleanupAsync(TaskCompletionSource completion)
    {
        _insideCallback.Value = true;
        _cleanupAccess.Value++;
        var errors = new List<Exception>();
        try
        {
            try
            {
                SetPhase(ArchitecturePhase.Stopping);
            }
            catch (Exception error) { errors.Add(error); }
            await AttemptAsync(OnStoppingAsync, errors).ConfigureAwait(false);
            for (var index = _started.Count - 1; index >= 0; index--)
            {
                var component = _started[index];
                if (component is IAsyncDestroyable asynchronous)
                    await AttemptAsync(asynchronous.DestroyAsync, errors).ConfigureAwait(false);
                else if (component is IDestroyable synchronous)
                {
                    try
                    {
                        synchronous.Destroy();
                    }
                    catch (Exception error) { errors.Add(error); }
                }
            }
            _started.Clear();
            await AttemptAsync(OnStoppedAsync, errors).ConfigureAwait(false);
            if (_provider is not null)
                await AttemptAsync(_provider.DisposeAsync, errors).ConfigureAwait(false);

        }
        finally
        {
            _cleanupAccess.Value--;
            _insideCallback.Value = false;
        }
        if (errors.Count == 0)
            completion.TrySetResult();
        else
            completion.TrySetException(new AggregateException("One or more cleanup phases failed.", errors));
    }

    /// <summary>Preserves later cleanup even when one participant fails.</summary>
    private static async ValueTask AttemptAsync(Func<ValueTask> action, List<Exception> errors)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception error) { errors.Add(error); }
    }

    /// <summary>Publishes a state before notifying observers; observer failure belongs to the current operation.</summary>
    private void SetPhase(ArchitecturePhase phase)
    {
        Volatile.Write(ref _phase, (int)phase);
        var previous = _insideCallback.Value;
        _insideCallback.Value = true;
        try
        {
            PhaseChanged?.Invoke(this, new ArchitecturePhaseChangedEventArgs(phase));
        }
        finally { _insideCallback.Value = previous; }
    }

    /// <summary>Notifies terminal observers once, returning their failure so completion barriers can still be published.</summary>
    private Exception? PublishTerminalPhase(ArchitecturePhase phase)
    {
        if (CurrentPhase == phase)
            return null;
        try
        {
            SetPhase(phase);
            return null;
        }
        catch (Exception error) { return error; }
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Resource;

namespace GFramework.Core.Resource;

/// <summary>
///     Owns one cache entry per resource type/path and issues independent leases on that ownership.
///     Loading and releasing resources is delegated to externally owned loaders.
/// </summary>
/// <remarks>
///     The operation lock protects cache identities, pending acquisitions and lease counts. Loader callbacks run
///     outside that lock. One shared load is retained even when individual waiters cancel. Closing waits for loads and releases already
///     in progress, but does not wait for consumer-held leases: those remain valid and release their resource when disposed.
///     Keep loader release capabilities alive until every lease is returned, including after shutdown completes.
///     Prefer asynchronous acquisition and disposal whenever a loader can suspend.
/// </remarks>
public sealed class ResourceManager : IResourceManager
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly Dictionary<(Type Type, string Path), LoadOperation> _entries = [];
    private readonly Dictionary<Type, object> _loaders = [];
    private readonly List<Exception> _shutdownErrors = [];
    private IResourceReleaseStrategy _releaseStrategy = new ManualReleaseStrategy();
    private int _activeReleases;
    private TaskCompletionSource? _releaseDrain;
    private bool _closed;
    private Task? _shutdownTask;

    /// <inheritdoc />
    public int LoadedResourceCount
    {
        get
        {
            lock (_gate)
                return _entries.Values.Count(operation => operation.Entry != null);
        }
    }

    /// <inheritdoc />
    public IResourceHandle<T> Acquire<T>(string path) where T : class
    {
        var key = CreateKey<T>(path);
        var operation = Reserve<T>(key, allowPending: false, out var loader);
        try
        {
            if (loader != null)
                LoadShared(key, operation, loader);
            var entry = operation.Completion.Task.GetAwaiter().GetResult();
            return CreateLease<T>(key, operation, entry);
        }
        finally
        {
            FinishReservation(key, operation);
        }
    }

    /// <inheritdoc />
    public async Task<IResourceHandle<T>> AcquireAsync<T>(string path, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey<T>(path);
        var operation = Reserve<T>(key, allowPending: true, out var loader);
        try
        {
            if (loader != null)
                _ = LoadSharedAsync(key, operation, loader);
            var entry = await operation.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return CreateLease<T>(key, operation, entry);
        }
        finally
        {
            FinishReservation(key, operation);
        }
    }

    /// <inheritdoc />
    public async Task PreloadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey<T>(path);
        var operation = Reserve<T>(key, allowPending: true, out var loader);
        try
        {
            if (loader != null)
                _ = LoadSharedAsync(key, operation, loader);
            await operation.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                ThrowIfClosed();
                operation.Pinned = true;
            }
        }
        finally
        {
            FinishReservation(key, operation);
        }
    }

    /// <inheritdoc />
    public bool Unload<T>(string path) where T : class
    {
        var key = CreateKey<T>(path);
        ResourceCacheEntry? entry;
        lock (_gate)
        {
            ThrowIfClosed();
            if (!_entries.TryGetValue(key, out var operation) || operation.Entry == null ||
                operation.Entry.LeaseCount != 0 || operation.Reservations != 0)
                return false;
            entry = operation.Entry;
            _entries.Remove(key);
            TrackRelease();
        }
        ReleaseTracked(entry);
        return true;
    }

    /// <inheritdoc />
    public void UnloadAll()
    {
        List<ResourceCacheEntry> released;
        lock (_gate)
        {
            ThrowIfClosed();
            released = DetachUnleasedEntries();
        }
        ReleaseAll(released);
    }

    /// <inheritdoc />
    public bool IsLoaded<T>(string path) where T : class
    {
        var key = CreateKey<T>(path);
        lock (_gate)
        {
            ThrowIfClosed();
            return _entries.TryGetValue(key, out var operation) && operation.Entry != null;
        }
    }

    /// <inheritdoc />
    public void RegisterLoader<T>(IResourceLoader<T> loader) where T : class
    {
        ArgumentNullException.ThrowIfNull(loader);
        lock (_gate)
        {
            ThrowIfClosed();
            _loaders[typeof(T)] = loader;
        }
    }

    /// <inheritdoc />
    public void UnregisterLoader<T>() where T : class
    {
        lock (_gate)
        {
            ThrowIfClosed();
            _loaders.Remove(typeof(T));
        }
    }

    /// <inheritdoc />
    public void SetReleaseStrategy(IResourceReleaseStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        lock (_gate)
        {
            ThrowIfClosed();
            _releaseStrategy = strategy;
        }
    }

    /// <summary>
    ///     Closes the manager synchronously when no load or release is pending. Use DisposeAsync otherwise,
    ///     so a loader awaiting a host synchronization context is never blocked by synchronous shutdown.
    /// </summary>
    /// <exception cref="InvalidOperationException">A load, release or asynchronous shutdown is still pending.</exception>
    public void Dispose()
    {
        StartShutdown(synchronous: true).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Rejects new acquisitions, waits for shared loads and in-flight releases, and releases entries without leases.
    ///     Repeated calls observe the same shutdown task; active leases remain valid until individually disposed.
    /// </summary>
    /// <returns>The shared shutdown operation.</returns>
    public ValueTask DisposeAsync()
    {
        var shutdown = StartShutdown(synchronous: false);
        GC.SuppressFinalize(this);
        return shutdown;
    }

    /// <summary>Publishes the closing boundary atomically with the synchronous pending-load check.</summary>
    private ValueTask StartShutdown(bool synchronous)
    {
        TaskCompletionSource? completion = null;
        Task[] pending;
        List<ResourceCacheEntry> released;
        lock (_gate)
        {
            if (synchronous && (_activeReleases != 0 || _entries.Values.Any(operation => !operation.Completion.Task.IsCompleted) ||
                                _shutdownTask is { IsCompleted: false }))
                throw new InvalidOperationException("Resource loading or release is pending. Await DisposeAsync() instead.");
            if (_shutdownTask != null)
                return new ValueTask(_shutdownTask);
            _closed = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _shutdownTask = completion.Task;
            pending = _entries.Values.Select(operation => operation.Completion.Task).ToArray();
            released = DetachUnleasedEntries();
            _loaders.Clear();
        }
        _ = CompleteShutdownAsync(completion, pending, released);
        return new ValueTask(completion.Task);
    }

    /// <summary>Validates the path before constructing the typed ordinal cache identity.</summary>
    private static (Type Type, string Path) CreateKey<T>(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return (typeof(T), path);
    }

    /// <summary>
    ///     Reserves one acquisition before starting a load. Reservations prevent last-lease release from evicting
    ///     an entry before an awaiting caller can convert its reservation into a lease.
    /// </summary>
    private LoadOperation Reserve<T>((Type Type, string Path) key, bool allowPending, out IResourceLoader<T>? loader)
        where T : class
    {
        lock (_gate)
        {
            ThrowIfClosed();
            loader = null;
            if (_entries.TryGetValue(key, out var existing))
            {
                if (!allowPending && !existing.Completion.Task.IsCompleted)
                    throw new InvalidOperationException("A shared load is in flight. Use AcquireAsync().");
                existing.Reservations++;
                return existing;
            }
            if (!_loaders.TryGetValue(typeof(T), out var registered))
                throw new InvalidOperationException($"No loader registered for type {typeof(T).Name}.");
            loader = (IResourceLoader<T>)registered;
            var operation = new LoadOperation { Reservations = 1 };
            _entries.Add(key, operation);
            return operation;
        }
    }

    /// <summary>Loads outside the operation lock and publishes all failures through the shared completion.</summary>
    private void LoadShared<T>((Type Type, string Path) key, LoadOperation operation, IResourceLoader<T> loader)
        where T : class
    {
        try
        {
            if (!loader.CanLoad(key.Path))
                throw new InvalidOperationException($"Loader does not support '{key.Path}'.");
            var resource = loader.Load(key.Path);
            if (resource == null)
                throw new InvalidOperationException($"Loader returned null for '{key.Path}'.");
            PublishLoad(key, operation, new ResourceCacheEntry(resource, () => loader.Unload(resource)));
        }
        catch (Exception error)
        {
            FailLoad(key, operation, error);
        }
    }

    /// <summary>Shares one load independently of caller cancellation; no thread-pool scheduling is introduced.</summary>
    private async Task LoadSharedAsync<T>((Type Type, string Path) key, LoadOperation operation, IResourceLoader<T> loader)
        where T : class
    {
        try
        {
            if (!loader.CanLoad(key.Path))
                throw new InvalidOperationException($"Loader does not support '{key.Path}'.");
            var resource = await loader.LoadAsync(key.Path, CancellationToken.None).ConfigureAwait(false);
            if (resource == null)
                throw new InvalidOperationException($"Loader returned null for '{key.Path}'.");
            PublishLoad(key, operation, new ResourceCacheEntry(resource, () => loader.Unload(resource)));
        }
        catch (Exception error)
        {
            FailLoad(key, operation, error);
        }
    }

    /// <summary>Publishes ownership before waking waiters; a manager closed during loading releases it immediately.</summary>
    private void PublishLoad((Type Type, string Path) key, LoadOperation operation, ResourceCacheEntry entry)
    {
        ResourceCacheEntry? released;
        lock (_gate)
        {
            operation.Entry = entry;
            released = DetachIfEligible(key, operation);
        }
        ReleaseAfterClose(released);
        operation.Completion.TrySetResult(entry);
    }

    /// <summary>Removes a failed flight so a later acquisition can retry, while all current waiters see the failure.</summary>
    private void FailLoad((Type Type, string Path) key, LoadOperation operation, Exception error)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, operation))
                _entries.Remove(key);
        }
        operation.Completion.TrySetException(error);
    }

    /// <summary>Converts an acquisition reservation into an independently owned lease under the same lock.</summary>
    private IResourceHandle<T> CreateLease<T>((Type Type, string Path) key, LoadOperation operation, ResourceCacheEntry entry)
        where T : class
    {
        lock (_gate)
        {
            ThrowIfClosed();
            entry.LeaseCount++;
            return new ResourceHandle<T>((T)entry.Resource, key.Path,
                () => CreateLease<T>(key, operation, entry), () => ReleaseLease(key, operation, entry));
        }
    }

    /// <summary>Ends an acquisition even after cancellation; orphaned auto-release loads can then be released.</summary>
    private void FinishReservation((Type Type, string Path) key, LoadOperation operation)
    {
        ResourceCacheEntry? released;
        lock (_gate)
        {
            operation.Reservations--;
            released = DetachIfEligible(key, operation);
        }
        ReleaseAfterClose(released);
    }

    /// <summary>Releases exactly one lease and, when eligible, detaches before calling external code.</summary>
    private void ReleaseLease((Type Type, string Path) key, LoadOperation operation, ResourceCacheEntry entry)
    {
        ResourceCacheEntry? released;
        lock (_gate)
        {
            entry.LeaseCount--;
            released = DetachIfEligible(key, operation);
        }
        if (released is not null)
            ReleaseTracked(released);
    }

    /// <summary>Detaches only the current identity; stale release callbacks cannot affect a later load of the same key.</summary>
    private ResourceCacheEntry? DetachIfEligible((Type Type, string Path) key, LoadOperation operation)
    {
        if (operation.Entry == null || operation.Entry.LeaseCount != 0 ||
            !_entries.TryGetValue(key, out var current) || !ReferenceEquals(current, operation))
            return null;
        if (!_closed && (operation.Reservations != 0 || operation.Pinned ||
                         !_releaseStrategy.ShouldRelease(key.Path, 0)))
            return null;
        // A custom policy is application code. Recheck after it runs so even a reentrant policy cannot
        // release an entry which acquired a new lease or was replaced while the policy was deciding.
        if (operation.Entry.LeaseCount != 0 ||
            !_entries.TryGetValue(key, out current) || !ReferenceEquals(current, operation) ||
            (!_closed && (operation.Reservations != 0 || operation.Pinned)))
            return null;
        _entries.Remove(key);
        TrackRelease();
        return operation.Entry;
    }

    /// <summary>Collects releasable ownership before invoking loaders, so one failure cannot skip other resources.</summary>
    private List<ResourceCacheEntry> DetachUnleasedEntries()
    {
        var released = new List<ResourceCacheEntry>();
        foreach (var (key, operation) in _entries.ToArray())
        {
            if (operation.Entry == null || operation.Entry.LeaseCount != 0 || (!_closed && operation.Reservations != 0))
                continue;
            _entries.Remove(key);
            TrackRelease();
            released.Add(operation.Entry);
        }
        return released;
    }

    /// <summary>Reserves a release under the cache lock before its detached entry becomes invisible to shutdown.</summary>
    private void TrackRelease()
    {
        if (_activeReleases++ == 0)
            _releaseDrain = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    ///     Runs external cleanup without the cache lock. Failure routing and the release drain are published under
    ///     one lock, so a concurrent close cannot hide an error between the caller and the shutdown result.
    /// </summary>
    private void ReleaseTracked(ResourceCacheEntry entry, bool suppressAfterClose = false)
    {
        Exception? failure = null;
        var suppressed = false;
        try
        {
            entry.Release();
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            lock (_gate)
            {
                if (_closed && failure is not null)
                {
                    _shutdownErrors.Add(failure);
                    suppressed = suppressAfterClose;
                }
                if (--_activeReleases == 0)
                    _releaseDrain!.TrySetResult();
            }
        }
        if (failure is not null && !suppressed)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    /// <summary>Delivers ordinary failures to acquisitions, or records closing failures for the shared shutdown.</summary>
    private void ReleaseAfterClose(ResourceCacheEntry? entry)
    {
        if (entry is not null)
            ReleaseTracked(entry, suppressAfterClose: true);
    }

    /// <summary>Attempts every release and reports failures together.</summary>
    private void ReleaseAll(IEnumerable<ResourceCacheEntry> entries)
    {
        var errors = new List<Exception>();
        foreach (var entry in entries)
        {
            try
            {
                ReleaseTracked(entry);
            }
            catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count != 0)
            throw new AggregateException("One or more resource releases failed.", errors);
    }

    /// <summary>Completes only after loads and their closing releases settle; load failures remain acquisition failures.</summary>
    private async Task CompleteShutdownAsync(TaskCompletionSource completion, Task[] pending, List<ResourceCacheEntry> released)
    {
        try
        {
            foreach (var entry in released)
                ReleaseAfterClose(entry);
            try
            {
                await Task.WhenAll(pending).ConfigureAwait(false);
            }
            catch (Exception) { /* Each shared failure is already delivered to its acquiring callers. */ }
            // A detached entry may no longer be in the cache when shutdown starts. Join its release barrier,
            // then recheck under the same lock so another release cannot slip between the drain and completion.
            while (true)
            {
                Task drain;
                lock (_gate)
                {
                    if (_activeReleases == 0)
                    {
                        if (_shutdownErrors.Count != 0)
                            completion.TrySetException(new AggregateException("Resource shutdown failed.", _shutdownErrors));
                        else
                            completion.TrySetResult();
                        return;
                    }
                    drain = _releaseDrain!.Task;
                }
                await drain.ConfigureAwait(false);
            }
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }
    }

    /// <summary>Rejects new work after the closing boundary is published.</summary>
    private void ThrowIfClosed() => ObjectDisposedException.ThrowIf(_closed, this);

    /// <summary>Shares a load and reserves entry ownership until each pending consumer obtains or abandons its lease.</summary>
    private sealed class LoadOperation
    {
        /// <summary>Gets the single load completion observed by all waiters.</summary>
        public TaskCompletionSource<ResourceCacheEntry> Completion
        {
            get;
        } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets or sets the loaded ownership, if loading succeeded.</summary>
        public ResourceCacheEntry? Entry
        {
            get; set;
        }

        /// <summary>Gets or sets pending callers that have not yet converted or abandoned their lease reservation.</summary>
        public int Reservations
        {
            get; set;
        }

        /// <summary>Gets or sets whether a successful explicit preload pins the entry.</summary>
        public bool Pinned
        {
            get; set;
        }
    }
}

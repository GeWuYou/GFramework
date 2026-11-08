// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Content;
using GFramework.Game.Storage;

namespace GFramework.Game.Data;

/// <summary>
/// Owns slot serialization and document-first save preparation. Schema conversion never requires the current
/// game DTO to understand historical data. Preparation has no storage/world side effects; explicit commit backs
/// up original bytes and atomically replaces them. One repository owns a slot set; gates are not inter-process locks.
/// </summary>
/// <typeparam name="TSaveData">Current-schema game DTO.</typeparam>
public sealed partial class SaveRepository<TSaveData> : ISaveRepository<TSaveData> where TSaveData : class, IData, new()
{
    private readonly SaveConfiguration _configuration;
    private readonly IStorage _rootStorage;
    private readonly SaveRepositoryOptions<TSaveData> _options;
    private readonly ISaveDocumentCodec _codec;
    private readonly IReadOnlyDictionary<string, ModSavePolicy> _modPolicies;
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _slotGates = new();
    private readonly Guid _owner = Guid.NewGuid();

    /// <summary>Creates an explicitly configured document repository; storage must support lossless atomic snapshots.</summary>
    /// <param name="storage">Independent game-owned storage with IStorageBackup support.</param>
    /// <param name="configuration">Slot layout.</param>
    /// <param name="options">Game identity, current schema and pure transformation policies.</param>
    public SaveRepository(IStorage storage, SaveConfiguration configuration, SaveRepositoryOptions<TSaveData> options)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GameId, nameof(options));
        if (options.CurrentVersion < 1) throw new ArgumentOutOfRangeException(nameof(options));
        if (storage is not IStorageBackup) throw new ArgumentException("Document saves require IStorageBackup.", nameof(storage));
        _codec = options.Codec ?? throw new ArgumentException("A document codec is required.", nameof(options));
        ArgumentNullException.ThrowIfNull(options.Migrations, nameof(options));
        ArgumentNullException.ThrowIfNull(options.ModPolicies, nameof(options));
        if (options.Content is not null) ArgumentNullException.ThrowIfNull(options.Content.CurrentCatalog, nameof(options));
        _configuration = configuration;
        _options = options;
        _rootStorage = new ScopedStorage(storage, configuration.SaveRoot);
        var policies = new Dictionary<string, ModSavePolicy>(StringComparer.Ordinal);
        foreach (var policy in options.ModPolicies)
        {
            ArgumentNullException.ThrowIfNull(policy, nameof(options));
            // Capture explicit paths as well as the collection: caller-owned lists cannot mutate a prepared policy.
            var captured = new ModSavePolicy(policy.ModId, policy.CurrentVersion, policy.Migrations)
            {
                VersionPath = policy.VersionPath is null ? null : Array.AsReadOnly(policy.VersionPath.ToArray())
            };
            if (!policies.TryAdd(policy.ModId, captured)) throw new ArgumentException("Duplicate active mod save policy.", nameof(options));
        }
        _modPolicies = policies;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int slot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetSlotStorage(slot).ExistsAsync(_configuration.SaveFileName).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TSaveData> LoadAsync(int slot, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(slot, cancellationToken).ConfigureAwait(false)) return new TSaveData();
        var prepared = await PrepareLoadAsync(slot, cancellationToken).ConfigureAwait(false);
        if (prepared.RequiresCommit) throw new SavePreparationRequiredException<TSaveData>(prepared);
        return prepared.Data;
    }

    /// <summary>Creates a new run's empty mod snapshot, independent of any existing slot or prior run.</summary>
    /// <returns>A game-owned draft for a new session.</returns>
    public ModSaveSession CreateModState() => new(_owner, new Dictionary<string, SaveDataBlock>(StringComparer.Ordinal), _modPolicies, _codec);

    /// <inheritdoc />
    public Task SaveAsync(int slot, TSaveData data, CancellationToken cancellationToken = default) =>
        SaveCoreAsync(slot, data, null, cancellationToken);

    /// <summary>Atomically writes game state and an explicitly captured mod draft together.</summary>
    /// <param name="slot">Target slot.</param>
    /// <param name="data">Current validated game DTO.</param>
    /// <param name="modState">Complete run snapshot issued by this repository; loaded sessions retain their own unavailable mod blocks.</param>
    /// <param name="cancellationToken">Cancels admission; accepted atomic writes are joined.</param>
    /// <returns>Completion after the whole document is committed.</returns>
    public Task SaveDocumentAsync(int slot, TSaveData data, ModSaveSession modState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modState);
        if (modState.Owner != _owner) throw new ArgumentException("The mod draft belongs to another repository.", nameof(modState));
        return SaveCoreAsync(slot, data, modState.Snapshot(), cancellationToken);
    }

    /// <summary>Writes one run snapshot, or preserves slot mod blocks only for the game-only convenience API.</summary>
    private async Task SaveCoreAsync(int slot, TSaveData data, Dictionary<string, SaveDataBlock>? modState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = GetSlotStorage(slot);
            var backups = (IStorageBackup)storage;
            var document = new SaveDocument
            {
                GameId = _options.GameId,
                Game = new SaveDataBlock { Version = _options.CurrentVersion, Data = _codec.SerializeData(data) },
                Content = GetCurrentCatalog()?.Snapshot
            };
            if (modState is not null)
            {
                // A session owns one run's entire mod snapshot, including its unavailable owners. The target slot may
                // contain another run, so merging any of its blocks would resurrect unrelated world progress.
                document.Mods = modState.ToDictionary(static pair => pair.Key, static pair => pair.Value.Clone(), StringComparer.Ordinal);
            }
            else if (await storage.ExistsAsync(_configuration.SaveFileName).ConfigureAwait(false))
            {
                // The game-only convenience API retains this slot's mod blocks. New runs use an explicit fresh session.
                var previous = ReadDocument(await backups.ReadSnapshotAsync(_configuration.SaveFileName, cancellationToken).ConfigureAwait(false));
                document.Mods = previous.Mods;
            }
            ValidateDocumentForCurrentGame(document);
            var encoded = _codec.Encode(document);
            cancellationToken.ThrowIfCancellationRequested();
            await backups.WriteSnapshotAsync(_configuration.SaveFileName, encoded, cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int slot, CancellationToken cancellationToken = default)
    {
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await GetSlotStorage(slot).DeleteAsync(_configuration.SaveFileName).ConfigureAwait(false); }
        finally { gate.Release(); }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ListSlotsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directories = await _rootStorage.ListDirectoriesAsync().ConfigureAwait(false);
        var slots = new List<int>();
        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (directory.StartsWith(_configuration.SaveSlotPrefix, StringComparison.Ordinal) &&
                int.TryParse(directory[_configuration.SaveSlotPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var slot) &&
                await ExistsAsync(slot, cancellationToken).ConfigureAwait(false)) slots.Add(slot);
        }
        return slots.OrderBy(static slot => slot).ToArray();
    }

    /// <summary>Decodes outer metadata before any DTO or conversion callback can execute.</summary>
    private SaveDocument ReadDocument(StorageSnapshot snapshot)
    {
        var document = _codec.Decode(snapshot);
        if (!string.Equals(document.GameId, _options.GameId, StringComparison.Ordinal))
            throw new InvalidOperationException("This save belongs to another game.");
        if (document.Content is not null && _options.Content is null)
            throw new InvalidOperationException("A content-aware document requires an explicit current content policy.");
        return document;
    }

    /// <summary>Validates plain references first and current business data only after every document transformation.</summary>
    private TSaveData ValidateDocumentForCurrentGame(SaveDocument document)
    {
        if (document.FormatVersion != 1 || !string.Equals(document.GameId, _options.GameId, StringComparison.Ordinal))
            throw new InvalidOperationException("Recovery cannot change the envelope format or game identity.");
        if (document.Game.Version != _options.CurrentVersion) throw new InvalidOperationException("Wrong current game schema.");
        foreach (var policy in _modPolicies.Values)
            if (document.Mods.TryGetValue(policy.ModId, out var block) && block.Version != policy.CurrentVersion)
                throw new InvalidOperationException("Mod data must be prepared before saving: " + policy.ModId);
        var catalog = GetCurrentCatalog();
        if (catalog is not null)
            foreach (var reference in EnumerateReferences(document))
                if (!catalog.Contains(reference)) throw new InvalidOperationException("Prepared save references missing content: " + reference.Id);
        var data = _codec.DeserializeData<TSaveData>(document.Game.Data);
        _options.Validate?.Invoke(data);
        return data;
    }

    /// <summary>Gets the immutable startup catalog; changing the session invalidates preparation.</summary>
    private ContentCatalog? GetCurrentCatalog() => _options.Content is null ? null :
        _options.Content.CurrentCatalog() ?? throw new InvalidOperationException("The current content catalog is unavailable.");

    /// <summary>Creates a slot scope after rejecting negative or malformed layouts at the storage boundary.</summary>
    private IStorage GetSlotStorage(int slot)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        return new ScopedStorage(_rootStorage, _configuration.SaveSlotPrefix + slot.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Gets the repository-lifetime operation gate for one slot.</summary>
    private SemaphoreSlim GetSlotGate(int slot)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        return _slotGates.GetOrAdd(slot, static _ => new SemaphoreSlim(1, 1));
    }
}

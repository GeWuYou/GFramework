// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using Arch.Core;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch;

/// <summary>Describes one DI-owned world and an explicit ordered system participant.</summary>
/// <param name="options">World capacity and startup order.</param>
/// <param name="enabled">Whether this composition includes ECS.</param>
public sealed class ArchEcsModule(ArchOptions? options = null, bool enabled = true) : IArchEcsModule
{
    private readonly ArchOptions _options = options ?? new ArchOptions();
    private EcsLifetime? _runtime;
    private bool _configured;

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (_configured)
            throw new InvalidOperationException("An ECS module can only describe one architecture.");
        _configured = true;
        if (!enabled)
            return;
        services.AddSingleton<IArchEcsModule>(this);
        services.AddSingleton(_ => World.Create(_options.WorldCapacity));
        services.AddComponent(provider =>
        {
            _runtime = new EcsLifetime(provider.GetServices<ArchSystemAdapter<float>>()
                .OrderBy(system => system is IPrioritized prioritized ? prioritized.Priority : 0).ToArray());
            return _runtime;
        }, order: _options.Priority);
    }

    /// <inheritdoc />
    public void Update(float deltaTime) => _runtime?.Update(deltaTime);

    /// <summary>Starts and stops systems, leaving world/system final disposal to their single DI owner.</summary>
    /// <param name="systems">The explicitly registered systems in stable priority order.</param>
    private sealed class EcsLifetime(IReadOnlyList<ArchSystemAdapter<float>> systems) : ILifecycle
    {
        private readonly List<ArchSystemAdapter<float>> _started = [];
        private bool _ready;

        /// <inheritdoc />
        public void Initialize()
        {
            foreach (var system in systems)
            {
                // Partial initialization still owes the system its business-stop callback.
                _started.Add(system);
                system.Initialize();
            }
            _ready = true;
        }

        /// <inheritdoc />
        public void Destroy()
        {
            _ready = false;
            var errors = new List<Exception>();
            for (var index = _started.Count - 1; index >= 0; index--)
            {
                try
                {
                    _started[index].Destroy();
                }
                catch (Exception error) { errors.Add(error); }
            }
            _started.Clear();
            if (errors.Count > 0)
                throw new AggregateException("ECS system stop failed.", errors);
        }

        /// <summary>Only initialized systems participate in frame updates.</summary>
        /// <param name="deltaTime">The owning host's time step.</param>
        public void Update(float deltaTime)
        {
            if (!_ready)
                return;
            foreach (var system in _started)
                system.Update(in deltaTime);
        }
    }
}

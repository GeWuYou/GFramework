// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using GFramework.Ecs.Arch;

namespace GFramework.Ecs.Arch.Tests.Ecs;

/// <summary>Verifies the explicit IDisposable contract for adapters with derived finalizers.</summary>
[TestFixture]
[NonParallelizable]
public sealed class ArchSystemAdapterDisposalTests
{
    /// <summary>Successful explicit cleanup must prevent a derived finalizer from repeating resource cleanup.</summary>
    /// <param name="dispose">Whether explicit adapter cleanup runs before collection.</param>
    /// <param name="expectedFinalizations">The finalizer count, including the non-disposed positive control.</param>
    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public void Explicit_Dispose_Should_Suppress_Derived_Finalizer(bool dispose, int expectedFinalizations)
    {
        var finalized = 0;
        var reference = CreateAdapter(() => Interlocked.Increment(ref finalized), dispose);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(reference.IsAlive, Is.False, "The probe must actually become unreachable.");
        Assert.That(Volatile.Read(ref finalized), Is.EqualTo(expectedFinalizations));
    }

    // Keep the adapter outside the test stack so JIT local lifetime extension cannot hide a missing suppression.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAdapter(Action onFinalized, bool dispose)
    {
        using var world = global::Arch.Core.World.Create();
        var adapter = new FinalizableAdapter(world, onFinalized);
        if (dispose)
        {
            ((IDisposable)adapter).Dispose();
        }
        return new WeakReference(adapter);
    }

    /// <summary>Records only finalizer execution; it does not allocate an ECS World or bind an architecture.</summary>
    /// <param name="onFinalized">The observation retained by the test independently of this adapter.</param>
    private sealed class FinalizableAdapter(global::Arch.Core.World world, Action onFinalized) : ArchSystemAdapter<float>(world)
    {
#pragma warning disable MA0055 // A real derived finalizer is required to verify the GC.SuppressFinalize contract.
        /// <summary>Records the positive control; successfully disposed adapters must never reach this callback.</summary>
        ~FinalizableAdapter()
        {
            onFinalized();
        }
#pragma warning restore MA0055
    }
}

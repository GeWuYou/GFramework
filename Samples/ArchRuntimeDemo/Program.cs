// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using ArchRuntimeDemo;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Arch.Core;
using Arch.System;
using GFramework.Ecs.Arch;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Components;

// #region native-runtime
// Direct construction borrows the World/system; their using declarations are the sole final owners.
using var world = World.Create();
using var movement = new NativeMovement(world);
var runtime = new ArchRuntime(world, [movement], new ArchOptions { EnableStatistics = true });
try
{
    runtime.Initialize();
    var entity = world.Create(new Position(0, 0), new Velocity(10, 5));
    runtime.Update(0.5f);
    var position = world.Get<Position>(entity);
    if (position.X != 5 || position.Y != 2.5f || movement.BeforeCalls != 1 || movement.AfterCalls != 1)
        throw new InvalidOperationException("Native phases or entity movement failed.");
    var statistics = runtime.GetStatistics()!;
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"ARCH_RUNTIME_PASS entities={statistics.EntityCount} frames={statistics.CompletedFrames} systems={statistics.SystemCount}"));
}
finally
{
    runtime.Stop();
}
// #endregion native-runtime

if (args.Contains("--benchmark", StringComparer.Ordinal))
{
    foreach (var enabled in new[] { false, true })
    {
        using var benchmarkWorld = World.Create();
        using var system = new EmptySystem(benchmarkWorld);
        var benchmark = new ArchRuntime(benchmarkWorld, [system], new ArchOptions { EnableStatistics = enabled });
        benchmark.Initialize();
        for (var index = 0; index < 10000; index++)
            benchmark.Update(0.016f);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < 100000; index++)
            benchmark.Update(0.016f);
        var elapsed = Stopwatch.GetElapsedTime(started);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"statistics={enabled} ns/frame={elapsed.TotalNanoseconds / 100000:F1} allocated={allocated}"));
        benchmark.Stop();
    }
}

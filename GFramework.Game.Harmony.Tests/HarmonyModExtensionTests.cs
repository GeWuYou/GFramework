// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.IO;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Modding;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Runtime;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace GFramework.Game.Harmony.Tests;

/// <summary>Exercises real method replacement and independently owned removal through the optional adapter.</summary>
[TestFixture]
[NonParallelizable]
public sealed class HarmonyModExtensionTests
{
    /// <summary>Stopping one owner preserves a second owner's patch on the same method.</summary>
    [Test]
    public async Task TwoOwners_CoexistAndUnpatchIndependently()
    {
        var extension = new HarmonyModExtension();
        await using var first = extension.CreateScope(new TestContext("first"));
        await using var second = extension.CreateScope(new TestContext("second"));
        var firstPatches = (IModPatchService)first.GetService(typeof(IModPatchService))!;
        var secondPatches = (IModPatchService)second.GetService(typeof(IModPatchService))!;
        var target = Method(nameof(Score));
        firstPatches.Patch(target, postfix: new ModPatchMethod(Method(nameof(AddOne))) { Priority = 123, Before = new[] { "other-owner" } });
        secondPatches.Patch(target, postfix: new ModPatchMethod(Method(nameof(AddTen))));
        Assert.That(Score(), Is.EqualTo(21));
        var diagnostics = firstPatches.GetPatches();
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].OwnerId, Is.EqualTo(HarmonyModExtension.GetOwnerId("test", "first")));
        Assert.That(diagnostics[0].Kind, Is.EqualTo("Postfix"));
        Assert.That(diagnostics[0].Priority, Is.EqualTo(123));
        Assert.That(diagnostics[0].Before, Is.EqualTo(new[] { "other-owner" }));
        firstPatches.UnpatchAll();
        Assert.That(Score(), Is.EqualTo(20));
        Assert.That(secondPatches.GetPatches(), Has.Count.EqualTo(1));
        secondPatches.UnpatchAll();
        Assert.That(Score(), Is.EqualTo(10));
    }

    /// <summary>A rejected patch removes that owner's previous changes while preserving other owners.</summary>
    [Test]
    public async Task FailedPatch_RollsBackItsOwnerOnly()
    {
        var extension = new HarmonyModExtension();
        await using var first = extension.CreateScope(new TestContext("failure"));
        await using var other = extension.CreateScope(new TestContext("other"));
        var patches = (IModPatchService)first.GetService(typeof(IModPatchService))!;
        var others = (IModPatchService)other.GetService(typeof(IModPatchService))!;
        patches.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(AddOne))));
        others.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(AddTen))));
        Assert.Catch<Exception>(() => patches.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(InvalidPostfix)))));
        Assert.That(Score(), Is.EqualTo(20));
        Assert.That(patches.GetPatches(), Is.Empty);
        Assert.Throws<InvalidOperationException>(() => patches.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(AddOne)))));
    }

    /// <summary>The adapter passes through native prefix, transpiler and finalizer contracts.</summary>
    [Test]
    public async Task ManualPatch_SupportsPrefixTranspilerAndFinalizer()
    {
        await using var scope = new HarmonyModExtension().CreateScope(new TestContext("variants"));
        var patches = (IModPatchService)scope.GetService(typeof(IModPatchService))!;
        patches.Patch(Method(nameof(Score)), transpiler: new ModPatchMethod(Method(nameof(ReplaceTen))));
        Assert.That(Score(), Is.EqualTo(30));
        patches.UnpatchAll();
        patches.Patch(Method(nameof(Score)), prefix: new ModPatchMethod(Method(nameof(SkipOriginal))));
        Assert.That(Score(), Is.EqualTo(42));
        patches.Patch(Method(nameof(Throwing)), finalizer: new ModPatchMethod(Method(nameof(SuppressError))));
        Assert.DoesNotThrow(Throwing);
    }

    /// <summary>Managed patch state is scoped and serialized, rather than incorrectly pinned to a native thread.</summary>
    [Test]
    public async Task Scope_CanPatchAndDisposeAcrossAsyncThreads()
    {
        var scope = new HarmonyModExtension().CreateScope(new TestContext("cross-thread"));
        var patches = (IModPatchService)scope.GetService(typeof(IModPatchService))!;
        await Task.Run(() => patches.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(AddOne))))).ConfigureAwait(false);
        Assert.That(Score(), Is.EqualTo(11));
        await Task.Run(async () => await scope.DisposeAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(Score(), Is.EqualTo(10));
        Assert.Throws<ObjectDisposedException>(() => patches.Patch(Method(nameof(Score)), postfix: new ModPatchMethod(Method(nameof(AddOne)))));
    }

    /// <summary>An engine-owned adapter never loads a second patch library; generated proxy dependencies find the Default engine.</summary>
    [Test]
    public async Task CustomAdapterContext_UsesDefaultEngineForRealPatches()
    {
        var context = new AdapterLoadContext();
        using var source = File.OpenRead(typeof(HarmonyModExtension).Assembly.Location);
        var adapter = context.LoadFromStream(source);
        Assert.That(adapter.Location, Is.Empty, "The fixture reproduces Godot's in-memory assembly loading.");
        var extensionType = adapter.GetType(typeof(HarmonyModExtension).FullName!, true)!;
        var extension = (IGameModExtension)Activator.CreateInstance(extensionType, new object?[] { null })!;
        var engine = extension.SharedAssemblies.Single(static assembly => string.Equals(assembly.GetName().Name, "0Harmony", StringComparison.Ordinal));
        Assert.That(AssemblyLoadContext.GetLoadContext(engine), Is.SameAs(AssemblyLoadContext.Default));
        Assert.That(context.Assemblies.Any(static assembly => string.Equals(assembly.GetName().Name, "0Harmony", StringComparison.Ordinal)), Is.False);
        var scope = extension.CreateScope(new TestContext("custom-adapter"));
        await using var cleanup = scope.ConfigureAwait(false);
        var serviceType = adapter.GetType(typeof(IModPatchService).FullName!, true)!;
        var service = scope.GetService(serviceType)!;
        var declarationType = adapter.GetType(typeof(ModPatchMethod).FullName!, true)!;
        var declaration = Activator.CreateInstance(declarationType, Method(nameof(AddOne)))!;
        serviceType.GetMethod("Patch")!.Invoke(service,
            new object?[] { Method(nameof(Score)), null, declaration, null, null });
        Assert.That(Score(), Is.EqualTo(11));
        await scope.DisposeAsync().ConfigureAwait(false);
        Assert.That(Score(), Is.EqualTo(10));
    }

    /// <summary>A host-owned path cannot substitute another library or use implicit relative file lookup.</summary>
    [Test]
    public void EnginePath_RequiresPinnedAbsoluteAssembly()
    {
        Assert.Throws<ArgumentException>(() => new HarmonyModExtension("0Harmony.dll"));
        Assert.Throws<InvalidOperationException>(() => new HarmonyModExtension(typeof(HarmonyModExtension).Assembly.Location));
    }

    /// <summary>Simulates a host that would otherwise load a private engine when a static reference requests it.</summary>
    private sealed class AdapterLoadContext : AssemblyLoadContext
    {
        /// <summary>Creates a distinct in-memory host context.</summary>
        internal AdapterLoadContext() : base("HarmonyAdapter:" + Guid.NewGuid().ToString("N"), isCollectible: false) { }
        /// <inheritdoc />
        protected override Assembly? Load(AssemblyName assemblyName) =>
            string.Equals(assemblyName.Name, "0Harmony", StringComparison.Ordinal)
                ? LoadFromAssemblyPath(typeof(HarmonyLib.Harmony).Assembly.Location)
                : null;
    }

    /// <summary>The extension shares exactly the adapter and real Harmony libraries with mod contexts.</summary>
    [Test]
    public void Extension_DeclaresSharedLibraryAndCollisionFreeOwner()
    {
        var extension = new HarmonyModExtension();
        Assert.That(extension.Capability, Is.EqualTo("harmony"));
        Assert.That(extension.SharedAssemblies, Does.Contain(typeof(HarmonyLib.Harmony).Assembly));
        Assert.That(extension.SharedAssemblies, Does.Contain(typeof(IModPatchService).Assembly));
        Assert.That(typeof(HarmonyModExtension).Assembly.GetReferencedAssemblies().Any(static name => string.Equals(name.Name, "0Harmony", StringComparison.Ordinal)), Is.False,
            "The bridge must not statically bind the engine to an arbitrary engine-owned ALC.");
        Assert.That(HarmonyModExtension.GetOwnerId("a:b", "c"), Is.Not.EqualTo(HarmonyModExtension.GetOwnerId("a", "b:c")));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Score() => 10;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throwing() => throw new InvalidOperationException("fixture");
    private static void AddOne(ref int __result) => __result++;
    private static void AddTen(ref int __result) => __result += 10;
    private static void InvalidPostfix(string absentParameter) => _ = absentParameter;
    private static bool SkipOriginal(ref int __result) { __result = 42; return false; }
    private static Exception? SuppressError(Exception __exception) => null;
    /// <summary>Changes one known IL constant to prove actual transpiler execution instead of inspecting registration.</summary>
    private static IEnumerable<CodeInstruction> ReplaceTen(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S && Convert.ToInt32(instruction.operand, System.Globalization.CultureInfo.InvariantCulture) == 10)
                instruction.operand = (sbyte)30;
            yield return instruction;
        }
    }
    private static MethodInfo Method(string name) => typeof(HarmonyModExtensionTests).GetMethod(name,
        BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>Provides the minimum host contract needed to create independent real patch scopes.</summary>
    private sealed class TestContext(string id) : IGameModContext
    {
        /// <inheritdoc />
        public string GameId => "test";
        /// <inheritdoc />
        public ContentPackStamp Package { get; } = new(id, "1.0.0", "fingerprint");
        /// <inheritdoc />
        public IServiceProvider Services { get; } = new EmptyServices();
        /// <inheritdoc />
        public IGameDispatcher Dispatcher { get; } = new InlineGameDispatcher();
        /// <inheritdoc />
        public ILogger Logger => NullLogger.Instance;
        /// <inheritdoc />
        public void Own(IDisposable resource) { }
        /// <inheritdoc />
        public void OwnAsync(IAsyncDisposable resource) { }
        /// <inheritdoc />
        public void OwnRegistration(IUnRegister registration) { }
    }
    /// <summary>Ensures these tests do not depend on game service registration.</summary>
    private sealed class EmptyServices : IServiceProvider
    {
        /// <inheritdoc />
        public object? GetService(Type serviceType) => null;
    }
}

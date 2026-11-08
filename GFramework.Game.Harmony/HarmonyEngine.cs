// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace GFramework.Game.Harmony;

/// <summary>Uses one pinned Default-ALC patch engine through its public API, independent of the engine host's own ALC.</summary>
/// <remarks>MonoMod creates helper assemblies with Assembly.Load(byte[]), whose dependencies resolve through Default.
/// Keeping the engine there avoids duplicate generic constraint identities in self-contained engine exports.
/// The adapter deliberately uses no statically typed Harmony classes, so it cannot bind an engine-private copy first.</remarks>
internal sealed class HarmonyEngine
{
    private static readonly Version RequiredVersion = new(2, 4, 3, 0);
    private readonly ConstructorInfo _harmonyConstructor;
    private readonly ConstructorInfo _methodConstructor;
    private readonly MethodInfo _patchAll;
    private readonly MethodInfo _patch;
    private readonly MethodInfo _unpatchAll;
    private readonly MethodInfo _allPatchedMethods;
    private readonly MethodInfo _patchInfo;
    private readonly Type _patchesType;
    private readonly Type _patchType;
    private readonly Type _methodType;

    /// <summary>Loads a pinned, host-owned engine and binds only its documented public API.</summary>
    internal HarmonyEngine(string? assemblyPath)
    {
        Assembly = LoadEngine(assemblyPath);
        var harmonyType = Assembly.GetType("HarmonyLib.Harmony", throwOnError: true)!;
        _methodType = Assembly.GetType("HarmonyLib.HarmonyMethod", throwOnError: true)!;
        _patchesType = Assembly.GetType("HarmonyLib.Patches", throwOnError: true)!;
        _patchType = Assembly.GetType("HarmonyLib.Patch", throwOnError: true)!;
        _harmonyConstructor = harmonyType.GetConstructor(new[] { typeof(string) })!;
        _methodConstructor = _methodType.GetConstructor(new[] { typeof(MethodInfo) })!;
        _patchAll = harmonyType.GetMethod("PatchAll", new[] { typeof(Assembly) })!;
        _patch = harmonyType.GetMethod("Patch", new[] { typeof(MethodBase), _methodType, _methodType, _methodType, _methodType })!;
        _unpatchAll = harmonyType.GetMethod("UnpatchAll", new[] { typeof(string) })!;
        _allPatchedMethods = harmonyType.GetMethod("GetAllPatchedMethods", Type.EmptyTypes)!;
        _patchInfo = harmonyType.GetMethod("GetPatchInfo", new[] { typeof(MethodBase) })!;
    }
    /// <summary>Gets the single Default-ALC engine assembly shared with all code mods.</summary>
    internal Assembly Assembly { get; }
    /// <summary>Creates an owner instance without applying any patches.</summary>
    internal object CreateOwner(string ownerId) => Construct(_harmonyConstructor, new object?[] { ownerId });
    /// <summary>Applies explicit assembly annotations in the Default-owned engine.</summary>
    internal void PatchAll(object owner, Assembly assembly) => Invoke(_patchAll, owner, new object?[] { assembly });
    /// <summary>Converts neutral declarations to native engine objects inside the bridge.</summary>
    internal MethodInfo? Patch(object owner, MethodBase original, ModPatchMethod? prefix, ModPatchMethod? postfix,
        ModPatchMethod? transpiler, ModPatchMethod? finalizer) =>
        (MethodInfo?)Invoke(_patch, owner, new[] { original, CreateMethod(prefix), CreateMethod(postfix), CreateMethod(transpiler), CreateMethod(finalizer) });
    /// <summary>Removes exactly one owner, never every Harmony user's patches.</summary>
    internal void UnpatchAll(object owner, string ownerId) => Invoke(_unpatchAll, owner, new object?[] { ownerId });
    /// <summary>Projects public engine diagnostics into adapter-owned records.</summary>
    internal IReadOnlyList<ModPatchDiagnostic> GetPatches(string ownerId)
    {
        var result = new List<ModPatchDiagnostic>();
        foreach (var target in ((IEnumerable)Invoke(_allPatchedMethods, null, Array.Empty<object?>())!).Cast<MethodBase>())
        {
            var info = Invoke(_patchInfo, null, new object?[] { target });
            if (info is null) continue;
            AddDiagnostics(result, target, info, ownerId, "Prefix", "Prefixes");
            AddDiagnostics(result, target, info, ownerId, "Postfix", "Postfixes");
            AddDiagnostics(result, target, info, ownerId, "Transpiler", "Transpilers");
            AddDiagnostics(result, target, info, ownerId, "Finalizer", "Finalizers");
        }
        return result.AsReadOnly();
    }

    /// <summary>Loads only the supported engine identity into Default, even when this adapter was loaded from memory.</summary>
    private static Assembly LoadEngine(string? assemblyPath)
    {
        if (assemblyPath is not null)
        {
            if (!Path.IsPathFullyQualified(assemblyPath))
                throw new ArgumentException("The host Harmony assembly path must be absolute.", nameof(assemblyPath));
            ValidateEngine(AssemblyName.GetAssemblyName(assemblyPath));
        }
        var existing = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(static assembly =>
            string.Equals(assembly.GetName().Name, "0Harmony", StringComparison.Ordinal));
        if (existing is not null)
        {
            ValidateEngine(existing.GetName());
            return existing;
        }
        Assembly engine;
        if (assemblyPath is not null) engine = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        else
        {
            try { engine = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("0Harmony")); }
            catch (FileNotFoundException exception)
            {
                var candidate = Path.Combine(AppContext.BaseDirectory, "0Harmony.dll");
                if (!File.Exists(candidate))
                    throw new FileNotFoundException("The host must distribute Lib.Harmony 2.4.3 for the Default load context. " +
                        "Provide an absolute 0Harmony.dll path to HarmonyModExtension when it is not beside the application.", candidate, exception);
                ValidateEngine(AssemblyName.GetAssemblyName(candidate));
                engine = AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate);
            }
        }
        ValidateEngine(engine.GetName());
        return engine;
    }
    /// <summary>Rejects a conflicting process-wide engine before any native patch is registered.</summary>
    private static void ValidateEngine(AssemblyName name)
    {
        if (!string.Equals(name.Name, "0Harmony", StringComparison.Ordinal) || name.Version != RequiredVersion)
            throw new InvalidOperationException("Code mods require Lib.Harmony 2.4.3 (0Harmony assembly 2.4.3.0) in the Default load context; " +
                "another engine version cannot be substituted inside the same process.");
    }
    /// <summary>Applies only supplied overrides, retaining Harmony attributes when declarations omit them.</summary>
    private object? CreateMethod(ModPatchMethod? declaration)
    {
        if (declaration is null) return null;
        ArgumentNullException.ThrowIfNull(declaration.Before, nameof(declaration));
        ArgumentNullException.ThrowIfNull(declaration.After, nameof(declaration));
        var method = Construct(_methodConstructor, new object?[] { declaration.Method });
        if (declaration.Priority.HasValue) _methodType.GetField("priority")!.SetValue(method, declaration.Priority.Value);
        if (declaration.Before.Count != 0) _methodType.GetField("before")!.SetValue(method, declaration.Before.ToArray());
        if (declaration.After.Count != 0) _methodType.GetField("after")!.SetValue(method, declaration.After.ToArray());
        return method;
    }
    /// <summary>Reads documented public Patch/Patches fields without modifying library internals or caches.</summary>
    private void AddDiagnostics(List<ModPatchDiagnostic> result, MethodBase target, object info, string ownerId,
        string kind, string collection)
    {
        var patches = (IEnumerable)_patchesType.GetField(collection)!.GetValue(info)!;
        foreach (var patch in patches)
        {
            if (!string.Equals((string?)_patchType.GetField("owner")!.GetValue(patch), ownerId, StringComparison.Ordinal)) continue;
            var patchMethod = (MethodInfo)_patchType.GetProperty("PatchMethod")!.GetValue(patch)!;
            var priority = (int)_patchType.GetField("priority")!.GetValue(patch)!;
            var before = (string[])_patchType.GetField("before")!.GetValue(patch)!;
            var after = (string[])_patchType.GetField("after")!.GetValue(patch)!;
            result.Add(new ModPatchDiagnostic(Describe(target), Describe(patchMethod), ownerId, kind, priority,
                Array.AsReadOnly((string[])before.Clone()), Array.AsReadOnly((string[])after.Clone())));
        }
    }
    /// <summary>Creates a readable target including overload parameter types.</summary>
    private static string Describe(MethodBase method) => (method.DeclaringType?.FullName ?? "<global>") + "." + method.Name +
        "(" + string.Join(", ", method.GetParameters().Select(static parameter => parameter.ParameterType.FullName)) + ")";
    /// <summary>Preserves the actual engine exception and stack instead of exposing reflection wrappers.</summary>
    private static object? Invoke(MethodInfo method, object? owner, object?[] arguments)
    {
        try { return method.Invoke(owner, arguments); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
    /// <summary>Constructs only known public engine types while preserving underlying failures.</summary>
    private static object Construct(ConstructorInfo constructor, object?[] arguments)
    {
        try { return constructor.Invoke(arguments); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}

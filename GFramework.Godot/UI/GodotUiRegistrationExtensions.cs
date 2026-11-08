// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;
using GFramework.Game.UI;

namespace GFramework.Godot.UI;

/// <summary>Registers ordinary Control factories and PackedScene resources without generated page-provider code.</summary>
public static class GodotUiRegistrationExtensions
{
    /// <summary>Registers a native view factory with strongly typed route arguments and instance actions.</summary>
    /// <typeparam name="TArgs">Business argument type.</typeparam>
    /// <param name="registry">Service route registry.</param>
    /// <param name="route">Definition shared with callers.</param>
    /// <param name="create">Creates an unparented Control and binds business arguments.</param>
    public static void RegisterGodot<TArgs>(this UiRegistry registry, UiRoute<TArgs> route,
        Func<TArgs, UiViewContext, Control> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        registry.Register(route, (args, context, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IUiView>(new GodotUiView(create(args, context), route.Options));
        });
    }
    /// <summary>Registers a typed native dialog.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <typeparam name="TResult">Submitted business result type.</typeparam>
    /// <param name="registry">Service route registry.</param>
    /// <param name="route">Dialog definition.</param>
    /// <param name="create">Creates a detached Control connected to its typed completion context.</param>
    public static void RegisterGodotDialog<TArgs, TResult>(this UiRegistry registry, UiDialogRoute<TArgs, TResult> route,
        Func<TArgs, UiDialogContext<TResult>, Control> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        registry.Register(route, (args, context, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IUiView>(new GodotUiView(create(args, context), route.Options));
        });
    }
    /// <summary>Registers a plain Control scene. Binding errors reclaim the newly instantiated node.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <param name="registry">Service route registry.</param>
    /// <param name="route">Route definition.</param>
    /// <param name="scene">Packed Control scene.</param>
    /// <param name="bind">Optional business binding after instantiation and before mounting.</param>
    public static void RegisterGodotScene<TArgs>(this UiRegistry registry, UiRoute<TArgs> route, PackedScene scene,
        Action<Control, TArgs, UiViewContext>? bind = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        registry.RegisterGodot(route, (args, context) =>
        {
            var node = scene.Instantiate();
            try
            {
                if (node is not Control control)
                    throw new InvalidOperationException($"UI route '{route.Key}' requires a Control scene root.");
                bind?.Invoke(control, args, context);
                return control;
            }
            catch { node.Free(); throw; }
        });
    }
}

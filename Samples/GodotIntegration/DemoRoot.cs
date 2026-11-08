// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Input;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;
using GFramework.Game.Data;
using GFramework.Game.Runtime;
using GFramework.Game.UI;
using GFramework.Godot.Config;
using GFramework.Godot.Input;
using GFramework.Godot.Pause;
using GFramework.Godot.Storage;
using GFramework.Godot.UI;
using Godot;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

using Microsoft.Extensions.DependencyInjection;

namespace GodotIntegration;

/// <summary>
/// Runnable sample composition root. It owns native roots and persistence, observes all async UI actions,
/// and awaits framework shutdown before asking Godot to quit. All tree work begins on the Godot thread;
/// awaits explicitly preserve that context because their continuations access native nodes and InputMap.
/// </summary>
public partial class DemoRoot : Node, ISceneRoot
{
    private readonly DemoSettingsLocation _settingsLocation = new();
    private readonly CancellationTokenSource _startupCancellation = new();
    private Task _startupTask = Task.CompletedTask;
    private Task _actionTask = Task.CompletedTask;
    private string _dataRootPath = "user://runtime_sample";
    private bool _smoke;
    private bool _smokeContinue;
    private string _smokeDataId = string.Empty;
    private int _exitCode;
    private DemoArchitecture? _architecture;
    private GodotFileStorage? _storage;
    private SaveRepository<DemoSave> _saves = null!;
    private UnifiedSettingsDataRepository _settings = null!;
    private GodotInputBindingStore _bindings = null!;
    private Node2D _sceneRoot = null!;
    private GodotUiRoot _uiRoot = null!;
    private Label _status = null!;
    private Label _hud = null!;
    private bool _ready;
    private bool _busy;
    private bool _awaitingBinding;
    private bool _stopping;

    /// <summary>Gets the current gameplay DTO; starting a new game does not overwrite the existing slot.</summary>
    internal DemoSave Save { get; private set; } = new();

    /// <summary>Gets the settings section shared by the views and gameplay.</summary>
    internal DemoPreferences Preferences { get; private set; } = new();

    /// <summary>Gets the generated configuration entry loaded from the shipped YAML.</summary>
    internal GameplayConfig Gameplay { get; private set; } = null!;

    /// <summary>Gets the initialized scene router belonging to this host.</summary>
    private ISceneRouter Scenes => _architecture!.Services.GetRequiredService<ISceneRouter>();

    /// <summary>Gets the ordinary UI service belonging to this host.</summary>
    private IUiService Ui => _architecture!.Services.GetRequiredService<IUiService>();

    /// <summary>Builds native roots, loads data, then makes the menu available after Architecture is ready.</summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        if (!TryConfigureSmoke(OS.GetCmdlineUserArgs(), out var argumentError))
        {
            GD.PrintErr("GFRAMEWORK_SMOKE_FAILURE " + argumentError);
            GD.Print("GFRAMEWORK_SMOKE_RESULT FAIL");
            GetTree().Quit(2);
            return;
        }
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += RequestQuit;
        CreateRoots();
        // Keep startup observable so a window-close request can cancel and join it before releasing the host.
        _startupTask = StartAsync();
        if (_smoke)
            RunSmoke();
    }

    /// <summary>Loads the composition asynchronously while keeping the scene tree available for graceful shutdown.</summary>
    private async Task StartAsync()
    {
        try
        {
            var registry = new ConfigRegistry();
            var configLoader = new GodotYamlConfigLoader(new GodotYamlConfigLoaderOptions
            {
                SourceRootPath = "res://",
                RuntimeCacheRootPath = _dataRootPath + "/config_cache",
                TableSources = [.. GeneratedConfigCatalog.Tables.Select(static entry =>
                    new GodotYamlConfigTableSource(entry.TableName, entry.ConfigRelativePath, entry.SchemaRelativePath))],
                ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables(),
            });
            await configLoader.LoadAsync(registry, _startupCancellation.Token).ConfigureAwait(true);
            Gameplay = registry.GetTable<int, GameplayConfig>("gameplay").Get(1);

            var serializer = new JsonSerializer();
            _storage = new GodotFileStorage(serializer);
            _saves = new SaveRepository<DemoSave>(_storage, new SaveConfiguration
            {
                SaveRoot = _dataRootPath + "/saves",
            }, new SaveRepositoryOptions<DemoSave> { GameId = "godot-integration", CurrentVersion = 1 });
            _settings = new UnifiedSettingsDataRepository(_storage, serializer,
                new DataRepositoryOptions { BasePath = _dataRootPath, EnableEvents = false });
            _settings.RegisterDataType(_settingsLocation, typeof(DemoPreferences));
            Preferences = await _settings.LoadAsync<DemoPreferences>(_settingsLocation, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
            _startupCancellation.Token.ThrowIfCancellationRequested();
            if (Preferences.Version != 1)
                throw new InvalidOperationException("Unsupported settings version; existing data has been preserved.");

            _bindings = new GodotInputBindingStore();
            if (Preferences.Bindings is not null)
                _bindings.ImportSnapshot(Preferences.Bindings);
            var context = SynchronizationContext.Current
                ?? throw new InvalidOperationException("Godot startup requires the native synchronization context.");
            _architecture = new DemoArchitecture(this, CreateUiRegistry(), _uiRoot,
                new SynchronizationContextGameDispatcher(context));
            await _architecture.InitializeAsync(_startupCancellation.Token).ConfigureAwait(true);
            _architecture.Services.GetRequiredService<IPauseStackManager>().RegisterHandler(new GodotPauseHandler(GetTree()));
            await Ui.ReplaceAsync(DemoUiRoutes.Menu, "GFramework runtime sample", _startupCancellation.Token)
                .ConfigureAwait(true);
            InstallUiInputBridge();
            _ready = true;
            _status.Text = "Ready. New game replaces slot 1 only when you explicitly save.";
        }
        catch (OperationCanceledException) when (_startupCancellation.IsCancellationRequested)
        {
            // A normal window close joins this startup task before disposing the architecture and storage.
        }
        catch (Exception error)
        {
            ReportError(error);
        }
    }

    /// <summary>Connects native unhandled Back to the service and a gameplay-only pause fallback.</summary>
    private void InstallUiInputBridge()
    {
        var bridge = new GodotUiInputBridge
        {
            Name = "UiInput",
            Service = Ui,
            CancelAction = "demo_pause",
            RunAction = RunAction,
            UnhandledCancelAsync = () => Scenes.Current is null
                ? Task.CompletedTask
                : Ui.ShowAsync(DemoUiRoutes.Pause, "Paused", cancellationToken: global::System.Threading.CancellationToken.None),
        };
        AddChild(bridge);
    }

    /// <summary>Captures rebinding before native GUI handling; the default bridge owns unhandled pause/back.</summary>
    /// <param name="inputEvent">Input received on the Godot thread.</param>
    public override void _Input(InputEvent inputEvent)
    {
        if (!_ready || _busy || _stopping)
            return;
        _bindings.UpdateDeviceFromInput(inputEvent);
        if (_awaitingBinding && inputEvent is InputEventKey { Pressed: true, Echo: false } key)
        {
            var physicalKey = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            _bindings.SetPrimaryBinding("demo_pause", new InputBindingDescriptor(
                InputDeviceKind.KeyboardMouse, InputBindingKind.Key,
string.Create(CultureInfo.InvariantCulture, $"key:{(int)physicalKey}"), physicalKey.ToString()));
            _awaitingBinding = false;
            GetViewport().SetInputAsHandled();
            _status.Text = $"Pause binding: {physicalKey}. Save settings to keep it after restart.";
        }
    }

    /// <summary>Updates the gameplay status label from the active world.</summary>
    /// <param name="text">Current counters.</param>
    internal void SetHud(string text) => _hud.Text = text;

    /// <summary>Runs a button operation once and observes any failure instead of losing async exceptions.</summary>
    /// <param name="action">A host operation that may await persistence or navigation.</param>
    internal void RunAction(Func<Task> action)
    {
        if (_busy || _stopping)
            return;
        _busy = true;
        _actionTask = ObserveActionAsync(action);
    }

    /// <summary>Retains each action so shutdown can wait for an in-flight save or navigation operation.</summary>
    /// <param name="action">The accepted action to execute and observe.</param>
    private async Task ObserveActionAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            ReportError(error);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Registers ordinary Control factories once at the composition root.</summary>
    private UiRegistry CreateUiRegistry()
    {
        var registry = new UiRegistry();
        registry.RegisterGodot(DemoUiRoutes.Menu, (title, context) => CreatePanel("menu", title, context));
        registry.RegisterGodot(DemoUiRoutes.Pause, (title, context) => CreatePanel("pause", title, context));
        registry.RegisterGodot(DemoUiRoutes.Settings, (title, context) => CreatePanel("settings", title, context));
        return registry;
    }

    /// <summary>Builds a plain native view with typed title and instance-close context.</summary>
    /// <param name="key">Project-owned menu kind.</param>
    /// <param name="title">Visible heading supplied by the route caller.</param>
    /// <param name="context">Explicit identity and close action of this instance.</param>
    /// <returns>A detached view whose lifetime is transferred to the UI service.</returns>
    private DemoPanel CreatePanel(string key, string title, UiViewContext context)
    {
        if (key is not ("menu" or "pause" or "settings"))
            throw new ArgumentException("Unknown sample page.", nameof(key));
        var panel = new DemoPanel
        {
            InstanceHandle = context.Handle,
            ProcessMode = ProcessModeEnum.Always,
            Position = new Vector2(285, 135),
            CustomMinimumSize = new Vector2(390, 330),
        };
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        panel.AddChild(box);
        box.AddChild(new Label { Text = title });
        if (string.Equals(key, "menu", StringComparison.Ordinal))
        {
            AddButton(box, "New game", () => StartGameAsync(loadExisting: false));
            AddButton(box, "Continue slot 1", () => StartGameAsync(loadExisting: true));
            AddButton(box, "Settings", () => Ui.ShowAsync(DemoUiRoutes.Settings, "Settings", cancellationToken: global::System.Threading.CancellationToken.None));
            // Quit bypasses ordinary action suppression and never joins its own task.
            CreateButton(box, "Quit").Pressed += RequestQuit;
        }
        else if (string.Equals(key, "pause", StringComparison.Ordinal))
        {
            AddButton(box, "Resume", () => context.CloseAsync(cancellationToken: global::System.Threading.CancellationToken.None));
            AddButton(box, "Settings", () => Ui.ShowAsync(DemoUiRoutes.Settings, "Settings", cancellationToken: global::System.Threading.CancellationToken.None));
            AddButton(box, "Save slot 1", SaveGameAsync);
            AddButton(box, "Save and return to menu", async () =>
            {
                await SaveGameAsync().ConfigureAwait(true);
                await ReturnToMenuAsync().ConfigureAwait(true);
            });
            AddButton(box, "Return without saving", ReturnToMenuAsync);
        }
        else
        {
            var grid = new CheckButton { Text = "Show grid", ButtonPressed = Preferences.ShowGrid };
            grid.Toggled += show => Preferences.ShowGrid = show;
            box.AddChild(grid);
            AddButton(box, "Rebind pause: press a key", () =>
            {
                _awaitingBinding = true;
                _status.Text = "Press the new pause/back key.";
                return Task.CompletedTask;
            });
            AddButton(box, "Reset pause binding", () =>
            {
                _bindings.ResetAction("demo_pause");
                return Task.CompletedTask;
            });
            AddButton(box, "Save settings", SavePreferencesAsync);
            AddButton(box, "Back", () =>
            {
                _awaitingBinding = false;
                return context.CloseAsync(cancellationToken: global::System.Threading.CancellationToken.None);
            });
        }
        return panel;
    }

    /// <inheritdoc />
    public void AddScene(ISceneBehavior scene) => _sceneRoot.AddChild((Node)scene.Original);

    /// <inheritdoc />
    public void RemoveScene(ISceneBehavior scene)
    {
        var node = (Node)scene.Original;
        if (GodotObject.IsInstanceValid(node) && node.GetParent() == _sceneRoot)
            _sceneRoot.RemoveChild(node);
    }

    /// <summary>Creates world, overlay and labels before any asynchronous startup work begins.</summary>
    private void CreateRoots()
    {
        _sceneRoot = new Node2D { Name = "World", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(_sceneRoot);
        var canvas = new CanvasLayer { Name = "Overlay", Layer = 10 };
        AddChild(canvas);
        var labels = new Control { Name = "Labels", MouseFilter = Control.MouseFilterEnum.Ignore };
        labels.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(labels);
        _uiRoot = new GodotUiRoot { Name = "Ui", ProcessMode = ProcessModeEnum.Always };
        AddChild(_uiRoot);
        _hud = new Label { Position = new Vector2(20, 16), Text = "Move with WASD; collect the gold circles." };
        _status = new Label { Position = new Vector2(20, 48), Text = "Loading configuration and settings..." };
        labels.AddChild(_hud);
        labels.AddChild(_status);
    }

    /// <summary>Starts an unsaved run or loads a real slot, preserving corrupt data on load failure.</summary>
    private async Task StartGameAsync(bool loadExisting)
    {
        if (loadExisting && !await _saves.ExistsAsync(1, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true))
        {
            _status.Text = "Slot 1 does not exist yet. Start a new game and save first.";
            return;
        }
        Save = loadExisting ? await _saves.LoadAsync(1, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true) : new DemoSave();
        await Scenes.ReplaceAsync("play", cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        await Ui.ClearAsync(cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        var pauseBindings = _bindings.GetBindings("demo_pause").Bindings;
        _status.Text = "Pause/back: " + (pauseBindings.Count == 0 ? null : pauseBindings[0].DisplayName);
    }

    /// <summary>Saves only after an explicit player action; write failures keep the gameplay view open.</summary>
    private async Task SaveGameAsync()
    {
        Save.LastModified = DateTime.UtcNow;
        await _saves.SaveAsync(1, Save, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        _status.Text = "Slot 1 saved.";
    }

    /// <summary>Persists the settings DTO and input snapshot through the unified settings repository.</summary>
    private async Task SavePreferencesAsync()
    {
        Preferences.LastModified = DateTime.UtcNow;
        Preferences.Bindings = _bindings.ExportSnapshot();
        await _settings.SaveAsync(_settingsLocation, Preferences, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        _status.Text = "Settings saved.";
    }

    /// <summary>Removes modal pause owners before clearing the gameplay scene.</summary>
    private async Task ReturnToMenuAsync()
    {
        _awaitingBinding = false;
        await Ui.ClearAsync(cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        await Scenes.ClearAsync(cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        await Ui.ReplaceAsync(DemoUiRoutes.Menu, "GFramework runtime sample", cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        _hud.Text = "Move with WASD; collect the gold circles.";
    }

    /// <summary>
    /// Retains the first quit request even during an action. Repeated requests share the same shutdown;
    /// cancellation stops startup, while an already accepted save or navigation is allowed to finish.
    /// </summary>
    private async void RequestQuit()
    {
        if (_stopping)
            return;
        _stopping = true;
        try
        {
            _startupCancellation.Cancel();
            await _startupTask.ConfigureAwait(true);
            await _actionTask.ConfigureAwait(true);
            if (_architecture is not null)
                await _architecture.DestroyAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            _exitCode = 1;
            // This is the native signal boundary: observe failures even when shutdown cannot finish cleanly.
            GD.PushError(error.ToString());
        }
        finally
        {
            try
            {
                _storage?.Dispose();
                _startupCancellation.Dispose();
            }
            catch (Exception error)
            {
                _exitCode = 1;
                GD.PushError(error.ToString());
            }
            finally
            {
                if (_smoke)
                    GD.Print($"GFRAMEWORK_SMOKE_RESULT {(_exitCode == 0 ? "PASS" : "FAIL")}");
                GetTree().Quit(_exitCode);
            }
        }
    }

    /// <summary>Connects a synchronous Godot signal to the single observed async action boundary.</summary>
    private void AddButton(VBoxContainer parent, string text, Func<Task> action)
    {
        var button = CreateButton(parent, text);
        button.Pressed += () => RunAction(action);
    }

    /// <summary>Creates a project button so quit can use its dedicated shutdown signal boundary.</summary>
    /// <param name="parent">Container that owns the new button.</param>
    /// <param name="text">Visible action label.</param>
    /// <returns>A button already added to its container.</returns>
    private static Button CreateButton(VBoxContainer parent, string text)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(360, 36) };
        parent.AddChild(button);
        return button;
    }

    /// <summary>Displays a recoverable action failure and logs its details without replacing persisted data.</summary>
    private void ReportError(Exception error)
    {
        _status.Text = "Operation failed: " + error.Message;
        GD.PushError(error.ToString());
    }
}

// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Localization;
using GFramework.Godot.Setting.Data;
using Godot;
using FileAccess = Godot.FileAccess;

namespace DodgeTheCreeps;

/// <summary>
/// Composes the existing framework localization manager with Godot's locale bridge and packaged tables.
/// The application owns persistence; live views subscribe only while their native nodes remain alive.
/// All application and binding calls run on the Godot thread.
/// </summary>
internal sealed class DodgeLocalization : IDisposable
{
    private static readonly Regex Variables = new(@"\{(?<variable>[A-Za-z_]\w*)\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));
    private static readonly LocalizationMap Map = new(
        new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "en", ["zh-CN"] = "zh_CN" },
        new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "en", ["zh-CN"] = "zh-CN" });
    private LocalizationManager? _manager;

    /// <summary>Gets the effective supported locale, independent of an unsaved settings draft.</summary>
    internal string CurrentLanguage => _manager?.CurrentLanguage ?? "en";

    /// <summary>Gets whether the currently effective UI uses Simplified Chinese.</summary>
    internal bool IsChinese => string.Equals(CurrentLanguage, "zh-CN", StringComparison.Ordinal);

    /// <summary>Creates a caller-owned medium-weight variation of the shared, unmodified Chinese font file.</summary>
    /// <returns>A variation to dispose after the caller's native labels and themes stop using it.</returns>
    internal static FontVariation CreateChineseFont() => new()
    {
        BaseFont = GD.Load<Font>("res://fonts/NotoSansSC.ttf"),
        // Use the numeric OpenType tag: this engine's font cache ignores string-keyed
        // coordinates even though the resource accepts them as dictionary entries.
        VariationOpentype = new Godot.Collections.Dictionary { [TextServerManager.GetPrimaryInterface().NameToTag("wght")] = 500 }
    };

    /// <summary>Loads and validates packaged translations after Godot has initialized its filesystem.</summary>
    internal void Initialize()
    {
        if (_manager is not null) throw new InvalidOperationException("Localization is already initialized.");
        var english = FileAccess.GetFileAsString("res://Localization/en/ui.json");
        var chinese = FileAccess.GetFileAsString("res://Localization/zh-CN/ui.json");
        ValidateTables(english, chinese);
        // Core's existing manager loads physical JSON tables. A content-addressed, atomically written
        // cache makes those exact shipped resources available in both editor and exported games.
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(english + "\0" + chinese)));
        var root = ProjectSettings.GlobalizePath("user://dodge_localization/" + digest);
        Cache(root, "en", english);
        Cache(root, "zh-CN", chinese);
        _manager = new LocalizationManager(new LocalizationConfig
        {
            DefaultLanguage = "en", FallbackLanguage = "en", LocalizationPath = root, EnableHotReload = false
        });
        _manager.Initialize();
        ApplySaved(null);
    }

    /// <summary>Uses OS language only for a missing first-launch preference; invalid saved values safely use English.</summary>
    /// <param name="savedLanguage">Explicit preference, or null for a game that has never saved a language choice.</param>
    internal void ApplySaved(string? savedLanguage)
        => SetLanguage(savedLanguage is null ?
            (OS.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh-CN" : "en") :
            Normalize(savedLanguage));

    /// <summary>Normalizes supported explicit values without guessing unsupported saved languages.</summary>
    /// <param name="language">Requested or persisted language code.</param>
    /// <returns>Supported canonical locale.</returns>
    internal static string Normalize(string? language)
        => string.Equals(language, "zh-CN", StringComparison.Ordinal) ? "zh-CN" : "en";

    /// <summary>Applies the locale using the same native/framework mapping boundary as GodotLocalizationSettings.</summary>
    /// <param name="language">Supported language code.</param>
    internal void SetLanguage(string language)
    {
        var normalized = Normalize(language);
        TranslationServer.SetLocale(Map.ResolveGodotLocale(normalized));
        Manager.SetLanguage(Map.ResolveFrameworkLanguage(normalized));
    }

    /// <summary>Formats a named translation through the existing framework localization string capability.</summary>
    /// <param name="key">Table key.</param>
    /// <param name="variables">Named values required by the translation.</param>
    /// <returns>Localized text.</returns>
    internal string Get(string key, params (string Name, object Value)[] variables)
        => Manager.GetString("ui", key).WithVariables(variables).Format();

    /// <summary>Binds an existing view's text or theme without replacing its node, focus, route or pause lease.</summary>
    /// <param name="owner">Native owner whose tree exit releases the subscription.</param>
    /// <param name="refresh">Godot-thread text/theme refresh.</param>
    internal void Bind(Node owner, Action refresh)
    {
        void Changed(string _)
        {
            if (GodotObject.IsInstanceValid(owner) && !owner.IsQueuedForDeletion()) refresh();
            else Manager.UnsubscribeFromLanguageChange(Changed);
        }
        Manager.SubscribeToLanguageChange(Changed);
        owner.TreeExiting += () => Manager.UnsubscribeFromLanguageChange(Changed);
        refresh();
    }

    /// <summary>Translates sample-owned package names while preserving third-party author naming.</summary>
    /// <param name="id">Stable package ID.</param>
    /// <param name="fallback">Author-supplied display name for unknown packages.</param>
    /// <returns>Localized sample name or the author's unchanged name.</returns>
    internal string PackageName(string id, string fallback)
        => Manager.TryGetText("ui", "pack_" + id, out var translated) ? translated : fallback;

    /// <summary>Translates sample-owned arenas without changing persistent content IDs.</summary>
    /// <param name="id">Stable arena ID.</param>
    /// <param name="fallback">Author-supplied display name.</param>
    /// <returns>Localized sample arena name or the original authored name.</returns>
    internal string ArenaName(string id, string fallback)
        => Manager.TryGetText("ui", "arena_" + id.Replace(':', '_'), out var translated) ? translated : fallback;

    /// <summary>Releases framework subscriptions after application views have stopped.</summary>
    public void Dispose() => _manager?.Destroy();

    private LocalizationManager Manager => _manager ?? throw new InvalidOperationException("Initialize localization on the Godot thread first.");

    private static void Cache(string root, string language, string text)
    {
        var directory = Path.Combine(root, language);
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, "ui.json");
        if (File.Exists(target) && string.Equals(File.ReadAllText(target), text, StringComparison.Ordinal)) return;
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text); File.Move(temporary, target, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void ValidateTables(string english, string chinese)
    {
        var first = JsonSerializer.Deserialize<Dictionary<string, string>>(english)
            ?? throw new InvalidDataException("English localization table is empty.");
        var second = JsonSerializer.Deserialize<Dictionary<string, string>>(chinese)
            ?? throw new InvalidDataException("Chinese localization table is empty.");
        if (!first.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(second.Keys))
            throw new InvalidDataException("English and Chinese localization keys must match.");
        foreach (var (key, value) in first)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(second[key]))
                throw new InvalidDataException("Localization values cannot be empty: " + key);
            var expected = Variables.Matches(value).Select(static match => match.Groups["variable"].Value).ToHashSet(StringComparer.Ordinal);
            var actual = Variables.Matches(second[key]).Select(static match => match.Groups["variable"].Value);
            if (!expected.SetEquals(actual)) throw new InvalidDataException("Localization variables differ: " + key);
        }
    }
}

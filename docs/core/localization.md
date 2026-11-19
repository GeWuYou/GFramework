---
title: "Localize game text"
description: "Load JSON tables by language and refresh views on language change."
---

# Localize game text {#overview}

## Prepare text tables {#tables}

Point `LocalizationConfig.LocalizationPath` at a filesystem directory accessible to the host. Use a subdirectory per language and a JSON file per table. The file name gives the table its name. For example, `localization/eng/menu.json`:

```json
{ "play": "Play", "settings": "Settings" }
```

Initialize `LocalizationManager` on startup and call `GetText("menu", "play")` for a string. Configure the default and fallback languages and supply complete fallback tables. Use `TryGetText` for optional entries.

## Switch languages {#switch}

Call `SetLanguage` to switch languages. Keep the callback you register for language changes so the view can call `UnsubscribeFromLanguageChange` when it closes. Use `GetString` for dynamic text and register formatters as needed.

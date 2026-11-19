---
title: "Application configuration"
description: "Load, validate, and inject application settings at startup."
---

# Application configuration {#overview}

Read startup settings with `Microsoft.Extensions.Configuration` in the composition root. Pass typed Options or parameter objects to consumers. Validate required values and acceptable ranges before startup.

Use [typed configuration](../game/config-system.md) for content tables and the [settings service](../game/setting.md) for player preferences. Give each its own loading and persistence schedule.

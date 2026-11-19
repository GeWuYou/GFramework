---
title: "Frequently asked questions"
description: "Answers to common setup, architecture, and Godot questions."
---

# Frequently asked questions {#overview}

## Which packages do I need? {#packages}

Choose features from the [installation table](./getting-started/installation.md). The Godot package brings in Game and Core. Add source generators for the features that need them.

## Does every service participate in lifecycle management? {#lifecycle}

Register ordinary classes with standard DI. Explicitly register components whose startup or shutdown needs coordination through `AddComponent`. See [application composition](./core/architecture.md).

## When should I use events or CQRS requests? {#messages}

Use [synchronous events](./core/events.md) to notify observers and an explicit method or [CQRS request](./core/cqrs.md) to get a result. Use CQRS when you need a shared asynchronous pipeline.

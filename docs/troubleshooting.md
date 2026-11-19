---
title: "Troubleshooting"
description: "Diagnose registration, resource, and host integration symptoms."
---

# Troubleshooting {#overview}

| Symptom | Check first |
| --- | --- |
| A service cannot be resolved | Check its registration key and lifetime. Resolve scoped services from an operation scope. [Dependency injection](./core/ioc.md) |
| Startup loading fails | Check the initial exception, component registration, and cancellation. [Asynchronous initialization](./core/async-initialization.md) |
| An event is missed | Check the EventBus instance, notification type, and subscription lifetime. [Synchronous events](./core/events.md) |
| A scene or view reports a thread error | Create the dispatcher on the Godot main thread and use it for host integration. [Host composition](./godot/architecture.md) |
| Configuration is missing after export | Check that the preset includes YAML and schema files. [Export configuration](./game/config-system.md#godot) |
| An old save needs conversion | Prepare a candidate, then inspect its confirmation requirements and migration path. [Prepare a load](./game/data.md#load) |
| A generated field or method is absent | Check the analyzer package, inputs, and build diagnostics. [Source generators](./source-generators/index.md) |

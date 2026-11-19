---
title: "Asynchronous initialization"
description: "Wait for file, content, or other initial loads before opening the application."
---

# Asynchronous initialization {#overview}

Implement `IAsyncInitializable` and register the component with `AddComponent`. Put startup I/O in `InitializeAsync` and pass the cancellation token to the operations it awaits.

Await `Architecture.InitializeAsync` in the host before enabling UI or request entry points. Make components able to stop after partial initialization. When startup fails or is canceled, Architecture attempts to stop components whose startup began. Request host shutdown outside the initialization callback.

See [startup and shutdown ordering](../core/architecture.md#lifecycle).

---
title: "Compose fallible computations"
description: "Use Option or Result for a readable application flow."
---

# Compose fallible computations {#overview}

Use `Option<string>` for a nickname that may be absent and choose placeholder text with `Match` at display time. Use `Result` with an error when you need to explain a failed save. Chain fallible steps with `Bind` and handle the outcomes at the service boundary.

See [Option and Result](../core/functional.md) for a minimal example.

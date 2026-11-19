---
title: "Handle failures and cancellation"
description: "Keep operation outcomes actionable for players and callers."
---

# Handle failures and cancellation {#overview}

Distinguish invalid input, cancellation, and unexpected errors at the command boundary. Pass tokens through to I/O. Log the full unexpected exception and operation identifier and give the view an action the player can take.

Complete preparation before publishing results from saving, migration, or resource loading. Keep the current save or data until commit succeeds. See [save preparation](../game/data.md) and [resource leases](../core/resource.md) for concrete flows.

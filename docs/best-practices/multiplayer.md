---
title: "Integrate network synchronization"
description: "Organize remote messages and local events in the game host."
---

# Integrate network synchronization {#overview}

Choose network transport and state authority in the game host. Define versioned network messages and stable entity IDs. Handle joins, disconnects, duplicates, reordering, and reconnects. Validate remote input, call domain rules, and then publish [synchronous events](../core/events.md) or update snapshots for local views.

Use the in-process [CQRS pipeline](../core/cqrs.md) to organize use cases. Have the application explicitly send data that needs to be synchronized remotely.

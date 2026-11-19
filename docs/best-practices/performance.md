---
title: "Measure game performance"
description: "Check frame times, allocations, and resource usage on target devices."
---

# Measure game performance {#overview}

Use the Godot profiler on a repeatable scene and gameplay sequence. Record a baseline, then change one factor at a time. Compare frame time spikes, allocations, peak memory, and load times.

If instantiation is a hotspot, evaluate [node pools](../godot/pool.md). Use [leases](../core/resource.md) to check how long resources remain in use. Recheck the optimization on the same workload and device.

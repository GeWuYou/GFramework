---
title: "Compose frame-based gameplay"
description: "Start, wait for, and observe coroutines in the game loop."
---

# Compose frame-based gameplay {#overview}

## Drive the scheduler {#host}

<<< ../../Samples/CoroutinePerformance/CoroutineExamples.cs#Host{csharp}

## Start a flow {#flow}

<<< ../../Samples/CoroutinePerformance/CoroutineExamples.cs#Flow{csharp}

## Observe completion {#completion}

<<< ../../Samples/CoroutinePerformance/CoroutineExamples.cs#Observe{csharp}

Have the owning component stop scheduling when the game shuts down. See [Godot coroutines](../godot/coroutine.md) for native phases.

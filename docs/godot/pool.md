---
title: "Reuse Godot nodes"
description: "Borrow, reset, and return nodes on the main thread."
---

# Reuse Godot nodes {#overview}

## Create a node pool {#setup}

Derive a pool from `AbstractNodePoolSystem<TKey, TNode>` and create nodes by key. Implement `IPoolableNode` so borrowing restores usable state and returning clears data and subscriptions from the previous use. Prewarming can move creation work to a load phase.

## Manage borrowed nodes {#ownership}

Run pool operations and node callbacks on the Godot main thread. Gameplay owns each active node and returns it once with the original key. On shutdown, stop spawning, return active nodes, and then clear the pool. Decide whether to pool nodes using measured frame time, allocations, and retained memory.

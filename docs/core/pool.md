---
title: "Object pools"
description: "Borrow and return resettable objects by key."
---

# Object pools {#overview}

Derive from `AbstractObjectPoolSystem<TKey, TObject>` and implement object creation. Implement `IPoolableObject` so `OnAcquire` prepares the borrowed state, `OnRelease` resets it, and `OnPoolDestroy` releases final resources.

## Borrowing rules {#ownership}

- Borrow with `Acquire(key)` and return the object once with its original key.
- Use one owner thread for borrowing and returning, or synchronize access in the host.
- If `OnRelease` throws, the borrower retains the object and can retry after fixing the problem. Finish resetting it before it goes back into circulation.

Choose prewarming and capacity based on measured construction cost. For native Godot nodes, see [node pools](../godot/pool.md).

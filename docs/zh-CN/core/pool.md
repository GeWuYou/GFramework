---
title: "对象池"
description: "按键借用和归还可重置的对象。"
---

# 对象池 {#overview}

继承 `AbstractObjectPoolSystem<TKey, TObject>` 并实现创建方法。对象通过 `IPoolableObject` 在 `OnAcquire` 初始化借出状态，在 `OnRelease` 清理，在 `OnPoolDestroy` 释放最终资源。

## 借用规则 {#ownership}

- 使用 `Acquire(key)` 借用对象，完成后归还一次，并使用原始键。
- 将借出与归还集中到同一拥有者线程，或由宿主显式同步。
- `OnRelease` 抛出异常时，修正问题后由持有者归还；先完成必要重置，再让对象进入下一次使用。

预热和容量依据实际创建成本选择。Godot 原生节点见[节点池](../godot/pool.md)。

---
title: "复用 Godot 节点"
description: "在主线程借用、重置和归还节点。"
---

# 复用 Godot 节点 {#overview}

## 建立节点池 {#setup}

从 `AbstractNodePoolSystem<TKey, TNode>` 创建适合项目的节点池，按键创建节点。节点实现 `IPoolableNode`，在借出时恢复可用状态，归还时释放上一次使用的订阅和数据。可以用预热将创建工作移到加载阶段。

## 管理借用 {#ownership}

在 Godot 主线程执行池操作和节点回调。每个活动节点由玩法对象持有，使用完成后按原键归还一次。停止时先停掉生成流程，归还活动节点，再清理池。根据实测帧时间、分配及常驻内存确定是否池化。

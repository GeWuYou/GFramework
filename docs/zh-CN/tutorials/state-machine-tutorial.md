---
title: 状态机教程
---

# 状态机教程

使用普通状态对象表达菜单、运行和结束状态。依赖在构造时传入，Godot 节点由宿主适配器操作。

1. 创建 StateMachine，并配置需要保留的历史数量。
2. 构造并注册全部状态，首次切换后拓扑冻结。
3. 通过 ChangeToAsync 启动，监听实例 StateChanged 更新展示。
4. 回退同样经过当前状态的退出条件。
5. 调用 StopAsync 或 DisposeAsync 等待当前切换并完成清理。

同步状态继承 StateBase，异步状态继承 AsyncStateBase。回调失败后观察 Fault、停止机器并创建新实例。不要在状态回调中等待同一机器的另一次操作。

可运行边界回归见 Core.Tests 的 StateMachineOwnershipTests；完整接口与最小代码见 [状态机](../core/state-machine.md)。

---
title: 输入系统
description: 说明 GFramework.Game 与 GFramework.Game.Abstractions 当前提供的统一输入契约、默认运行时与 UI 语义桥接边界。
---

# 输入系统

`GFramework.Game.Abstractions.Input` 与 `GFramework.Game.Input` 提供的是“动作绑定管理”和“UI 语义桥接”这一层输入系统，
而不是直接替代任何具体引擎的输入 API。

当前 v1 聚焦三件事：

- 用稳定 DTO 描述动作绑定，而不是把引擎原生输入事件暴露给业务层
- 允许导出 / 导入绑定快照，并支持主绑定替换、冲突交换和默认恢复
- 把逻辑动作名桥接到现有 `UiInputAction`，继续复用 `UiService` 的输入仲裁

## 契约层入口

`GFramework.Game.Abstractions.Input` 当前公开这些核心类型：

- `InputBindingDescriptor`
  - 描述一个动作绑定使用的设备族、绑定类型、稳定码值和展示名称
- `InputActionBinding`
  - 描述单个逻辑动作当前持有的绑定集合
- `InputBindingSnapshot`
  - 描述一组动作绑定的可持久化快照
- `IInputBindingStore`
  - 定义查询、主绑定替换、快照导入导出与默认恢复契约
- `IInputDeviceTracker`
  - 定义当前活跃输入设备上下文查询入口
- `IUiInputActionMap` / `IUiInputDispatcher`
  - 定义逻辑动作名到 `UiInputAction` 的桥接边界

这里仍然保留字符串动作名，而不是额外发明新的动作 ID 类型。对 Godot 项目来说，这意味着可以直接继续使用
`project.godot` 生成出来的 `InputActions.*` 常量。

## 默认运行时

`GFramework.Game.Input` 当前提供的默认实现是：

- `InputBindingStore`
  - 纯托管输入绑定存储
  - 管理默认快照、当前快照、主绑定替换与冲突交换
- `InputDeviceTracker`
  - 可由宿主侧更新的活跃设备上下文持有者
- `UiInputActionMap`
  - 默认把 `ui_cancel` / `cancel` 映射到 `UiInputAction.Cancel`
  - 默认把 `ui_accept` / `confirm` / `submit` 映射到 `UiInputAction.Confirm`
- `UiInputDispatcher`
  - 把逻辑动作名继续分发给 `IUiService.DispatchActionAsync(...)`

也就是说，`Game` 层现在只负责统一输入语义与默认运行时行为；实际的物理输入事件采集仍由宿主层负责。

## 最小接入方式

如果你的项目已经有动作名常量，只想先接入统一输入绑定和 UI 桥接，可以从这组最小组合开始：

```csharp
using GFramework.Game.Abstractions.Input;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Input;

var defaultSnapshot = new InputBindingSnapshot(
[
    new InputActionBinding(
        "ui_accept",
        [
            new InputBindingDescriptor(
                InputDeviceKind.KeyboardMouse,
                InputBindingKind.Key,
                "key:13",
                "Enter")
        ])
]);

var bindingStore = new InputBindingStore(defaultSnapshot);
var dispatcher = new UiInputDispatcher(new UiInputActionMap(), ui);
```

随后由项目自己的宿主层决定：

- 什么时候读取物理输入
- 什么时候调用 `SetPrimaryBinding(...)`
- 什么时候触发 `await dispatcher.DispatchAsync(...)`

## 当前边界

- 这套输入抽象当前不尝试复刻完整 `PlayerInput` / `ActionMap` 系统
- 当前只统一动作绑定管理、快照导入导出与 UI 语义桥接
- 设备品牌识别、平台差异文案、震动等宿主专属能力不在 `Game.Abstractions` 契约层
- 触摸 / 手柄轴向等更复杂输入源当前只保证 DTO 能表达，不保证 `Game` 层自带完整采集策略

## 相关主题

- [UI 系统](./ui.md)
- [Godot 输入集成](../godot/input.md)
- [Godot 集成教程](../tutorials/godot-integration.md)

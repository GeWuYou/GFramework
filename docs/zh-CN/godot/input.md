---
title: Godot 输入集成
description: 说明 GFramework.Godot 如何把 InputMap 与 project.godot InputActions 接到新的框架输入抽象。
---

# Godot 输入集成

`GFramework.Godot.Input` 负责把 Godot 的 `InputMap` 绑定表接到 `GFramework.Game.Abstractions.Input` 契约。

当前入口是：

- `GodotInputBindingStore`
  - 读取 / 写回 `InputMap`
  - 导出 / 导入 `InputBindingSnapshot`
  - 把逻辑动作名继续桥接给 `Game` 层的绑定存储与 UI 输入分发语义

## 与 `project.godot` 的关系

当前推荐组合仍然是：

- `project.godot`
  - 继续定义动作名与默认绑定
- `GFramework.Godot.SourceGenerators`
  - 继续生成 `InputActions.*` 字符串常量
- `GFramework.Godot.Input.GodotInputBindingStore`
  - 负责运行时读取默认绑定、替换主绑定、恢复默认和导出快照

这意味着新的运行时输入系统不会替代 `InputActions`，而是把它当作稳定动作名入口继续使用。

## 最小接入方式

```csharp
using GFramework.Game.Abstractions.Input;
using GFramework.Game.Input;
using GFramework.Godot.Generated;
using GFramework.Godot.Input;

var bindingStore = new GodotInputBindingStore();

var acceptBinding = bindingStore.GetBindings(InputActions.UiAccept);
bindingStore.SetPrimaryBinding(
    InputActions.UiAccept,
    new InputBindingDescriptor(
        InputDeviceKind.KeyboardMouse,
        InputBindingKind.Key,
        "key:32",
        "Space"));
```

使用 `IUiService` 时，可将逻辑动作名异步分发到 UI 语义：

```csharp
var dispatcher = new UiInputDispatcher(new UiInputActionMap(), ui);
await dispatcher.DispatchAsync(InputActions.UiCancel, cancellationToken);
```

## 当前边界

- `GodotInputBindingStore` 当前聚焦 `InputMap` 绑定管理，而不是完整 gameplay input runtime
- 当前测试覆盖的是纯托管后端语义，不是 Godot 原生 `InputEvent` 对象在所有宿主中的行为差异
- 设备品牌、手柄图标、震动预设等宿主特化体验仍应视为 Godot 专属扩展，不上升到 `Game.Abstractions`

## 相关主题

- [Game 输入系统](../game/input.md)
- [Godot 运行时集成](./index.md)
- [Godot 集成教程](../tutorials/godot-integration.md)

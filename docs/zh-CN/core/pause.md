---
title: 暂停系统
description: 使用独立暂停服务管理嵌套请求、分组、处理器和宿主停止。
---

# 暂停系统

`PauseStackManager` 是通过构造函数接收可选标准日志的普通服务。它记录暂停请求，由宿主实现的 `IPauseHandler` 将状态应用到引擎、音频或其他系统。

## 嵌套暂停

```csharp
using GFramework.Core.Pause;

await using var pauses = new PauseStackManager();
var menu = pauses.Push("Pause menu");
var settings = pauses.Push("Settings");

pauses.Pop(settings); // 主暂停菜单仍持有请求。
pauses.Pop(menu);     // 最后一个请求释放后恢复。
```

令牌精确对应一次请求，可按任意顺序释放。`Pop` 在令牌不存在或已释放时返回 `false`。`GetPauseDepth` 和 `GetPauseReasons` 提供当前状态，`IsPaused` 表示指定组是否仍有请求。

短生命周期的请求可使用 `PauseScope`：

```csharp
using (pauses.PauseScope("Blocking operation"))
{
    await CompleteBlockingOperationAsync();
}
```

作用域释放只结束自己的请求；其他暂停请求保持有效。

## 分组和宿主处理

`Push`、`PauseScope`、`IsPaused` 等入口接受可选 `PauseGroup`，缺省为 `Global`。不同组分别维护深度和状态。宿主可以根据组选择受影响的子系统。

`RegisterHandler` 登记实现 `IPauseHandler` 的适配器。处理器提供优先级和 `OnPauseStateChanged(PauseGroup, bool)`；从零请求到有请求时暂停，从最后一个请求到零时恢复。

`OnPauseStateChanged` 使用标准 .NET 事件模式。处理器和事件在内部锁外调用，适配器应按宿主要求在正确线程执行原生操作。将管理器作为实例服务共享给 UI、业务流程和宿主，避免每个功能创建自己的暂停状态。

Godot 接入见 [Godot 暂停](../godot/pause.md)。UI 可在路由定义中声明可见期间持有暂停请求，显示和关闭时由 UI 服务管理令牌。

## 清理

宿主停止时先关闭使用暂停的 UI 和业务操作，再等待管理器 `DisposeAsync`。关闭会释放全部请求、通知恢复并清理同步资源；重复关闭不会重复发布恢复。

标准 DI 创建的管理器随容器异步释放。手工创建或外部传入的实例由调用方负责关闭。`ClearGroup` 和 `ClearAll` 适合明确的业务重置，不应代替普通调用方释放自己的令牌。

## 迁移

暂停能力采用构造注入或标准服务登记。实例创建后即可使用，无需 `Initialize`。宿主在自己的停止顺序中等待 `DisposeAsync`。

## 相关主题

- [UI 系统](../game/ui.md)
- [Godot 暂停](../godot/pause.md)

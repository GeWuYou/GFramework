---
title: Godot 扩展方法
description: 以当前 GFramework.Godot.Extensions 源码为准，说明路径、Node、signal 和订阅释放 扩展的真实成员与边界。
---

# Godot 扩展方法

`GFramework.Godot.Extensions` 当前并不是“覆盖所有 Godot 节点操作”的万能层。按源码看，它实际公开的扩展主要只有四组：

- `GodotPathExtensions`
- `NodeExtensions`
- `SignalFluentExtensions`
- `SubscriptionExtensions`

按源码看，这些扩展各自解决的问题并不相同，也需要区分哪些能力属于单一运行时辅助，而不是统一入口。

## 当前公开入口

### `GodotPathExtensions`

这组扩展只负责判断 Godot 虚拟路径前缀：

- `IsUserPath(this string path)`
- `IsResPath(this string path)`
- `IsGodotPath(this string path)`

它们不做文件访问，也不解析目录结构，只是用字符串前缀判断 `user://` 和 `res://`。

```csharp
using GFramework.Godot.Extensions;

if ("user://save.json".IsUserPath())
{
}

if ("res://config/gameplay.yaml".IsGodotPath())
{
}
```

### `NodeExtensions`

`NodeExtensions` 是当前扩展集合里体量最大的部分，但职责仍然比较具体，主要分成下面几类。

#### 生命周期与有效性辅助

- `QueueFreeX(this Node? node)`
- `FreeX(this Node? node)`
- `WaitUntilReadyAsync(this Node node)`
- `WaitUntilReady(this Node node, Action callback)`
- `IsValidNode(this Node? node)`
- `IsInvalidNode(this Node? node)`

这里最容易写偏的地方有两个：

- `QueueFreeX()` / `FreeX()` 会先检查 null、实例是否仍有效、是否已经进入删除队列
- `IsValidNode()` 不只要求实例还活着，还要求节点已经在 `SceneTree` 里；单纯 `new` 出来但还没挂树的节点会返回 `false`

#### 节点访问与装配辅助

- `FindChildX<T>(...)`
- `GetOrCreateNode<T>(...)`
- `AddChildXAsync(...)`
- `GetParentX<T>()`
- `GetRootNodeX()`
- `ForEachChild<T>(...)`
- `OfType<T>()`

这几组方法更偏“少量常用装配动作”，不是完整查询 DSL。

特别是 `GetOrCreateNode<T>(string path)` 的当前实现要注意：

1. 先尝试 `GetNodeOrNull<T>(path)`
2. 如果没找到，就 `new T()`
3. 把新节点直接 `AddChild(...)` 到当前节点
4. 再把 `created.Name = path`

它不会按斜杠路径逐级创建中间节点，所以不要把它当成层级化路径构建器。

#### 输入、暂停与调试辅助

- `SetInputAsHandled()`
- `Paused(bool paused = true)`
- `DisableInput()`
- `EnableInput()`
- `LogNodePath()`
- `PrintTreeX(string indent = "")`
- `SafeCallDeferred(string method)`

这些方法都很薄，基本是在现有 `Viewport` / `SceneTree` / `CallDeferred(...)` 上做便捷包装，没有额外状态机。

### `SignalFluentExtensions`

`SignalFluentExtensions` 只提供一个入口：

- `Signal(this GodotObject @object, StringName signal)`

它把目标对象和 signal 名称包装成 `SignalBuilder`。具体连接语义请看 [Godot 信号系统](./signal.md)。

### `SubscriptionExtensions`

`DisposeWhenNodeExits(this IDisposable subscription, Node node)` 把订阅绑定到节点本次在树中的生命周期。返回的新令牌同时拥有节点 TreeExiting 钩子和原订阅；提前释放返回令牌时，会一次性解除两者。

```csharp
using System;
using GFramework.Godot.Extensions;

IDisposable lifetime = eventBus
    .Subscribe<SettingsChangedEvent>(OnSettingsChanged)
    .DisposeWhenNodeExits(this);

lifetime.Dispose(); // 可以提前结束；节点退出时同样释放这个组合令牌。
```

重复释放幂等，原始订阅只释放一次。该入口不会替代原生 signal 连接的显式管理。

## 最小接入路径

### 1. 节点进入树之后再做装配

如果你的节点可能在 `_Ready()` 前就被访问，先用 `WaitUntilReadyAsync()`：

```csharp
using GFramework.Godot.Extensions;
using GFramework.Godot.Extensions.Signal;
using Godot;

public partial class SettingsPanel : Control
{
    public override async void _Ready()
    {
        await this.WaitUntilReadyAsync();

        var applyButton = FindChildX<Button>("ApplyButton");
        applyButton?.Signal(Button.SignalName.Pressed)
                    .To(Callable.From(OnApplyPressed));
    }

    private void OnApplyPressed()
    {
        this.SetInputAsHandled();
    }
}
```

### 2. 框架订阅和节点生命周期一起收尾

订阅返回 IDisposable；保存 DisposeWhenNodeExits 返回的组合令牌：

```csharp
private IDisposable? _subscription;

public override void _Ready()
{
    _subscription = _eventBus
        .Subscribe<SettingsChangedEvent>(OnSettingsChanged)
        .DisposeWhenNodeExits(this);
}

public void StopListening()
{
    _subscription?.Dispose();
    _subscription = null;
}
```

事件总线通过节点的显式配置入口提供。节点重新入树后，需要重新建立本次树生命周期的订阅。

### 3. 只在需要时使用 signal fluent API

`Signal(...)` 属于扩展集合的一部分，但它已经有独立页面。实践上可以这样分工：

- 节点查找、ready 等待、输入处理：`NodeExtensions`
- 动态 signal 绑定：`Signal(...)`
- 框架订阅释放：`DisposeWhenNodeExits(...)`
- 路径前缀判断：`GodotPathExtensions`

## 当前边界

- 当前 `NodeExtensions` 不提供 `GetNodeX()`、`CreateSignalBuilder()` 这类额外包装 API
- 它不是 router、scene factory、UI factory 或生成器的替代层
- `GetOrCreateNode<T>()` 只会创建一个直接子节点，不会递归补整条路径
- `SafeCallDeferred(...)` 只有在 `IsValidNode()` 为 `true` 时才会调用；节点未入树时不会执行
- `DisposeWhenNodeExits(...)` 接收 `IDisposable` 订阅，并返回同时管理节点钩子和订阅的新令牌，不会自动处理 Godot 原生 `Connect(...)`
- 协程辅助扩展在 `GFramework.Godot.Coroutine` 命名空间，不属于这组 `Extensions` 页面要覆盖的核心范围

## 继续阅读

- [Godot 运行时集成](./index.md)
- [Godot 信号系统](./signal.md)
- [Godot 场景系统](./scene.md)
- [Godot UI 系统](./ui.md)

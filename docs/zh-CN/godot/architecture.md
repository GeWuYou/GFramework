---
title: Godot 架构集成
description: 说明 AbstractArchitecture、ArchitectureAnchor 和 Godot 模块挂接的当前生命周期语义，避免继续沿用旧版 `.Wait()` 接法。
---

# Godot 架构集成

## 概述

`GFramework.Godot` 当前的架构集成目标很直接：让 `Architecture` 能安全地感知 Godot `SceneTree` 生命周期，并在需要时把
带 `Node` 的扩展模块挂到场景树上。

当前真正参与这条链路的核心类型只有三类：

- `AbstractArchitecture`：在原有 `Architecture` 之上增加 Godot 生命周期绑定
- `ArchitectureAnchor`：挂在 `SceneTree.Root` 下的锚点节点，负责把 `_ExitTree()` 事件转回架构销毁
- `IGodotModule` / `AbstractGodotModule`：当模块本身需要携带 Godot `Node` 时使用

它不是另一套独立的模块系统，也不意味着所有模块都必须改成 `InstallGodotModule(...)`。

## 什么时候该用 `AbstractArchitecture`

当你的架构需要满足下面任一条件时，可以让它继承 `AbstractArchitecture`：

- 需要把架构生命周期绑定到 Godot `SceneTree`
- 需要在架构里安装带 `Node` 的扩展模块
- 需要通过受保护的 `ArchitectureRoot` 访问锚点节点，继续挂接 Godot 子节点

如果你只是做普通的 Model / System / Utility 注册，`AbstractArchitecture` 的主要价值仍然是“让架构知道自己何时跟随
Godot 场景树销毁”，而不是改变注册方式。

## 最小接入路径

### 常规模块仍然用 `InstallModule(...)`

当前消费者 `ai-libs/CoreGrid` 的默认做法，是保持普通模块注册方式：

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Environment;
using GFramework.Godot.Architectures;

namespace MyGame.Scripts.Core;

public sealed class GameArchitecture(
    IArchitectureConfiguration configuration,
    IEnvironment environment)
    : AbstractArchitecture(configuration, environment)
{
    protected override void InstallModules()
    {
        InstallModule(new UtilityModule());
        InstallModule(new ModelModule());
        InstallModule(new GameplayModule());
        InstallModule(new SystemModule());
    }
}
```

这里继承 `AbstractArchitecture` 的意义，是把架构绑定到 Godot 生命周期，而不是把普通模块注册改写成 Godot 风格 API。

### 只有携带 `Node` 的模块才需要 `InstallGodotModule(...)`

如果模块本身暴露一个 Godot `Node`，并且希望由架构锚点统一托管，可以这样写：

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Godot.Architectures;
using Godot;

namespace MyGame.Scripts.Core;

public sealed class HudModule : AbstractGodotModule
{
    private readonly Control _root = new()
    {
        Name = "HudModule"
    };

    public override Node Node => _root;

    public override void Install(IArchitecture architecture)
    {
    }

    public override void OnAttach(GFramework.Core.Architectures.Architecture architecture)
    {
    }

    public override void OnDetach()
    {
        _root.QueueFree();
    }
}
```

这类模块的关键点不是“注册更多框架能力”，而是“让模块节点跟着架构锚点进出场景树”。
真正调用 `InstallGodotModule(...)` 时，也应该把它放在能够接受异步挂接流程的初始化路径里，而不是继续沿用旧文档里的
`.Wait()` 叙述。

## 当前生命周期

### 初始化阶段

`AbstractArchitecture.OnInitialize()` 目前会按这个顺序工作：

1. 生成唯一的锚点节点名称
2. 调用 `AttachToGodotLifecycle()`
3. 在可用的 `SceneTree` 上创建并绑定 `ArchitectureAnchor`
4. 执行你重写的 `InstallModules()`

也就是说，Godot 生命周期绑定先发生，业务模块注册后发生。

### `InstallGodotModule(...)` 的执行顺序

当前实现里，`InstallGodotModule(...)` 会：

1. 检查模块参数是否为 `null`
2. 检查 `_anchor` 是否已初始化
3. 先执行 `module.Install(this)`
4. 把模块登记进内部 `_extensions`
5. `await anchor.WaitUntilReadyAsync()`
6. 通过 `CallDeferred(AddChild, module.Node)` 把模块节点挂到锚点下
7. 调用 `module.OnAttach(this)`

这条顺序有两个实际意义：

- 模块会在挂接节点前先完成框架侧注册
- 只有等锚点真正 ready 后，才进入需要访问 Godot 节点 API 的附加阶段

### 销毁阶段

`ArchitectureAnchor._ExitTree()` 会触发绑定好的退出回调，随后 `AbstractArchitecture` 会开始观察异步销毁流程：

- 防止重复销毁
- 依次调用已登记 Godot 模块的 `OnDetach()`
- 清空内部扩展列表
- 再进入基类 `DestroyAsync()`

如果异步销毁抛异常，当前实现会把错误写到 Godot 错误输出，而不是静默吞掉。

## 当前边界

### 没有锚点时不会偷偷安装模块

`GFramework.Godot.Tests/Architectures/AbstractArchitectureModuleInstallationTests.cs` 已覆盖一个关键边界：

- 当锚点尚未初始化时，`InstallGodotModule(...)` 会直接抛 `InvalidOperationException("Anchor not initialized")`
- 失败发生在 `module.Install(...)` 之前，因此不会留下半安装副作用

这也是为什么文档不应该再把 `InstallGodotModule(...).Wait()` 写成一种随处可用的默认初始化方式。

### `AbstractGodotModule` 只是便捷基类，不代表自动阶段广播

当前接口 `IGodotModule` 真正保证的成员只有：

- `Node`
- `Install(IArchitecture architecture)`
- `OnAttach(Architecture architecture)`
- `OnDetach()`

`AbstractGodotModule` 里虽然保留了 `OnPhase(...)` / `OnArchitecturePhase(...)` 虚方法，但它们不在当前接口契约内，也没有在
这条挂接流程里形成稳定的自动广播语义。不要把它写成当前公开保证。

### `ArchitectureRoot` 只在锚点就绪后可用

`ArchitectureRoot` 是受保护属性，底层直接返回 `_anchor`。如果锚点尚未准备好或架构已经失效，它会抛
`InvalidOperationException("Architecture root not ready")`。因此它适合放在明确依赖锚点存在的挂接逻辑里，而不是拿来做
任意时机的全局节点查找。

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Godot 集成教程](../tutorials/godot-integration.md)
3. [Godot 场景系统](./scene.md)
4. [Godot UI 系统](./ui.md)

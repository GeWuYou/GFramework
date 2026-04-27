---
title: ContextAware 生成器
description: 说明 [ContextAware] 当前会生成什么、何时使用、与 ContextAwareBase 的边界以及测试场景。
---

# ContextAware 生成器

`[ContextAware]` 是 `GFramework.Core.SourceGenerators` 中最常用的一类生成器。它的职责很明确：

- 为当前类型自动补齐 `IContextAware`
- 提供可复用的上下文懒加载入口
- 让类型可以直接使用 `this.GetSystem<T>()`、`this.GetModel<T>()`、`this.GetUtility<T>()` 等扩展方法

它不负责注册服务，也不会替你决定应该取哪个 `System` / `Model`。它解决的是“当前类型如何拿到架构上下文”。

## 当前包关系

- 特性来源：`GFramework.Core.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Core.SourceGenerators`
- 运行时接口：`GFramework.Core.Abstractions.Rule.IContextAware`
- 常用扩展方法：`GFramework.Core.Extensions`

如果只安装运行时 `GFramework.Core` 而没有安装 `Core.SourceGenerators`，`[ContextAware]` 本身不会生效。

## 最小用法

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class PlayerController : IController
{
    public void Initialize()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var combatSystem = this.GetSystem<ICombatSystem>();

        combatSystem.Bind(playerModel);
    }
}
```

当前最重要的前置条件只有两个：

- 必须是 `class`
- 必须声明为 `partial`

如果缺少这两个条件，生成器不会补代码。

## 当前会生成什么

按当前源码，`[ContextAware]` 会为目标类型生成：

- `IContextAware` 的显式接口实现
- 受保护的 `Context` 属性
- 类型级静态 `SetContextProvider(...)`
- 类型级静态 `ResetContextProvider()`
- 一个实例级 `_context` 缓存字段
- 一个类型级共享的 `_contextProvider`
- 一个类型级锁 `_contextSync`

这意味着它不是“每次访问都现查当前架构”。当前行为更接近：

1. 先看当前实例是否已经缓存了上下文
2. 如果没有，就在同步锁内取共享 provider
3. 若 provider 为空，则回退到 `GameContextProvider`
4. 把拿到的上下文缓存到当前实例

## 当前语义里最容易误解的点

### provider 是按类型共享，不是按实例共享

`SetContextProvider(...)` 影响的是“这个生成类型的后续实例或尚未初始化上下文的实例”，不是全仓库所有 `[ContextAware]`
类型共享同一个 provider。

### provider 切换不会自动刷新已缓存实例

一旦某个实例已经把上下文缓存进 `_context`，后续再调用：

- `SetContextProvider(...)`
- `ResetContextProvider()`

都不会自动改写这个实例的已缓存上下文。

如果你确实要覆盖某个现有实例的上下文，应显式调用：

```csharp
((IContextAware)controller).SetContext(context);
```

### 生成路径和 `ContextAwareBase` 不是一回事

当前源码里两者的默认回退策略不同：

- `[ContextAware]` 生成实现
  - 通过共享 provider 回退，默认 provider 是 `GameContextProvider`
  - 带同步锁，支持 `SetContextProvider(...)` / `ResetContextProvider()`
- `ContextAwareBase`
  - 只维护简单的实例级缓存
  - 不维护共享 provider
  - 默认直接回退到 `GameContext.GetFirstArchitectureContext()`

因此，旧文档里把两条路径混写成“只是写法不同”已经不准确。

## 何时使用 `[ContextAware]`

优先用于这些场景：

- 你的类型不是 `AbstractSystem`、`AbstractModel`、`AbstractCommand` 这类已经继承 `ContextAwareBase` 的框架基类
- 你希望在测试中显式切换 provider
- 你需要在同一生成类型上统一切换上下文来源
- 你在 Godot 节点、Controller、ViewModel、包装器类型上只想获得上下文访问能力

典型例子：

- `IController` 实现
- Godot `Node` / `Control` 的项目侧包装器
- 不继承框架基类但要访问架构的辅助类型

## 何时改用 `ContextAwareBase`

以下场景优先考虑 `ContextAwareBase` 或已经继承它的框架基类：

- 你本来就继承 `AbstractSystem`、`AbstractModel`、`AbstractCommand`、`AbstractQuery`
- 你不需要类型级共享 provider
- 你只需要简单的实例级上下文缓存
- 调用线程模型已经天然串行，不需要生成实现那套 provider 切换与同步语义

如果一个类型已经通过继承链拿到了 `ContextAwareBase`，通常没必要再额外标 `[ContextAware]`。

## 与 Context Get 注入的关系

`[GetModel]`、`[GetSystem]`、`[GetUtility]`、`[GetService]` 这类字段注入生成器，并不是独立工作的。

按当前 `ContextGetGenerator` 的判定规则，目标类型必须满足以下三者之一：

- 标记了 `[ContextAware]`
- 实现了 `IContextAware`
- 继承了 `ContextAwareBase`

所以更准确的理解是：

- `[ContextAware]` 负责“让类型成为 context-aware 类型”
- Context Get 系列特性负责“在这个前提下继续减少字段取值样板”

## 测试场景

如果测试里不想依赖默认全局上下文，推荐显式配置 provider：

```csharp
PlayerController.SetContextProvider(new TestContextProvider(testArchitecture.Context));

try
{
    var controller = new PlayerController();
    controller.Initialize();
}
finally
{
    PlayerController.ResetContextProvider();
}
```

需要注意两点：

- `ResetContextProvider()` 只会重置共享 provider，不会清除已创建实例上的 `_context`
- 如果测试要复用同一实例并切换上下文，应该显式调用 `((IContextAware)instance).SetContext(...)`

## 诊断与约束

当前文档里最值得记住的约束只有这些：

- 非 `class` 会触发 `GF_Rule_001`
- 非 `partial` 不会生成实现，并会触发公共 partial 约束诊断
- 嵌套、字段注入等其他错误通常由对应的 Context Get 生成器和其诊断补充报告

## 与旧写法的边界

下面这些旧说法已经不够准确：

- “`[ContextAware]` 只是帮你补一个简单的 `GetContext()`”
- “切换 provider 后，已有实例会自动跟着切换”
- “`[ContextAware]` 和 `ContextAwareBase` 的默认行为完全一致”

当前更准确的理解是：

- 生成实现带有实例缓存、类型级共享 provider 和同步锁
- provider 切换只影响尚未缓存上下文的实例
- `ContextAwareBase` 是更轻量的实例级缓存路径

## 推荐阅读

1. [ContextGet 生成器](./context-get-generator.md)
2. [日志生成器](./logging-generator.md)
3. [Core 模块总览](../core/index.md)
4. [源码生成器总览](./index.md)

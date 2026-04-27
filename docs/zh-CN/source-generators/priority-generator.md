---
title: Priority 生成器
description: 说明 [Priority] 当前会生成什么、何时生效、应配合哪些优先级 API 使用，以及动态优先级的边界。
---

# Priority 生成器

`[Priority]` 的职责很简单：为目标类型自动生成 `IPrioritized.Priority`。

它本身不是调度器，也不会自动改变系统、服务或处理器的执行顺序。只有调用方使用了“按优先级排序”的检索入口，生成出来的
`Priority` 才会真正影响顺序。

## 当前包关系

- 特性来源：`GFramework.Core.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Core.SourceGenerators`
- 运行时契约：`GFramework.Core.Abstractions.Bases.IPrioritized`
- 预定义常量：`GFramework.Core.Abstractions.Bases.PriorityGroup`

## 最小用法

```csharp
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.SourceGenerators.Abstractions.Bases;

[Priority(PriorityGroup.High)]
public partial class SaveSystem : AbstractSystem
{
    protected override void OnInit()
    {
    }
}
```

当前生成器会补出：

```csharp
public int Priority => PriorityGroup.High;
```

优先级值越小，优先级越高。

## 当前真正会读取优先级的入口

### `IIocContainer`

如果你直接在容器层取集合，使用：

```csharp
var handlers = container.GetAllByPriority<IMyHandler>();
```

### `IArchitectureContext`

当前推荐按组件类别使用这些 API：

- `GetServicesByPriority<TService>()`
- `GetSystemsByPriority<TSystem>()`
- `GetModelsByPriority<TModel>()`
- `GetUtilitiesByPriority<TUtility>()`

### `IContextAware` 扩展方法

如果你已经在 `[ContextAware]` 类型或 `ContextAwareBase` 派生类型里，直接用：

- `this.GetServicesByPriority<TService>()`
- `this.GetSystemsByPriority<TSystem>()`
- `this.GetModelsByPriority<TModel>()`
- `this.GetUtilitiesByPriority<TUtility>()`

这比旧文档里反复出现的 `this.GetAllByPriority<T>()` 更贴近当前公开扩展方法。

## 最小接入示例

### 系统排序

```csharp
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Bases;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[Priority(PriorityGroup.Critical)]
public partial class InputSystem : AbstractSystem
{
    protected override void OnInit()
    {
    }
}

[ContextAware]
public partial class SystemBootstrapper : IController
{
    public void Start()
    {
        var systems = this.GetSystemsByPriority<ISystem>();

        foreach (var system in systems)
        {
            system.Initialize();
        }
    }
}
```

### 服务排序

```csharp
[Priority(PriorityGroup.High)]
public partial class PremiumSaveMigration : ISaveMigration
{
}

[Priority(PriorityGroup.Low)]
public partial class MetricsSaveMigration : ISaveMigration
{
}

var migrations = architecture.Context.GetServicesByPriority<ISaveMigration>();
```

## `PriorityGroup` 的角色

当前仓库提供了这些预定义常量：

- `PriorityGroup.Critical`
- `PriorityGroup.High`
- `PriorityGroup.Normal`
- `PriorityGroup.Low`
- `PriorityGroup.Deferred`

文档不应该把这些值解释成硬编码的生命周期阶段。它们只是团队共享的排序语义常量，具体“高优先级意味着先做什么”仍然取决于
调用方对排序结果的使用方式。

如果项目有更细粒度的排序约定，也可以直接传 `int`，或在项目层自定义自己的优先级常量。

## 何时使用 `[Priority]`

适合以下场景：

- 类型顺序在编译期就能确定
- 你不想手写 `IPrioritized`
- 同一类型的所有实例都应共享同一个优先级

常见例子：

- 初始化顺序明确的系统
- 顺序敏感的服务实现
- 有先后要求的处理器或迁移器

## 何时不要使用 `[Priority]`

以下场景应改为手写 `IPrioritized`：

- 优先级要依赖运行时配置
- 优先级要根据环境、开关或状态动态变化
- 你已经手动实现了 `IPrioritized`

例如：

```csharp
public sealed class DynamicPrioritySystem : IPrioritized
{
    private readonly bool _enabled;

    public DynamicPrioritySystem(bool enabled)
    {
        _enabled = enabled;
    }

    public int Priority => _enabled ? PriorityGroup.High : PriorityGroup.Deferred;
}
```

## 当前诊断与约束

`[Priority]` 当前有几条直接约束：

- `GF_Priority_001`
  - 只能标在 `class`
- `GF_Priority_002`
  - 目标类型已经手写实现 `IPrioritized`
- `GF_Priority_003`
  - 类型必须是 `partial`
- `GF_Priority_004`
  - 特性值缺失或无效
- `GF_Priority_005`
  - 不支持嵌套类

对文档而言，最关键的结论是：

- `partial` 是强约束
- 顶层类是强约束
- 手写实现与生成实现只能二选一

## 与旧写法的边界

下面这些旧写法或旧表述已经不再适合作为默认指导：

- 在 `IContextAware` 类型里统一写 `this.GetAllByPriority<T>()`
- 继续用 `system.Init()` 作为系统初始化示例
- 把 `[Priority]` 写成“标了就会自动改变执行顺序”

当前更准确的理解是：

- `[Priority]` 只生成 `Priority`
- 排序效果依赖容器、上下文或扩展方法是否走了 priority-aware API
- `IContextAware` 路径更推荐按组件类别使用 `GetSystemsByPriority` / `GetServicesByPriority` 等入口

## 推荐阅读

1. [ContextAware 生成器](./context-aware-generator.md)
2. [ContextGet 生成器](./context-get-generator.md)
3. [Core 模块总览](../core/index.md)
4. [源码生成器总览](./index.md)

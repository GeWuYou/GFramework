---
title: Rule 包使用说明
description: 说明 GFramework.Core.Rule 中 IContextAware 规则接口与上下文访问约定。
---

# Rule 包使用说明

## 概述

Rule 包定义了框架的核心规则接口 `IContextAware`，它用于需要架构上下文的组件，普通 DI 服务无需实现它。通过这个接口，框架实现了统一的上下文管理和能力访问机制。框架组件（Model、System）与 CQRS 的上下文感知处理器都实现此接口，并通过扩展方法获得访问架构服务的能力。

## 核心接口：IContextAware

`IContextAware` 是框架的核心规则接口，定义了上下文的设置和获取能力。

**接口定义：**

```csharp
public interface IContextAware
{
    /// <summary>
    ///     设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文对象，用于提供架构级别的服务和功能访问</param>
    void SetContext(IArchitectureContext context);

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>当前的架构上下文对象</returns>
    IArchitectureContext GetContext();
}
```

**作用：**

- `SetContext()` - 框架在初始化组件时调用此方法，注入架构上下文
- `GetContext()` - 组件通过此方法获取上下文，进而访问所有架构服务

**实现此接口的类型：**

- `AbstractModel` - 模型基类
- `AbstractSystem` - 系统基类
- CQRS 的上下文感知处理器基类（`AbstractRequestHandler` 等，位于 `GFramework.Cqrs`）
- 以及其他需要感知架构上下文的自定义组件

## 基类实现：ContextAwareBase

`ContextAwareBase` 是实现 `IContextAware` 的标准基类，为所有需要感知架构上下文的类提供基础实现。

**类定义：**

```csharp
public abstract class ContextAwareBase : IContextAware
{
    /// <summary>
    ///     获取当前实例的架构上下文
    /// </summary>
    protected IArchitectureContext? Context { get; set; }

    /// <summary>
    ///     设置架构上下文的实现方法，由框架调用
    /// </summary>
    void IContextAware.SetContext(IArchitectureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        OnContextReady();  // 上下文准备好后调用此方法
    }

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    IArchitectureContext IContextAware.GetContext()
    {
        return Context ?? throw new InvalidOperationException(
            "Bind this instance through IContextAware.SetContext before accessing it.");
    }

    /// <summary>
    ///     当上下文准备就绪时调用的虚方法，子类可以重写此方法来执行上下文相关的初始化逻辑
    /// </summary>
    protected virtual void OnContextReady()
    {
    }
}
```

**关键特性：**

- `Context` 属性 - 存储架构上下文的引用
- `OnContextReady()` 钩子 - 在上下文设置完成后调用，用于初始化逻辑
- 显式绑定 - 未调用 SetContext 的实例访问上下文会抛出 InvalidOperationException，不回退全局上下文

## 扩展方法：框架能力的来源

所有框架能力都通过 `ContextAwareExtensions` 类的扩展方法提供。这种设计使得 `IContextAware` 接口保持简洁，同时能够灵活地添加新能力。

### 获取架构组件

```csharp
// 获取指定类型的模型
public static TModel? GetModel<TModel>(this IContextAware contextAware)
    where TModel : class, IModel

// 获取指定类型的系统
public static TSystem? GetSystem<TSystem>(this IContextAware contextAware)
    where TSystem : class, ISystem

// 获取指定类型的工具
public static TUtility? GetUtility<TUtility>(this IContextAware contextAware)
    where TUtility : class, IUtility

// 获取指定类型的服务
public static TService? GetService<TService>(this IContextAware contextAware)
    where TService : class
```

### CQRS 消息分发

CQRS 的分发扩展位于 `GFramework.Cqrs.Extensions`（仅异步入口，需要架构安装 CQRS runtime）：

```csharp
// 发送请求并获取结果
public static ValueTask<TResponse> SendRequestAsync<TResponse>(this IContextAware contextAware,
    IRequest<TResponse> request, CancellationToken ct = default)

// 发送无返回值请求
public static ValueTask SendAsync<TCommand>(this IContextAware contextAware,
    TCommand command, CancellationToken ct = default) where TCommand : IRequest<Unit>

// 发布通知
public static ValueTask PublishAsync<TNotification>(this IContextAware contextAware,
    TNotification notification, CancellationToken ct = default) where TNotification : INotification

// 创建流式请求
public static IAsyncEnumerable<TResponse> CreateStream<TResponse>(this IContextAware contextAware,
    IStreamRequest<TResponse> request, CancellationToken ct = default)
```

### 事件系统

```csharp
// 注册事件处理器
public static IUnRegister RegisterEvent<TEvent>(this IContextAware contextAware,
    Action<TEvent> handler)

// 取消事件注册
public static void UnRegisterEvent<TEvent>(this IContextAware contextAware,
    Action<TEvent> onEvent)

// 发送无参数的事件
public static void SendEvent<TEvent>(this IContextAware contextAware)
    where TEvent : new()

// 发送具体的事件实例
public static void SendEvent<TEvent>(this IContextAware contextAware, TEvent e)
    where TEvent : class
```

### 环境访问

```csharp
// 获取环境对象（泛型版本）
public static T? GetEnvironment<T>(this IContextAware contextAware)
    where T : class

// 获取环境对象
public static IEnvironment GetEnvironment(this IContextAware contextAware)
```

## 框架组件如何使用 IContextAware

### CQRS 请求处理器

CQRS 的处理器基类继承轻量上下文基类，分发前由 runtime 注入架构上下文。

```csharp
public sealed class BuyItemHandler : AbstractRequestHandler<BuyItemRequest, bool>
{
    public override async ValueTask<bool> Handle(BuyItemRequest request, CancellationToken cancellationToken)
    {
        // 通过 Context 属性访问 Model
        var playerModel = Context.GetModel<PlayerModel>();
        var shopModel = Context.GetModel<ShopModel>();

        // 业务逻辑
        if (playerModel.Gold.Value >= request.Price)
        {
            playerModel.Gold.Value -= request.Price;
            // 分发其他请求
            await Context.SendAsync(new AddItemRequest(request.ItemId));
            return true;
        }
        return false;
    }
}
```

在架构集成路径中分发这个请求：

```csharp
using GFramework.Cqrs.Extensions;

var bought = await this.SendRequestAsync<bool>(new BuyItemRequest(...));
```

### Model

所有模型都继承自 `AbstractModel`，实现了 `IContextAware`。

```csharp
public class PlayerModel : AbstractModel
{
    public ReactiveProperty<int> Level { get; } = new(1);
    public ReactiveProperty<int> Gold { get; } = new(0);

    protected override void OnContextReady()
    {
        // 模型初始化时的逻辑
        Console.WriteLine("PlayerModel initialized");
    }
}
```

### System

所有系统都继承自 `AbstractSystem`，实现了 `IContextAware`。

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 注册事件监听
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
        this.RegisterEvent<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        var combatModel = this.GetModel<CombatModel>();

        // 处理攻击逻辑
        combatModel.ApplyDamage(e.Damage);
    }

    private void OnEnemyDefeated(EnemyDefeatedEvent e)
    {
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.Gold.Value += e.RewardGold;
    }
}
```

## 自定义组件使用 IContextAware

任何自定义类都可以继承 `ContextAwareBase` 来获得架构上下文访问能力。

```csharp
public class SaveManager : ContextAwareBase
{
    protected override void OnContextReady()
    {
        // 上下文准备好后的初始化
        Console.WriteLine("SaveManager initialized");
    }

    public void SaveGame()
    {
        var playerModel = this.GetModel<PlayerModel>();
        var saveData = new SaveData
        {
            PlayerName = playerModel.Name.Value,
            Level = playerModel.Level.Value,
            Gold = playerModel.Gold.Value
        };
        // 保存逻辑...
    }

    public void LoadGame()
    {
        var playerModel = this.GetModel<PlayerModel>();
        // 加载逻辑...
    }
}
```

## 上下文注入机制

### 自动注入流程

1. **组件创建** - 框架创建 Model、System 等组件，CQRS runtime 创建请求处理器
2. **上下文注入** - 框架调用组件的 `SetContext()` 方法，传入 `IArchitectureContext`
3. **初始化回调** - `ContextAwareBase` 在 `SetContext()` 中调用 `OnContextReady()` 钩子
4. **能力访问** - 组件现在可以通过扩展方法访问所有架构服务

### 未绑定实例

GetContext 只返回当前实例明确绑定的上下文。未绑定时抛出 InvalidOperationException；即使进程中存在其他 Architecture 或显式 GameContext 绑定，也不会隐式选择它们。生成的 ContextAware 实现使用相同的实例绑定规则，不再共享静态 provider。

```csharp
IArchitectureContext IContextAware.GetContext()
{
    return Context ?? throw new InvalidOperationException(
        "Bind this instance through IContextAware.SetContext before accessing it.");
}
```

## 设计优势

### 1. 简洁性

框架只定义了一个核心接口 `IContextAware`，包含两个方法。所有其他能力都通过扩展方法提供，使得接口定义保持简洁易懂。

### 2. 灵活性

扩展方法可以在不修改接口的情况下添加新能力。需要新功能时，只需添加新的扩展方法，无需修改 `IContextAware` 接口。

### 3. 一致性

所有框架组件使用相同的方式访问架构服务，通过 `IContextAware` 接口和扩展方法。这提供了统一的编程体验。

### 4. 可扩展性

自定义组件可以轻松继承 `ContextAwareBase` 并使用所有扩展方法，与框架组件保持一致的设计。

## 最佳实践

1. **继承 ContextAwareBase** - 自定义组件应继承 `ContextAwareBase` 而不是直接实现 `IContextAware`
2. **使用 OnContextReady()** - 在 `OnContextReady()` 中进行初始化，而不是在构造函数中
3. **使用扩展方法** - 通过扩展方法访问架构服务，不要手动存储 `IArchitectureContext` 引用
4. **避免循环依赖** - 在设计系统和模型时，避免创建循环依赖关系
5. **遵循单一职责** - 每个组件应该有明确的职责，不要让一个组件做太多事情

## 相关包

- [`architecture`](./architecture.md) - 定义 `IArchitectureContext` 接口
- [`command`](./command.md) - 写操作请求的 CQRS 表达
- [`query`](./query.md) - 读操作请求的 CQRS 表达
- [`model`](./model.md) - Model 继承 `AbstractModel` (实现 `IContextAware`)
- [`system`](./system.md) - System 继承 `AbstractSystem` (实现 `IContextAware`)
- [`extensions`](./extensions.md) - 提供 `ContextAwareExtensions` 扩展方法

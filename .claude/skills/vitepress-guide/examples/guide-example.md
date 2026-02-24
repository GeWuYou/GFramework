---
title: IoC 容器使用指南
description: IoC（控制反转）容器提供了轻量级的依赖注入功能，用于管理框架中各种组件的注册和获取。
---

# IoC 容器使用指南

## 概述

IoC（Inversion of Control，控制反转）包提供了一个轻量级的依赖注入容器，用于管理框架中各种组件的注册和获取。通过 IoC 容器，可以实现组件间的解耦，便于测试和维护。

IoC 容器是 GFramework 架构的核心组件之一，为整个框架提供依赖管理和组件解析服务。

**主要特性**：
- 类型安全的依赖管理
- 支持单例和多实例注册
- 线程安全操作
- 容器冻结保护
- 自动接口注册

## 核心概念

### 依赖注入

依赖注入是一种设计模式，通过容器管理对象的创建和依赖关系，而不是在代码中直接创建对象。

```csharp
// 不使用依赖注入
public class GameController
{
    private PlayerModel model = new PlayerModel(); // 硬编码依赖
}

// 使用依赖注入
public class GameController : IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Start()
    {
        var model = this.GetModel<PlayerModel>(); // 从容器获取
    }
}
```

### 容器注册

在架构初始化时，将组件注册到容器中：

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册 Model
        RegisterModel(new PlayerModel());

        // 注册 System
        RegisterSystem(new GameplaySystem());

        // 注册 Utility
        RegisterUtility(new StorageUtility());
    }
}
```

### 容器解析

通过扩展方法从容器中获取已注册的组件：

```csharp
// 在 Controller 中
var playerModel = this.GetModel<PlayerModel>();
var gameplaySystem = this.GetSystem<GameplaySystem>();
var storageUtility = this.GetUtility<StorageUtility>();
```

## 基本用法

### 注册组件

```csharp
var container = new IocContainer();

// 注册单例（一个类型只能有一个实例）
container.RegisterSingleton<IPlayerModel>(new PlayerModel());

// 注册多实例（一个类型可以有多个实例）
container.RegisterPlurality<IEnemy>(new Goblin());
container.RegisterPlurality<IEnemy>(new Orc());
container.RegisterPlurality<IEnemy>(new Dragon());
```

### 获取组件

```csharp
// 获取单例
var playerModel = container.Get<IPlayerModel>();

// 获取多实例集合
var enemies = container.GetAll<IEnemy>(); // 返回 List<IEnemy>
```

### 在架构中使用

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册组件
        RegisterModel(new PlayerModel());
        RegisterModel(new InventoryModel());
        RegisterSystem(new GameplaySystem());
    }
}

// 在 Controller 中使用
public class GameController : IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void Start()
    {
        // 通过扩展方法获取组件
        var playerModel = this.GetModel<PlayerModel>();
        var inventoryModel = this.GetModel<InventoryModel>();
        var gameplaySystem = this.GetSystem<GameplaySystem>();
    }
}
```

## 高级用法

### 容器冻结

容器在架构初始化完成后会被冻结，防止运行时修改：

```csharp
var container = new IocContainer();
container.Register<IPlayerModel>(new PlayerModel());

// 冻结容器
container.Freeze();

// 以下操作会抛出 InvalidOperationException
// container.Register<IGameSystem>(new GameSystem());
```

### 多实例管理

```csharp
// 注册多个同类型实例
container.RegisterPlurality<IWeapon>(new Sword());
container.RegisterPlurality<IWeapon>(new Bow());
container.RegisterPlurality<IWeapon>(new Staff());

// 获取所有实例
var allWeapons = container.GetAll<IWeapon>();
foreach (var weapon in allWeapons)
{
    weapon.Attack();
}
```

### 接口自动注册

注册实例时，容器会自动将其注册到所有实现的接口：

```csharp
public class PlayerModel : IModel, IPlayerModel, IDisposable
{
    // ...
}

// 注册实例
container.Register<PlayerModel>(new PlayerModel());

// 可以通过任何接口获取
var model1 = container.Get<IModel>();
var model2 = container.Get<IPlayerModel>();
var model3 = container.Get<IDisposable>();
// 以上三个变量指向同一个实例
```

### 线程安全操作

容器的所有操作都是线程安全的：

```csharp
// 多线程环境下安全使用
Parallel.For(0, 100, i =>
{
    var model = container.Get<IPlayerModel>();
    model.DoSomething();
});
```

## 最佳实践

1. **使用接口注册**：优先使用接口类型注册，而不是具体类型
   ```csharp
   ✓ container.Register<IPlayerModel>(new PlayerModel());
   ✗ container.Register<PlayerModel>(new PlayerModel());
   ```

2. **单例 vs 多实例**：根据需求选择合适的注册方式
   - 单例：全局唯一的服务（如配置、管理器）
   - 多实例：可以有多个实例的对象（如敌人、道具）

3. **避免循环依赖**：组件之间不应该相互依赖
   ```csharp
   ✗ System A 依赖 System B，System B 又依赖 System A
   ✓ 使用事件系统进行通信，避免直接依赖
   ```

4. **在 Init 中注册**：所有组件应该在架构的 `Init()` 方法中注册
   ```csharp
   protected override void Init()
   {
       // 在这里注册所有组件
       RegisterModel(new PlayerModel());
       RegisterSystem(new GameplaySystem());
   }
   ```

5. **使用扩展方法**：通过扩展方法获取组件，代码更简洁
   ```csharp
   ✓ var model = this.GetModel<PlayerModel>();
   ✗ var model = this.GetArchitecture().GetModel<PlayerModel>();
   ```

6. **不要在运行时注册**：容器冻结后不应该再注册新组件
   ```csharp
   ✗ 在游戏运行时动态注册组件
   ✓ 在架构初始化时注册所有需要的组件
   ```

## 常见问题

### 问题：如何判断使用单例还是多实例？

**解答**：
- 使用单例（`RegisterSingleton`）：全局唯一的服务，如 PlayerModel、GameConfiguration
- 使用多实例（`RegisterPlurality`）：可以有多个实例的对象，如 Enemy、Weapon

### 问题：容器冻结后如何添加新组件？

**解答**：
容器冻结是为了保护架构稳定性。如果需要动态添加组件，应该：
1. 在架构初始化时预先注册所有可能需要的组件
2. 使用对象池模式管理动态对象
3. 考虑使用工厂模式创建临时对象

### 问题：如何处理组件的生命周期？

**解答**：
- 实现 `IDisposable` 接口的组件会在架构销毁时自动释放
- 架构会按注册的逆序销毁组件
- 不需要手动管理组件的生命周期

### 问题：可以在容器中注册值类型吗？

**解答**：
可以，但会发生装箱。建议将值类型包装在类中：
```csharp
// 不推荐
container.Register<int>(42);

// 推荐
public class GameConfig
{
    public int MaxPlayers { get; set; } = 42;
}
container.Register<GameConfig>(new GameConfig());
```

## 相关文档

- [架构组件](/zh-CN/core/architecture) - 架构基础
- [Model 层](/zh-CN/core/model) - 数据模型
- [System 层](/zh-CN/core/system) - 业务系统
- [Utility 工具类](/zh-CN/core/utility) - 工具类

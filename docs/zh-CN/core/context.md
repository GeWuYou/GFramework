# Context 上下文指南

## 概述

Context（上下文）是 GFramework 中的核心概念，提供了对架构服务的统一访问入口。通过 Context，组件可以访问事件总线、命令总线、查询总线、IoC
容器等核心服务。

## 核心接口

### IArchitectureContext

架构上下文接口，定义了对架构服务的访问契约。

**核心属性：**

```csharp
IEventBus EventBus { get; }              // 事件总线
ICommandBus CommandBus { get; }          // 命令总线
IQueryBus QueryBus { get; }              // 查询总线
IIocContainer Container { get; }         // IoC 容器
IEnvironment Environment { get; }        // 环境配置
IArchitectureConfiguration Configuration { get; }  // 架构配置
ILogger Logger { get; }                  // 日志系统
```

## 核心类

### ArchitectureContext

架构上下文的完整实现。

**使用示例：**

```csharp
// 通过架构获取上下文
var context = architecture.Context;

// 访问各个服务
var eventBus = context.EventBus;
var commandBus = context.CommandBus;
var queryBus = context.QueryBus;
var container = context.Container;
var environment = context.Environment;
var logger = context.Logger;
```

### GameContext

游戏上下文类，管理架构类型与上下文实例的映射关系。

**核心方法：**

```csharp
// 绑定架构类型到上下文
static void Bind<TArchitecture>(IArchitectureContext context)
    where TArchitecture : IArchitecture;

// 获取架构类型对应的上下文
static IArchitectureContext GetContext<TArchitecture>()
    where TArchitecture : IArchitecture;

// 解绑架构类型
static void Unbind<TArchitecture>()
    where TArchitecture : IArchitecture;
```

## 在组件中使用 Context

### 在 Model 中使用

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);

    protected override void OnInit()
    {
        // 通过 Context 访问事件总线
        var context = this.GetContext();
        var eventBus = context.EventBus;

        // 监听生命值变化
        Health.Register(hp =>
        {
            if (hp <= 0)
            {
                // 发送事件
                eventBus.Send(new PlayerDiedEvent());
            }
        });
    }
}
```

### 在 System 中使用

```csharp
public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 通过 Context 访问各个服务
        var context = this.GetContext();
        var eventBus = context.EventBus;
        var commandBus = context.CommandBus;
        var container = context.Container;

        // 注册事件监听
        eventBus.Register<EnemyAttackEvent>(OnEnemyAttack);
    }

    private void OnEnemyAttack(EnemyAttackEvent e)
    {
        var context = this.GetContext();
        var playerModel = context.Container.Get<PlayerModel>();

        // 处理伤害
        playerModel.Health.Value -= e.Damage;
    }
}
```

### 在 Command 中使用

```csharp
public class StartGameCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 通过 Context 访问服务
        var context = this.GetContext();
        var container = context.Container;
        var eventBus = context.EventBus;

        var playerModel = container.Get<PlayerModel>();
        playerModel.Health.Value = playerModel.MaxHealth.Value;

        eventBus.Send(new GameStartedEvent());
    }
}
```

### 在 Query 中使用

```csharp
public class GetPlayerHealthQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        // 通过 Context 访问容器
        var context = this.GetContext();
        var playerModel = context.Container.Get<PlayerModel>();

        return playerModel.Health.Value;
    }
}
```

## GameContext 的使用

### 绑定架构到 GameContext

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册组件
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
    }
}

// 在应用启动时绑定
var architecture = new GameArchitecture();
await architecture.InitializeAsync();

// 绑定架构到 GameContext
GameContext.Bind<GameArchitecture>(architecture.Context);
```

### 从 GameContext 获取上下文

```csharp
// 在任何地方获取架构上下文
var context = GameContext.GetContext<GameArchitecture>();

// 访问服务
var playerModel = context.Container.Get<PlayerModel>();
var eventBus = context.EventBus;
```

### 使用 GameContext 的扩展方法

```csharp
// 通过扩展方法简化访问
public static class GameContextExtensions
{
    public static T GetModel<T>(this IArchitectureContext context)
        where T : class, IModel
    {
        return context.Container.Get<T>();
    }

    public static T GetSystem<T>(this IArchitectureContext context)
        where T : class, ISystem
    {
        return context.Container.Get<T>();
    }
}

// 使用
var context = GameContext.GetContext<GameArchitecture>();
var playerModel = context.GetModel<PlayerModel>();
var combatSystem = context.GetSystem<CombatSystem>();
```

## Context 中的服务

### EventBus - 事件总线

```csharp
var context = architecture.Context;
var eventBus = context.EventBus;

// 注册事件
eventBus.Register<PlayerDiedEvent>(e =>
{
    Console.WriteLine("Player died!");
});

// 发送事件
eventBus.Send(new PlayerDiedEvent());
```

### CommandBus - 命令总线

```csharp
var context = architecture.Context;
var commandBus = context.CommandBus;

// 发送命令
commandBus.Send(new StartGameCommand());

// 发送带返回值的命令
var damage = commandBus.Send(new CalculateDamageCommand { Input = input });
```

### QueryBus - 查询总线

```csharp
var context = architecture.Context;
var queryBus = context.QueryBus;

// 发送查询
var health = queryBus.Send(new GetPlayerHealthQuery { Input = new EmptyQueryInput() });
```

### Container - IoC 容器

```csharp
var context = architecture.Context;
var container = context.Container;

// 获取已注册的组件
var playerModel = container.Get<PlayerModel>();
var combatSystem = container.Get<CombatSystem>();

// 获取所有实现某接口的组件
var allSystems = container.GetAll<ISystem>();
```

### Environment - 环境配置

```csharp
var context = architecture.Context;
var environment = context.Environment;

// 获取环境值
var gameMode = environment.Get<string>("GameMode");
var maxPlayers = environment.Get<int>("MaxPlayers");

// 安全获取值
if (environment.TryGet<string>("ServerAddress", out var address))
{
    Console.WriteLine($"Server: {address}");
}
```

### Logger - 日志系统

```csharp
var context = architecture.Context;
var logger = context.Logger;

// 记录日志
logger.Log("Game started");
logger.LogWarning("Low memory");
logger.LogError("Failed to load resource");
```

## Context 的生命周期

### 创建

Context 在架构初始化时自动创建：

```csharp
var architecture = new GameArchitecture();
// Context 在这里被创建
var context = architecture.Context;
```

### 使用

Context 在架构的整个生命周期中可用：

```csharp
// 初始化期间
await architecture.InitializeAsync();

// Ready 阶段
var context = architecture.Context;
var playerModel = context.Container.Get<PlayerModel>();

// 销毁前
architecture.Destroy();
```

### 销毁

Context 随着架构的销毁而销毁：

```csharp
architecture.Destroy();
// Context 不再可用
```

## 最佳实践

### 1. 通过扩展方法简化访问

```csharp
public static class ContextExtensions
{
    public static T GetModel<T>(this IArchitectureContext context)
        where T : class, IModel
    {
        return context.Container.Get<T>();
    }

    public static T GetSystem<T>(this IArchitectureContext context)
        where T : class, ISystem
    {
        return context.Container.Get<T>();
    }

    public static void SendCommand(this IArchitectureContext context, ICommand command)
    {
        context.CommandBus.Send(command);
    }

    public static TResult SendQuery<TResult>(this IArchitectureContext context, IQuery<TResult> query)
    {
        return context.QueryBus.Send(query);
    }
}

// 使用
var context = architecture.Context;
var playerModel = context.GetModel<PlayerModel>();
context.SendCommand(new StartGameCommand());
```

### 2. 缓存 Context 引用

```csharp
public class GameSystem : AbstractSystem
{
    private IArchitectureContext _context;

    protected override void OnInit()
    {
        // 缓存 Context 引用
        _context = this.GetContext();

        // 后续使用缓存的引用
        _context.EventBus.Register<GameStartedEvent>(OnGameStarted);
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        var playerModel = _context.Container.Get<PlayerModel>();
    }
}
```

### 3. 使用 GameContext 实现全局访问

```csharp
// 在应用启动时绑定
public class GameBootstrapper
{
    public async Task StartAsync()
    {
        var architecture = new GameArchitecture();
        await architecture.InitializeAsync();

        // 绑定到 GameContext
        GameContext.Bind<GameArchitecture>(architecture.Context);
    }
}

// 在任何地方访问
public class UIController
{
    public void UpdateHealthDisplay()
    {
        var context = GameContext.GetContext<GameArchitecture>();
        var playerModel = context.Container.Get<PlayerModel>();

        // 更新 UI
        healthText.text = playerModel.Health.Value.ToString();
    }
}
```

### 4. 处理 Context 不可用的情况

```csharp
public class SafeGameSystem : AbstractSystem
{
    protected override void OnInit()
    {
        try
        {
            var context = this.GetContext();
            if (context == null)
            {
                Console.WriteLine("Context not available");
                return;
            }

            var playerModel = context.Container.Get<PlayerModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing context: {ex.Message}");
        }
    }
}
```

## Context vs Architecture

### Architecture

- **职责**：管理组件的生命周期
- **作用**：注册、初始化、销毁组件
- **访问**：通过 `GetArchitecture()` 获取

### Context

- **职责**：提供对架构服务的访问
- **作用**：访问事件总线、命令总线、查询总线等
- **访问**：通过 `GetContext()` 获取

```csharp
// Architecture 用于管理
var architecture = GameArchitecture.Interface;
architecture.RegisterModel(new PlayerModel());

// Context 用于访问服务
var context = architecture.Context;
var playerModel = context.Container.Get<PlayerModel>();
```

## 相关包

- [`architecture`](./architecture.md) - 架构核心，创建和管理 Context
- [`ioc`](./ioc.md) - IoC 容器，通过 Context 访问
- [`events`](./events.md) - 事件总线，通过 Context 访问
- [`command`](./command.md) - 命令总线，通过 Context 访问
- [`query`](./query.md) - 查询总线，通过 Context 访问
- [`environment`](./environment.md) - 环境配置，通过 Context 访问
- [`logging`](./logging.md) - 日志系统，通过 Context 访问

---

**许可证**：Apache 2.0

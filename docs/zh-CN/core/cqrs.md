---
title: CQRS
description: GFramework 内建 CQRS runtime，用统一请求分发、通知发布和流式处理组织业务逻辑。
---

# CQRS

## 概述

CQRS（Command Query Responsibility Segregation，命令查询职责分离）是一种架构模式，将数据的读取（Query）和修改（Command）操作分离。GFramework
当前内建自有 CQRS runtime，通过统一的请求分发器、通知发布和流式请求管道提供类型安全、解耦的业务逻辑处理方式。

通过 CQRS，你可以将复杂的业务逻辑拆分为独立的命令和查询处理器，每个处理器只负责单一职责，使代码更易于测试和维护。

**主要特性**：

- 命令查询职责分离
- 内建请求分发与解耦设计
- 支持管道行为（Behaviors）
- 异步处理支持
- 与架构系统深度集成
- 支持流式处理

## 核心概念

### Command（命令）

命令表示修改系统状态的操作，如创建、更新、删除：

```csharp
using GFramework.Core.CQRS.Command;
using GFramework.Core.Abstractions.CQRS.Command;

// 定义命令输入
public class CreatePlayerInput : ICommandInput
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 定义命令
public class CreatePlayerCommand : CommandBase<CreatePlayerInput, int>
{
    public CreatePlayerCommand(CreatePlayerInput input) : base(input) { }
}
```

### Query（查询）

查询表示读取系统状态的操作，不修改数据：

```csharp
using GFramework.Core.CQRS.Query;
using GFramework.Core.Abstractions.CQRS.Query;

// 定义查询输入
public class GetPlayerInput : IQueryInput
{
    public int PlayerId { get; set; }
}

// 定义查询
public class GetPlayerQuery : QueryBase<GetPlayerInput, PlayerData>
{
    public GetPlayerQuery(GetPlayerInput input) : base(input) { }
}
```

### Handler（处理器）

处理器负责执行命令或查询的具体逻辑：

```csharp
using GFramework.Core.CQRS.Command;

// 命令处理器
public class CreatePlayerCommandHandler : AbstractCommandHandler<CreatePlayerCommand, int>
{
    public override async ValueTask<int> Handle(
        CreatePlayerCommand command,
        CancellationToken cancellationToken)
    {
        var input = command.Input;
        var playerModel = this.GetModel<PlayerModel>();

        // 创建玩家
        var playerId = playerModel.CreatePlayer(input.Name, input.Level);

        return playerId;
    }
}
```

### Dispatcher（请求分发器）

架构上下文会负责将命令、查询和通知路由到对应的处理器：

```csharp
// 通过架构上下文发送命令
var command = new CreatePlayerCommand(new CreatePlayerInput
{
    Name = "Player1",
    Level = 1
});

var playerId = await this.SendAsync(command);
```

## 基本用法

### 定义和发送命令

```csharp
// 1. 定义命令输入
public class SaveGameInput : ICommandInput
{
    public int SlotId { get; set; }
    public GameData Data { get; set; }
}

// 2. 定义命令
public class SaveGameCommand : CommandBase<SaveGameInput, Unit>
{
    public SaveGameCommand(SaveGameInput input) : base(input) { }
}

// 3. 实现命令处理器
public class SaveGameCommandHandler : AbstractCommandHandler<SaveGameCommand>
{
    public override async ValueTask<Unit> Handle(
        SaveGameCommand command,
        CancellationToken cancellationToken)
    {
        var input = command.Input;
        var saveSystem = this.GetSystem<SaveSystem>();

        // 保存游戏
        await saveSystem.SaveAsync(input.SlotId, input.Data);

        // 发送事件
        this.SendEvent(new GameSavedEvent { SlotId = input.SlotId });

        return Unit.Value;
    }
}

// 4. 发送命令
public async Task SaveGame()
{
    var command = new SaveGameCommand(new SaveGameInput
    {
        SlotId = 1,
        Data = currentGameData
    });

    await this.SendAsync(command);
}
```

### 定义和发送查询

```csharp
// 1. 定义查询输入
public class GetHighScoresInput : IQueryInput
{
    public int Count { get; set; } = 10;
}

// 2. 定义查询
public class GetHighScoresQuery : QueryBase<GetHighScoresInput, List<ScoreData>>
{
    public GetHighScoresQuery(GetHighScoresInput input) : base(input) { }
}

// 3. 实现查询处理器
public class GetHighScoresQueryHandler : AbstractQueryHandler<GetHighScoresQuery, List<ScoreData>>
{
    public override async ValueTask<List<ScoreData>> Handle(
        GetHighScoresQuery query,
        CancellationToken cancellationToken)
    {
        var input = query.Input;
        var scoreModel = this.GetModel<ScoreModel>();

        // 查询高分榜
        var scores = await scoreModel.GetTopScoresAsync(input.Count);

        return scores;
    }
}

// 4. 发送查询
public async Task<List<ScoreData>> GetHighScores()
{
    var query = new GetHighScoresQuery(new GetHighScoresInput
    {
        Count = 10
    });

    var scores = await this.SendQueryAsync(query);
    return scores;
}
```

### 注册处理器

在架构中注册 CQRS 行为；默认会自动接入当前架构所在程序集和 `GFramework.Core` 程序集中的处理器：

```csharp
public class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        // 注册通用开放泛型行为
        RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();
        RegisterCqrsPipelineBehavior<PerformanceBehavior<,>>();

        // 默认只自动扫描当前架构程序集和 GFramework.Core 程序集中的处理器
    }
}
```

当前版本会优先使用源码生成的程序集级 handler registry 来注册“当前业务程序集”里的处理器；
如果该程序集没有生成注册器，或者包含生成代码无法合法引用的处理器类型，则会自动回退到运行时反射扫描。
`GFramework.Core` 等未挂接该生成器的程序集仍会继续走反射扫描。

如果处理器位于其他模块或扩展程序集中，需要额外接入对应程序集的处理器注册，而不是只依赖默认接入范围：

```csharp
public class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();

        RegisterCqrsHandlersFromAssemblies(
        [
            typeof(InventoryCqrsMarker).Assembly,
            typeof(BattleCqrsMarker).Assembly
        ]);
    }
}
```

`RegisterCqrsHandlersFromAssembly(...)` / `RegisterCqrsHandlersFromAssemblies(...)` 会复用与默认启动路径相同的注册逻辑：
优先使用程序集级生成注册器，失败时自动回退到反射扫描；如果同一程序集已经由默认路径或其他模块接入，框架会自动去重，避免重复注册
handler。

`RegisterCqrsPipelineBehavior<TBehavior>()` 是推荐入口；旧的 `RegisterMediatorBehavior<TBehavior>()`
仅作为兼容名称保留，当前已标记为 `Obsolete` 并从 IntelliSense 主路径隐藏，计划在未来 major 版本中移除。
`ContextAwareMediator*Extensions` 与 `MediatorCoroutineExtensions` 也遵循同样的弃用节奏。当前接口支持两种形式：

- 开放泛型行为，例如 `LoggingBehavior<,>`，用于匹配所有请求
- 封闭行为类型，例如某个只服务于单一请求的 `SpecialBehavior`

## 高级用法

### Request（请求）

Request 是更通用的消息类型，可以用于任何场景：

```csharp
using GFramework.Core.CQRS.Request;
using GFramework.Core.Abstractions.CQRS.Request;

// 定义请求输入
public class ValidatePlayerInput : IRequestInput
{
    public string PlayerName { get; set; }
}

// 定义请求
public class ValidatePlayerRequest : RequestBase<ValidatePlayerInput, bool>
{
    public ValidatePlayerRequest(ValidatePlayerInput input) : base(input) { }
}

// 实现请求处理器
public class ValidatePlayerRequestHandler : AbstractRequestHandler<ValidatePlayerRequest, bool>
{
    public override async ValueTask<bool> Handle(
        ValidatePlayerRequest request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var playerModel = this.GetModel<PlayerModel>();

        // 验证玩家名称
        var isValid = await playerModel.IsNameValidAsync(input.PlayerName);

        return isValid;
    }
}
```

### Notification（通知）

Notification 用于一对多的消息广播：

```csharp
using GFramework.Core.CQRS.Notification;
using GFramework.Core.Abstractions.CQRS.Notification;

// 定义通知输入
public class PlayerLevelUpInput : INotificationInput
{
    public int PlayerId { get; set; }
    public int NewLevel { get; set; }
}

// 定义通知
public class PlayerLevelUpNotification : NotificationBase<PlayerLevelUpInput>
{
    public PlayerLevelUpNotification(PlayerLevelUpInput input) : base(input) { }
}

// 实现通知处理器 1
public class AchievementNotificationHandler : AbstractNotificationHandler<PlayerLevelUpNotification>
{
    public override async ValueTask Handle(
        PlayerLevelUpNotification notification,
        CancellationToken cancellationToken)
    {
        var input = notification.Input;
        // 检查成就
        CheckLevelAchievements(input.PlayerId, input.NewLevel);
        await Task.CompletedTask;
    }
}

// 实现通知处理器 2
public class RewardNotificationHandler : AbstractNotificationHandler<PlayerLevelUpNotification>
{
    public override async ValueTask Handle(
        PlayerLevelUpNotification notification,
        CancellationToken cancellationToken)
    {
        var input = notification.Input;
        // 发放奖励
        GiveRewards(input.PlayerId, input.NewLevel);
        await Task.CompletedTask;
    }
}

// 发布通知（所有处理器都会收到）
var notification = new PlayerLevelUpNotification(new PlayerLevelUpInput
{
    PlayerId = 1,
    NewLevel = 10
});

await this.PublishAsync(notification);
```

### Pipeline Behaviors（管道行为）

Behaviors 可以在处理器执行前后添加横切关注点：

```csharp
using GFramework.Core.Abstractions.Cqrs;

// 日志行为
public class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageName = message.GetType().Name;
        Console.WriteLine($"[开始] {messageName}");

        var response = await next(message, cancellationToken);

        Console.WriteLine($"[完成] {messageName}");

        return response;
    }
}

// 性能监控行为
public class PerformanceBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(message, cancellationToken);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (elapsed > 100)
        {
            Console.WriteLine($"警告: {message.GetType().Name} 耗时 {elapsed}ms");
        }

        return response;
    }
}

// 注册行为
RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();
RegisterCqrsPipelineBehavior<PerformanceBehavior<,>>();
```

### 验证行为

```csharp
public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // 验证输入
        if (message is IValidatable validatable)
        {
            var errors = validatable.Validate();
            if (errors.Any())
            {
                throw new ValidationException(errors);
            }
        }

        return await next(message, cancellationToken);
    }
}
```

### 流式处理

处理大量数据时使用流式处理：

```csharp
// 流式查询
public class GetAllPlayersStreamQuery : QueryBase<EmptyInput, IAsyncEnumerable<PlayerData>>
{
    public GetAllPlayersStreamQuery() : base(new EmptyInput()) { }
}

// 流式查询处理器
public class GetAllPlayersStreamQueryHandler : AbstractStreamQueryHandler<GetAllPlayersStreamQuery, PlayerData>
{
    public override async IAsyncEnumerable<PlayerData> Handle(
        GetAllPlayersStreamQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var playerModel = this.GetModel<PlayerModel>();

        await foreach (var player in playerModel.GetAllPlayersAsync(cancellationToken))
        {
            yield return player;
        }
    }
}

// 使用流式查询
var query = new GetAllPlayersStreamQuery();
var stream = this.CreateStream(query);

await foreach (var player in stream)
{
    Console.WriteLine($"玩家: {player.Name}");
}
```

## 最佳实践

1. **命令和查询分离**：严格区分修改和读取操作
   ```csharp
   ✓ CreatePlayerCommand, GetPlayerQuery // 职责清晰
   ✗ PlayerCommand // 职责不明确
   ```

2. **使用有意义的命名**：命令用动词，查询用 Get
   ```csharp
   ✓ CreatePlayerCommand, UpdateScoreCommand, GetHighScoresQuery
   ✗ PlayerCommand, ScoreCommand, ScoresQuery
   ```

3. **输入验证**：在处理器中验证输入
   ```csharp
   public override async ValueTask<int> Handle(...)
   {
       if (string.IsNullOrEmpty(command.Input.Name))
           throw new ArgumentException("Name is required");

       // 处理逻辑
   }
   ```

4. **使用 Behaviors 处理横切关注点**：日志、性能、验证等
   ```csharp
   RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();
   RegisterCqrsPipelineBehavior<ValidationBehavior<,>>();
   ```

5. **保持处理器简单**：一个处理器只做一件事
   ```csharp
   ✓ 处理器只负责业务逻辑，通过架构组件访问数据
   ✗ 处理器中包含复杂的数据访问和业务逻辑
   ```

6. **使用 CancellationToken**：支持操作取消
   ```csharp
   public override async ValueTask<T> Handle(..., CancellationToken cancellationToken)
   {
       await someAsyncOperation(cancellationToken);
   }
   ```

## 常见问题

### 问题：Command 和 Query 有什么区别？

**解答**：

- **Command**：修改系统状态，可能有副作用，通常返回 void 或简单结果
- **Query**：只读取数据，无副作用，返回查询结果

```csharp
// Command: 修改状态
CreatePlayerCommand -> 创建玩家
UpdateScoreCommand -> 更新分数

// Query: 读取数据
GetPlayerQuery -> 获取玩家信息
GetHighScoresQuery -> 获取高分榜
```

### 问题：什么时候使用 Request？

**解答**：
Request 是更通用的消息类型，当操作既不是纯命令也不是纯查询时使用：

```csharp
// 验证操作：读取数据并返回结果，但不修改状态
ValidatePlayerRequest

// 计算操作：基于输入计算结果
CalculateDamageRequest
```

### 问题：Notification 和 Event 有什么区别？

**解答**：

- **Notification**：通过内建 CQRS runtime 发送，处理器在同一请求上下文中执行
- **Event**：通过 EventBus 发送，监听器异步执行

```csharp
// Notification: 同步处理
await this.PublishAsync(notification); // 等待所有处理器完成

// Event: 异步处理
this.SendEvent(event); // 立即返回，监听器异步执行
```

### 问题：如何处理命令失败？

**解答**：
使用异常或返回 Result 类型：

```csharp
// 方式 1: 抛出异常
public override async ValueTask<Unit> Handle(...)
{
    if (!IsValid())
        throw new InvalidOperationException("Invalid operation");

    return Unit.Value;
}

// 方式 2: 返回 Result
public override async ValueTask<Result> Handle(...)
{
    if (!IsValid())
        return Result.Failure("Invalid operation");

    return Result.Success();
}
```

### 问题：处理器可以调用其他处理器吗？

**解答**：
可以，通过架构上下文继续发送新的命令或查询：

```csharp
public override async ValueTask<Unit> Handle(...)
{
    // 调用其他命令
    await this.SendAsync(new AnotherCommand(...));

    return Unit.Value;
}
```

### 问题：如何测试处理器？

**解答**：
处理器是独立的类，易于单元测试：

```csharp
[Test]
public async Task CreatePlayer_ShouldReturnPlayerId()
{
    // Arrange
    var handler = new CreatePlayerCommandHandler();
    handler.SetContext(mockContext);

    var command = new CreatePlayerCommand(new CreatePlayerInput
    {
        Name = "Test",
        Level = 1
    });

    // Act
    var playerId = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.That(playerId, Is.GreaterThan(0));
}
```

## 相关文档

- [命令系统](/zh-CN/core/command) - 传统命令模式
- [查询系统](/zh-CN/core/query) - 传统查询模式
- [事件系统](/zh-CN/core/events) - 事件驱动架构
- [协程系统](/zh-CN/core/coroutine) - 在协程中使用 CQRS

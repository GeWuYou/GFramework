---
title: 状态机系统
description: 状态机系统提供了灵活的状态管理机制，支持状态转换、历史记录和异步操作。
---

# 状态机系统

## 概述

状态机系统是 GFramework 中用于管理游戏状态的核心组件。通过状态机，你可以清晰地定义游戏的各种状态（如菜单、游戏中、暂停、游戏结束等），以及状态之间的转换规则，使游戏逻辑更加结构化和易于维护。

状态机系统支持同步和异步状态操作，提供状态历史记录，并与架构系统深度集成，让你可以在状态中访问所有架构组件。

**主要特性**：

- 类型安全的状态管理
- 支持同步和异步状态
- 状态转换验证
- 状态历史记录和回退
- 与架构系统集成
- 线程安全操作

## 核心概念

### 状态接口

`IState` 定义了状态的基本行为：

```csharp
public interface IState
{
    void OnEnter(IState? from);      // 进入状态
    void OnExit(IState? to);         // 退出状态
    bool CanTransitionTo(IState target); // 转换验证
}
```

### 状态机

`IStateMachine` 管理状态的注册和切换：

```csharp
public interface IStateMachine
{
    IState? Current { get; }                    // 当前状态
    IStateMachine Register(IState state);       // 注册状态
    Task<bool> ChangeToAsync<T>() where T : IState; // 切换状态
}
```

### 状态机系统

`IStateMachineSystem` 结合了状态机和系统的能力：

```csharp
public interface IStateMachineSystem : ISystem, IStateMachine
{
    // 继承 ISystem 和 IStateMachine 的所有功能
}
```

## 基本用法

### 定义状态

继承 `ContextAwareStateBase` 创建状态：

```csharp
using GFramework.Core.State;

// 菜单状态
public class MenuState : ContextAwareStateBase
{
    public override void OnEnter(IState? from)
    {
        Console.WriteLine("进入菜单");
        // 显示菜单 UI
    }

    public override void OnExit(IState? to)
    {
        Console.WriteLine("退出菜单");
        // 隐藏菜单 UI
    }
}

// 游戏状态
public class GameplayState : ContextAwareStateBase
{
    public override void OnEnter(IState? from)
    {
        Console.WriteLine("开始游戏");
        // 初始化游戏场景
    }

    public override void OnExit(IState? to)
    {
        Console.WriteLine("结束游戏");
        // 清理游戏场景
    }
}
```

### 注册和使用状态机

```csharp
using GFramework.Core.State;

public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 创建状态机系统
        var stateMachine = new StateMachineSystem();

        // 注册状态
        stateMachine
            .Register(new MenuState())
            .Register(new GameplayState())
            .Register(new PauseState());

        // 注册到架构
        RegisterSystem<IStateMachineSystem>(stateMachine);
    }
}
```

### 切换状态

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class GameController : IController
{
    public async Task StartGame()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();

        // 切换到游戏状态
        var success = await stateMachine.ChangeToAsync<GameplayState>();

        if (success)
        {
            Console.WriteLine("成功进入游戏状态");
        }
    }
}
```

## 高级用法

### 状态转换验证

控制状态之间的转换规则：

```csharp
public class GameplayState : ContextAwareStateBase
{
    public override bool CanTransitionTo(IState target)
    {
        // 只能从游戏状态转换到暂停或游戏结束状态
        return target is PauseState or GameOverState;
    }

    public override void OnEnter(IState? from)
    {
        Console.WriteLine($"从 {from?.GetType().Name ?? "初始"} 进入游戏");
    }
}

public class PauseState : ContextAwareStateBase
{
    public override bool CanTransitionTo(IState target)
    {
        // 暂停状态只能返回游戏状态
        return target is GameplayState;
    }
}
```

### 异步状态

处理需要异步操作的状态：

```csharp
using GFramework.Core.Abstractions.State;

public class LoadingState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        Console.WriteLine("开始加载...");

        // 异步加载资源
        await LoadResourcesAsync();

        Console.WriteLine("加载完成");

        // 自动切换到下一个状态
        var stateMachine = this.GetSystem<IStateMachineSystem>();
        await stateMachine.ChangeToAsync<GameplayState>();
    }

    private async Task LoadResourcesAsync()
    {
        // 模拟异步加载
        await Task.Delay(2000);
    }

    public override async Task OnExitAsync(IState? to)
    {
        Console.WriteLine("退出加载状态");
        await Task.CompletedTask;
    }
}
```

### 状态历史和回退

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class GameController : IController
{
    public async Task NavigateBack()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();

        // 回退到上一个状态
        var success = await stateMachine.GoBackAsync();

        if (success)
        {
            Console.WriteLine("已返回上一个状态");
        }
    }

    public void ShowHistory()
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();

        // 获取状态历史
        var history = stateMachine.GetStateHistory();

        Console.WriteLine("状态历史:");
        foreach (var state in history)
        {
            Console.WriteLine($"- {state.GetType().Name}");
        }
    }
}
```

### 在状态中访问架构组件

```csharp
public class GameplayState : ContextAwareStateBase
{
    public override void OnEnter(IState? from)
    {
        // 访问 Model
        var playerModel = this.GetModel<PlayerModel>();
        playerModel.Reset();

        // 访问 System
        var audioSystem = this.GetSystem<AudioSystem>();
        audioSystem.PlayBGM("gameplay");

        // 发送事件
        this.SendEvent(new GameStartedEvent());
    }

    public override void OnExit(IState? to)
    {
        // 停止音乐
        var audioSystem = this.GetSystem<AudioSystem>();
        audioSystem.StopBGM();

        // 发送事件
        this.SendEvent(new GameEndedEvent());
    }
}
```

### 状态数据传递

```csharp
// 定义带数据的状态
public class GameplayState : ContextAwareStateBase
{
    public int Level { get; set; }
    public string Difficulty { get; set; } = "Normal";

    public override void OnEnter(IState? from)
    {
        Console.WriteLine($"开始关卡 {Level}，难度: {Difficulty}");
    }
}

// 切换状态并设置数据
public async Task StartLevel(int level, string difficulty)
{
    var stateMachine = this.GetSystem<IStateMachineSystem>();

    // 获取状态实例并设置数据
    var gameplayState = stateMachine.GetState<GameplayState>();
    if (gameplayState != null)
    {
        gameplayState.Level = level;
        gameplayState.Difficulty = difficulty;
    }

    // 切换状态
    await stateMachine.ChangeToAsync<GameplayState>();
}
```

### 状态事件通知

```csharp
// 定义状态变更事件
public class StateChangedEvent
{
    public IState? From { get; set; }
    public IState To { get; set; }
}

// 自定义状态机系统
public class CustomStateMachineSystem : StateMachineSystem
{
    protected override async Task OnStateChangedAsync(IState? from, IState to)
    {
        // 发送状态变更事件
        this.SendEvent(new StateChangedEvent
        {
            From = from,
            To = to
        });

        await base.OnStateChangedAsync(from, to);
    }
}
```

### 条件状态转换

```csharp
public class BattleState : ContextAwareStateBase
{
    public override bool CanTransitionTo(IState target)
    {
        // 战斗中不能直接退出，必须先结束战斗
        if (target is MenuState)
        {
            var battleModel = this.GetModel<BattleModel>();
            return battleModel.IsBattleEnded;
        }

        return true;
    }
}

// 尝试切换状态
public async Task TryExitBattle()
{
    var stateMachine = this.GetSystem<IStateMachineSystem>();

    // 检查是否可以切换
    var canChange = await stateMachine.CanChangeToAsync<MenuState>();

    if (canChange)
    {
        await stateMachine.ChangeToAsync<MenuState>();
    }
    else
    {
        Console.WriteLine("战斗尚未结束，无法退出");
    }
}
```

## 最佳实践

1. **使用基类创建状态**：继承 `ContextAwareStateBase` 或 `AsyncContextAwareStateBase`
   ```csharp
   ✓ public class MyState : ContextAwareStateBase { }
   ✗ public class MyState : IState { } // 需要手动实现所有接口
   ```

2. **在 OnEnter 中初始化，在 OnExit 中清理**：保持状态的独立性
   ```csharp
   public override void OnEnter(IState? from)
   {
       // 初始化状态相关资源
       LoadUI();
       StartBackgroundMusic();
   }

   public override void OnExit(IState? to)
   {
       // 清理状态相关资源
       UnloadUI();
       StopBackgroundMusic();
   }
   ```

3. **使用转换验证控制状态流**：避免非法状态转换
   ```csharp
   public override bool CanTransitionTo(IState target)
   {
       // 定义明确的转换规则
       return target is AllowedState1 or AllowedState2;
   }
   ```

4. **异步操作使用异步状态**：避免阻塞主线程
   ```csharp
   ✓ public class LoadingState : AsyncContextAwareStateBase
   {
       public override async Task OnEnterAsync(IState? from)
       {
           await LoadDataAsync();
       }
   }

   ✗ public class LoadingState : ContextAwareStateBase
   {
       public override void OnEnter(IState? from)
       {
           LoadDataAsync().Wait(); // 阻塞主线程
       }
   }
   ```

5. **合理使用状态历史**：避免历史记录过大
   ```csharp
   // 创建状态机时设置历史大小
   var stateMachine = new StateMachineSystem(maxHistorySize: 10);
   ```

6. **状态保持单一职责**：每个状态只负责一个场景或功能
   ```csharp
   ✓ MenuState, GameplayState, PauseState // 职责清晰
   ✗ GameState // 职责不明确，包含太多逻辑
   ```

## 常见问题

### 问题：状态切换失败怎么办？

**解答**：
`ChangeToAsync` 返回 `false` 表示切换失败，通常是因为 `CanTransitionTo` 返回 `false`：

```csharp
var success = await stateMachine.ChangeToAsync<TargetState>();
if (!success)
{
    Console.WriteLine("状态切换被拒绝");
    // 检查转换规则
}
```

### 问题：如何在状态之间传递数据？

**解答**：
有几种方式：

1. **通过状态属性**：

```csharp
var state = stateMachine.GetState<GameplayState>();
state.Level = 5;
await stateMachine.ChangeToAsync<GameplayState>();
```

2. **通过 Model**：

```csharp
// 在切换前设置 Model
var gameModel = this.GetModel<GameModel>();
gameModel.CurrentLevel = 5;

// 在状态中读取
public override void OnEnter(IState? from)
{
    var gameModel = this.GetModel<GameModel>();
    var level = gameModel.CurrentLevel;
}
```

3. **通过事件**：

```csharp
this.SendEvent(new LevelSelectedEvent { Level = 5 });
await stateMachine.ChangeToAsync<GameplayState>();
```

### 问题：状态机系统和普通状态机有什么区别？

**解答**：

- **StateMachine**：纯状态机，不依赖架构
- **StateMachineSystem**：集成到架构中，状态可以访问所有架构组件

```csharp
// 使用 StateMachineSystem（推荐）
RegisterSystem<IStateMachineSystem>(new StateMachineSystem());

// 使用 StateMachine（独立使用）
var stateMachine = new StateMachine();
```

### 问题：如何处理状态切换动画？

**解答**：
在 `OnExit` 和 `OnEnter` 中使用协程：

```csharp
public class MenuState : AsyncContextAwareStateBase
{
    public override async Task OnExitAsync(IState? to)
    {
        // 播放淡出动画
        await PlayFadeOutAnimation();
    }
}

public class GameplayState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        // 播放淡入动画
        await PlayFadeInAnimation();
    }
}
```

### 问题：可以在状态中切换到其他状态吗？

**解答**：
可以，但要注意避免递归切换：

```csharp
public override async void OnEnter(IState? from)
{
    // 检查条件后自动切换
    if (ShouldSkip())
    {
        var stateMachine = this.GetSystem<IStateMachineSystem>();
        await stateMachine.ChangeToAsync<NextState>();
    }
}
```

### 问题：状态机是线程安全的吗？

**解答**：
是的，状态机的所有操作都是线程安全的，使用了内部锁机制。

### 问题：如何实现状态栈（多层状态）？

**解答**：
使用状态历史功能：

```csharp
// 进入子状态
await stateMachine.ChangeToAsync<SubMenuState>();

// 返回上一层
await stateMachine.GoBackAsync();
```

## 相关文档

- [生命周期管理](/zh-CN/core/lifecycle.md) - 状态的初始化和销毁
- [事件系统](/zh-CN/core/events.md) - 状态变更通知
- [协程系统](/zh-CN/core/coroutine.md) - 异步状态操作
- [状态机实现教程](/zh-CN/tutorials/state-machine-tutorial.md) - 完整示例

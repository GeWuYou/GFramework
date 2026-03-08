---
title: 实现状态机
description: 学习如何使用状态机系统管理游戏状态和场景切换
---

# 实现状态机

## 学习目标

完成本教程后，你将能够：

- 理解状态机的概念和应用场景
- 创建自定义游戏状态
- 实现状态之间的转换和验证
- 使用异步状态处理加载操作
- 在状态中访问架构组件
- 实现完整的游戏流程控制

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法和 async/await
- 阅读过[快速开始](/zh-CN/getting-started/quick-start)
- 了解[生命周期管理](/zh-CN/core/lifecycle)

## 步骤 1：定义游戏状态

首先，让我们为一个简单的游戏定义几个基本状态：主菜单、加载、游戏中、暂停和游戏结束。

```csharp
using GFramework.Core.Abstractions.state;
using GFramework.Core.state;

namespace MyGame.States
{
    /// <summary>
    /// 主菜单状态
    /// </summary>
    public class MenuState : ContextAwareStateBase
    {
        public override void OnEnter(IState? from)
        {
            Console.WriteLine("=== 进入主菜单 ===");

            // 显示菜单 UI
            ShowMenuUI();

            // 播放菜单音乐
            PlayMenuMusic();
        }

        public override void OnExit(IState? to)
        {
            Console.WriteLine("退出主菜单");

            // 隐藏菜单 UI
            HideMenuUI();
        }

        public override bool CanTransitionTo(IState target)
        {
            // 菜单只能切换到加载状态
            return target is LoadingState;
        }

        private void ShowMenuUI()
        {
            Console.WriteLine("显示菜单界面");
        }

        pd HideMenuUI()
        {
            Console.WriteLine("隐藏菜单界面");
        }

        private void PlayMenuMusic()
        {
            Console.WriteLine("播放菜单音乐");
        }
    }

    /// <summary>
    /// 游戏中状态
    /// </summary>
    public class GameplayState : ContextAwareStateBase
    {
        public override void OnEnter(IState? from)
        {
            Console.WriteLine("=== 开始游戏 ===");

            // 初始化游戏场景
            InitializeGameScene();

            // 重置玩家数据
            ResetPlayerData();

      // 播放游戏音乐
            PlayGameMusic();
        }

        public override void OnExit(IState? to)
        {
            Console.WriteLine("结束游戏");

            // 保存游戏进度（如果不是游戏结束）
            if (to is not GameOverState)
            {
                SaveGameProgress();
            }
        }

        public override bool CanTransitionTo(IState target)
        {
            // 游戏中可以切换到暂停或游戏结束状态
            return target is PauseState or GameOverState;
        }

        private void InitializeGameScene()
        {
            Console.WriteLine("初始化游戏场景");
        }

        private void ResetPlayerData()
        {
            Console.WriteLine("重置玩家数据");
        }

        private void PlayGameMusic()
        {
            Console.WriteLine("播放游戏音乐");
        }

        private void SaveGameProgress()
        {
            Console.WriteLine("保存游戏进度");
        }
    }

    /// <summary>
    /// 暂停状态
    /// </summary>
    public class PauseState : ContextAwareStateBase
    {
        public override void OnEnter(IState? from)
        {
            Console.WriteLine("=== 游戏暂停 ===");

            // 显示暂停菜单
            ShowPauseMenu();

            // 暂停游戏逻辑
            PauseGameLogic();
        }

        public override void OnExit(IState? to)
        {
            Console.WriteLine("取消暂停");

            // 隐藏暂停菜单
            HidePauseMenu();

            // 恢复游戏逻辑
            ResumeGameLogic();
        }

        public override bool CanTransitionTo(IState target)
        {
            // 暂停状态可以返回游戏或退出到菜单
            return target is GameplayState or MenuState;
        }

        private void ShowPauseMenu()
        {
            Console.WriteLine("显示暂停菜单");
        }

        private void HidePauseMenu()
        {
            Console.WriteLine("隐藏暂停菜单");
        }

        private void PauseGameLogic()
        {
            Console.WriteLine("暂停游戏逻辑");
        }

        private void ResumeGameLogic()
        {
            Console.WriteLine("恢复游戏逻辑");
        }
    }

    /// <summary>
    /// 游戏结束状态
    /// </summary>
    public class GameOverState : ContextAwareStateBase
    {
        public bool IsVictory { get; set; }

        public override void OnEnter(IState? from)
        {
            Console.WriteLine(IsVictory
                ? "=== 游戏胜利 ==="
                : "=== 游戏失败 ===");

            // 显示结算界面
            ShowGameOverUI();

            // 播放结算音乐
            PlayGameOverMusic();
        }

        public override void OnExit(IState? to)
        {
            Console.WriteLine("退出结算界面");

            // 隐藏结算界面
            HideGameOverUI();
        }

        public override bool CanTransitionTo(IState target)
        {
            // 游戏结束只能返回菜单
            return target is MenuState;
        }

        private void ShowGameOverUI()
        {
            Console.WriteLine("显示结算界面");
        }

        private void HideGameOverUI()
        {
            Console.WriteLine("隐藏结算界面");
        }

        private void PlayGameOverMusic()
        {
            Console.WriteLine("播放结算音乐");
        }
    }
}
```

**代码说明**：

- 继承 `ContextAwareStateBase` 创建状态
- `OnEnter` 在进入状态时调用，用于初始化
- `OnExit` 在退出状态时调用，用于清理
- `CanTransitionTo` 定义允许的状态转换规则
- 每个状态职责单一，逻辑清晰

## 步骤 2：创建异步加载状态

实现一个异步加载状态，用于加载游戏资源。

```csharp
using GFramework.Core.state;
using System.Threading.Tasks;

namespace MyGame.States
{
    /// <summary>
    /// 加载状态（异步）
    /// </summary>
    public class LoadingState : AsyncContextAwareStateBase
    {
        public int TargetLevel { get; set; } = 1;

        public override async Task OnEnterAsync(IState? from)
        {
            Console.WriteLine($"=== 开始加载关卡 {TargetLevel} ===");

            // 显示加载界面
            ShowLoadingUI();

            // 异步加载资源
            await LoadResourcesAsync();

            // 加载完成后自动切换到游戏状态
            Console.WriteLine("加载完成，进入游戏");
            var stateMachine = this.GetSystem<IStateMachineSystem>();
            await stateMachine.ChangeToAsync<GameplayState>();
        }

        public override async Task OnExitAsync(IState? to)
        {
            Console.WriteLine("退出加载状态");

            // 隐藏加载界面
            HideLoadingUI();

            await Task.CompletedTask;
        }

        public override bool CanTransitionTo(IState target)
        {
            // 加载状态只能切换到游戏状态
            return target is GameplayState;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        private async Task LoadResourcesAsync()
        {
            // 加载纹理
            Console.WriteLine("加载纹理资源...");
            await Task.Delay(500);
            Console.WriteLine("纹理加载完成 (33%)");

            // 加载音频
            Console.WriteLine("加载音频资源...");
            await Task.Delay(500);
            Console.WriteLine("音频加载完成 (66%)");

            // 加载关卡数据
            Console.WriteLine("加载关卡数据...");
            await Task.Delay(500);
            Console.WriteLine("关卡数据加载完成 (100%)");
        }

        private void ShowLoadingUI()
        {
            Console.WriteLine("显示加载界面");
        }

        private void HideLoadingUI()
        {
            Console.WriteLine("隐藏加载界面");
        }
    }
}
```

**代码说明**：

- 继承 `AsyncContextAwareStateBase` 支持异步操作
- `OnEnterAsync` 和 `OnExitAsync` 是异步方法
- 可以在状态中使用 `await` 等待异步操作
- 加载完成后自动切换到下一个状态

## 步骤 3：注册状态机系统

在架构中注册状态机系统和所有状态。

```csharp
using GFramework.Core.architecture;
using GFramework.Core.Abstractions.state;
using GFramework.Core.state;
using MyGame.States;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void Init()
        {
            Interface = this;

            // 创建状态机系统
            var stateMachine = new StateMachineSystem();

            // 注册所有状态
            stateMachine
                .Register(new MenuState())
                .Register(new LoadingState())
                .Register(new GameplayState())
                .Register(new PauseState())
                .Register(new GameOverState());

            // 注册状态机系统到架构
            RegisterSystem<IStateMachineSystem>(stateMachine);

            Console.WriteLine("状态机系统初始化完成");
        }
    }
}
```

**代码说明**：

- 创建 `StateMachineSystem` 实例
- 使用链式调用注册所有状态
- 将状态机注册为 `IStateMachineSystem` 服务
- 状态会自动获得架构上下文

## 步骤 4：创建游戏控制器

创建控制器来管理游戏流程和状态切换。

```csharp
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.Abstractions.state;
using GFramework.Core.extensions;
using MyGame.States;
using System.Threading.Tasks;
using GFramework.SourceGenerators.Abstractions.rule;

namespace MyGame.Controllers
{
    [ContextAware]
    public partial class GameFlowController : IController
    {
        /// <summary>
        /// 开始游戏
        /// </summary>
        public async Task StartGame(int level = 1)
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();

            // 设置加载状态的目标关卡
            var loadingState = stateMachine.GetState<LoadingState>();
            if (loadingState != null)
            {
                loadingState.TargetLevel = level;
            }

            // 切换到加载状态
            var success = await stateMachine.ChangeToAsync<LoadingState>();

            if (!success)
            {
                Console.WriteLine("无法开始游戏：状态切换失败");
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public async Task PauseGame()
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();

            // 检查当前是否在游戏状态
            if (stateMachine.Current is not GameplayState)
            {
                Console.WriteLine("当前不在游戏中，无法暂停");
                return;
            }

            // 切换到暂停状态
            await stateMachine.ChangeToAsync<PauseState>();
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public async Task ResumeGame()
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();

            // 检查当前是否在暂停状态
            if (stateMachine.Current is not PauseState)
            {
                Console.WriteLine("当前不在暂停状态");
                return;
            }

            // 返回游戏状态
            await stateMachine.ChangeToAsync<GameplayState>();
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        public async Task EndGame(bool isVictory)
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();

            // 设置游戏结束状态的数据
            var gameOverState = stateMachine.GetState<GameOverState>();
            if (gameOverState != null)
            {
                gameOverState.IsVictory = isVictory;
            }

            // 切换到游戏结束状态
            await stateMachine.ChangeToAsync<GameOverState>();
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public async Task ReturnToMenu()
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();

            // 切换到菜单状态
            var success = await stateMachine.ChangeToAsync<MenuState>();

            if (!success)
            {
                Console.WriteLine("无法返回菜单：状态切换被拒绝");
            }
        }

        /// <summary>
        /// 显示当前状态
        /// </summary>
        public void ShowCurrentState()
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();
            var currentState = stateMachine.Current;

            if (currentState != null)
            {
                Console.WriteLine($"当前状态: {currentState.GetType().Name}");
            }
            else
            {
                Console.WriteLine("当前没有活动状态");
            }
        }

        /// <summary>
        /// 显示状态历史
        /// </summary>
        public void ShowStateHistory()
        {
            var stateMachine = this.GetSystem<IStateMachineSystem>();
            var history = stateMachine.GetStateHistory();

            Console.WriteLine("状态历史:");
            foreach (var state in history)
            {
                Console.WriteLine($"  - {state.GetType().Name}");
            }
        }
    }
}
```

**代码说明**：

- 实现 `IController` 接口
- 提供各种游戏流程控制方法
- 使用 `GetState<T>()` 获取状态实例并设置数据
- 使用 `ChangeToAsync<T>()` 切换状态
- 检查切换结果并处理失败情况

## 步骤 5：测试状态机

编写测试代码验证状态机功能。

```csharp
using MyGame;
using MyGame.Controllers;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 状态机系统测试 ===\n");

        // 1. 初始化架构
        var architecture = new GameArchitecture();
        architecture.Initialize();
        await architecture.WaitUntilReadyAsync();

        // 2. 创建游戏流程控制器
        var gameFlow = new GameFlowController();

        // 3. 切换到主菜单
        Console.WriteLine("\n--- 测试 1: 进入主菜单 ---");
        await gameFlow.ReturnToMenu();
        gameFlow.ShowCurrentState();

        await Task.Delay(1000);

        // 4. 开始游戏（会经过加载状态）
        Console.WriteLine("\n--- 测试 2: 开始游戏 ---");
        await gameFlow.StartGame(level: 1);
        await Task.Delay(2000); // 等待加载完成
        gameFlow.ShowCurrentState();

        await Task.Delay(1000);

        // 5. 暂停游戏
        Console.WriteLine("\n--- 测试 3: 暂停游戏 ---");
        await gameFlow.PauseGame();
        gameFlow.ShowCurrentState();

        await Task.Delay(1000);

        // 6. 恢复游戏
        Console.WriteLine("\n--- 测试 4: 恢复游戏 ---");
        await gameFlow.ResumeGame();
        gameFlow.ShowCurrentState();

        await Task.Delay(1000);

        // 7. 游戏胜利
        Console.WriteLine("\n--- 测试 5: 游戏胜利 ---");
        await gameFlow.EndGame(isVictory: true);
        gameFlow.ShowCurrentState();

        await Task.Delay(1000);

        // 8. 返回菜单
        Console.WriteLine("\n--- 测试 6: 返回菜单 ---");
        await gameFlow.ReturnToMenu();
        gameFlow.ShowCurrentState();

        // 9. 显示状态历史
        Console.WriteLine("\n--- 状态历史 ---");
        gameFlow.ShowStateHistory();

        Console.WriteLine("\n=== 测试完成 ===");
    }
}
```

**代码说明**：

- 初始化架构并等待就绪
- 按顺序测试各种状态切换
- 使用 `Task.Delay` 模拟游戏运行
- 验证状态转换规则和历史记录

## 完整代码

所有代码文件已在上述步骤中提供。项目结构如下：

```
MyGame/
├── States/
│   ├── MenuState.cs
│   ├── LoadingState.cs
│   ├── GameplayState.cs
│   ├── PauseState.cs
│   └── GameOverState.cs
├── Controllers/
│   └── GameFlowController.cs
├── GameArchitecture.cs
└── Program.cs
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```
=== 状态机系统测试 ===

状态机系统初始化完成

--- 测试 1: 进入主菜单 ---
=== 进入主菜单 ===
显示菜单界面
播放菜单音乐
当前状态: MenuState

--- 测试 2: 开始游戏 ---
退出主菜单
隐藏菜单界面
=== 开始加载关卡 1 ===
显示加载界面
加载纹理资源...
纹理加载完成 (33%)
加载音频资源...
音频加载完成 (66%)
加载关卡数据...
关卡数据加载完成 (100%)
加载完成，进入游戏
退出加载状态
隐藏加载界面
=== 开始游戏 ===
初始化游戏场景
重置玩家数据
播放游戏音乐
当前状态: GameplayState

--- 测试 3: 暂停游戏 ---
结束游戏
保存游戏进度
=== 游戏暂停 ===
显示暂停菜单
暂停游戏逻辑
当前状态: PauseState

--- 测试 4: 恢复游戏 ---
取消暂停
隐藏暂停菜单
恢复游戏逻辑
=== 开始游戏 ===
初始化游戏场景
重置玩家数据
播放游戏音乐
当前状态: GameplayState

--- 测试 5: 游戏胜利 ---
结束游戏
=== 游戏胜利 ===
显示结算界面
播放结算音乐
当前状态: GameOverState

--- 测试 6: 返回菜单 ---
退出结算界面
隐藏结算界面
=== 进入主菜单 ===
显示菜单界面
播放菜单音乐
当前状态: MenuState

--- 状态历史 ---
状态历史:
  - MenuState
  - LoadingState
  - GameplayState
  - PauseState
  - GameplayState
  - GameOverState
  - MenuState

=== 测试完成 ===
```

**验证步骤**：

1. 所有状态切换成功执行
2. 状态转换规则正确生效
3. 异步加载状态正常工作
4. 状态历史记录完整
5. 状态数据传递正确

## 下一步

恭喜！你已经掌握了状态机系统的使用。接下来可以学习：

- [使用协程系统](/zh-CN/tutorials/coroutine-tutorial) - 在状态中使用协程
- [资源管理最佳实践](/zh-CN/tutorials/resource-management) - 在加载状态中管理资源
- [实现存档系统](/zh-CN/tutorials/save-system) - 保存和恢复游戏状态

## 相关文档

- [状态机系统](/zh-CN/core/state-machine) - 状态机详细说明
- [生命周期管理](/zh-CN/core/lifecycle) - 组件生命周期
- [System 层](/zh-CN/core/system) - System 详细说明
- [架构组件](/zh-CN/core/architecture) - 架构基础

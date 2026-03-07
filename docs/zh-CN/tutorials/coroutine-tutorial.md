---
title: 使用协程系统
description: 学习如何使用协程系统实现异步操作和时间控制
---

# 使用协程系统

## 学习目标

完成本教程后，你将能够：

- 理解协程的基本概念和执行机制
- 创建和启动协程
- 使用各种等待指令控制协程执行
- 在架构组件中使用协程
- 实现常见的游戏逻辑（延迟执行、循环任务、事件等待）

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法和迭代器（IEnumerator）
- 阅读过[架构概览](/zh-CN/getting-started)
- 了解[生命周期管理](/zh-CN/core/lifecycle)

## 步骤 1：创建第一个协程

首先，让我们创建一个简单的协程来理解基本概念。

```csharp
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.instructions;

namespace MyGame.Systems
{
    public class TutorialSystem : AbstractSystem
    {
        protected override void OnInit()
        {
            // 启动协程
            this.StartCoroutine(MyFirstCoroutine());
        }

        /// <summary>
        /// 第一个协程示例
        /// </summary>
        private IEnumerator<IYieldInstruction> MyFirstCoroutine()
        {
            Console.WriteLine("协程开始执行");

            // 等待 1 秒
            yield return CoroutineHelper.WaitForSeconds(1.0);

            Console.WriteLine("1 秒后执行");

            // 等待 1 帧
            yield return CoroutineHelper.WaitForOneFrame();

            Console.WriteLine("下一帧执行");

            // 等待 5 帧
            yield return CoroutineHelper.WaitForFrames(5);

            Console.WriteLine("5 帧后执行");
        }
    }
}
```

**代码说明**：

- 协程方法返回 `IEnumerator<IYieldInstruction>`
- 使用 `yield return` 返回等待指令
- `this.StartCoroutine()` 扩展方法启动协程
- `WaitForSeconds` 等待指定秒数
- `WaitForOneFrame` 等待一帧
- `WaitForFrames` 等待多帧

## 步骤 2：实现生命值自动恢复

让我们实现一个实用的功能：玩家生命值自动恢复。

```csharp
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.property;
using GFramework.Core.model;

namespace MyGame.Models
{
    public class PlayerModel : AbstractModel
    {
        // 当前生命值
        public BindableProperty<int> Health { get; } = new(100);

        // 最大生命值
        public BindableProperty<int> MaxHealth { get; } = new(100);

        // 是否启用自动恢复
        public BindableProperty<bool> AutoRegenEnabled { get; } = new(true);

        private CoroutineHandle? _regenHandle;

        protected override void OnInit()
        {
            // 启动生命值恢复协程
            StartHealthRegeneration();
        }

        /// <summary>
        /// 启动生命值恢复
        /// </summary>
        public void StartHealthRegeneration()
        {
            // 如果已经在运行，先停止
            if (_regenHandle.HasValue)
            {
                this.StopCoroutine(_regenHandle.Value);
            }

            // 启动新的恢复协程
            _regenHandle = this.StartCoroutine(HealthRegenerationCoroutine());
        }

        /// <summary>
        /// 停止生命值恢复
        /// </summary>
        public void StopHealthRegeneration()
        {
            if (_regenHandle.HasValue)
            {
                this.StopCoroutine(_regenHandle.Value);
                _regenHandle = null;
            }
        }

        /// <summary>
        /// 生命值恢复协程
        /// </summary>
        private IEnumerator<IYieldInstruction> HealthRegenerationCoroutine()
        {
            while (true)
            {
                // 等待 1 秒
                yield return CoroutineHelper.WaitForSeconds(1.0);

                // 检查是否启用自动恢复
                if (!AutoRegenEnabled.Value)
                    continue;

                // 如果生命值未满，恢复 5 点
                if (Health.Value < MaxHealth.Value)
                {
                    Health.Value = Math.Min(Health.Value + 5, MaxHealth.Value);
                    Console.WriteLine($"生命值恢复: {Health.Value}/{MaxHealth.Value}");
                }
            }
        }
    }
}
```

**代码说明**：

- 使用 `while (true)` 创建无限循环协程
- 保存协程句柄以便后续控制
- 使用 `StopCoroutine` 停止协程
- 协程中可以访问类成员变量

## 步骤 3：实现技能冷却系统

接下来实现一个技能冷却系统，展示如何使用协程管理时间相关的游戏逻辑。

```csharp
using GFramework.Core.system;
using System.Collections.Generic;

namespace MyGame.Systems
{
    public class SkillSystem : AbstractSystem
    {
        // 技能冷却状态
        private readonly Dictionary<string, bool> _skillCooldowns = new();

        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill(string skillName, double cooldownTime)
        {
            // 检查是否在冷却中
            if (_skillCooldowns.TryGetValue(skillName, out var isOnCooldown) && isOnCooldown)
            {
                Console.WriteLine($"技能 {skillName} 冷却中...");
                return false;
            }

            // 执行技能
            Console.WriteLine($"使用技能: {skillName}");

            // 启动冷却协程
            this.StartCoroutine(SkillCooldownCoroutine(skillName, cooldownTime));

            return true;
        }

        /// <summary>
        /// 技能冷却协程
        /// </summary>
        private IEnumerator<IYieldInstruction> SkillCooldownCoroutine(string skillName, double cooldownTime)
        {
            // 标记为冷却中
            _skillCooldowns[skillName] = true;

            Console.WriteLine($"技能 {skillName} 开始冷却 {cooldownTime} 秒");

            // 等待冷却时间
            yield return CoroutineHelper.WaitForSeconds(cooldownTime);

            // 冷却结束
            _skillCooldowns[skillName] = false;
            Console.WriteLine($"技能 {skillName} 冷却完成");
        }

        /// <summary>
        /// 带进度显示的技能冷却
        /// </summary>
        private IEnumerator<IYieldInstruction> SkillCooldownWithProgressCoroutine(
            string skillName,
            double cooldownTime)
        {
            _skillCooldowns[skillName] = true;

            // 使用 WaitForProgress 显示冷却进度
            yield return CoroutineHelper.WaitForProgress(
                duration: cooldownTime,
                onProgress: progress =>
                {
                    Console.WriteLine($"技能 {skillName} 冷却进度: {progress * 100:F0}%");
                }
            );

            _skillCooldowns[skillName] = false;
            Console.WriteLine($"技能 {skillName} 冷却完成");
        }
    }
}
```

**代码说明**：

- 使用字典管理多个技能的冷却状态
- 每个技能使用独立的协程管理冷却
- `WaitForProgress` 可以在等待期间执行回调
- 协程结束后自动清理冷却状态

## 步骤 4：等待事件触发

实现一个等待玩家完成任务的系统，展示如何在协程中等待事件。

```csharp
using GFramework.Core.Abstractions.events;
using GFramework.Core.coroutine.instructions;

namespace MyGame.Systems
{
    // 任务完成事件
    public record QuestCompletedEvent(int QuestId, string QuestName) : IEvent;

    public class QuestSystem : AbstractSystem
    {
        /// <summary>
        /// 开始任务并等待完成
        /// </summary>
        public void StartQuest(int questId, string questName)
        {
            this.StartCoroutine(QuestCoroutine(questId, questName));
        }

        /// <summary>
        /// 任务协程
        /// </summary>
        private IEnumerator<IYieldInstruction> QuestCoroutine(int questId, string questName)
        {
            Console.WriteLine($"任务开始: {questName}");

            // 获取事件总线
            var eventBus = this.GetService<IEventBus>();

            // 等待任务完成事件
            var waitEvent = new WaitForEvent<QuestCompletedEvent>(
                eventBus,
                evt => evt.QuestId == questId  // 过滤条件
            );

            yield return waitEvent;

            // 获取事件数据
            var completedEvent = waitEvent.EventData;
            Console.WriteLine($"任务完成: {completedEvent.QuestName}");

            // 发放奖励
            GiveReward(questId);
        }

        /// <summary>
        /// 带超时的任务
        /// </summary>
        private IEnumerator<IYieldInstruction> TimedQuestCoroutine(
            int questId,
            string questName,
            double timeLimit)
        {
            Console.WriteLine($"限时任务开始: {questName} (时限: {timeLimit}秒)");

            var eventBus = this.GetService<IEventBus>();

            // 等待事件，带超时
            var waitEvent = new WaitForEventWithTimeout<QuestCompletedEvent>(
                eventBus,
                timeout: timeLimit,
                predicate: evt => evt.QuestId == questId
            );

            yield return waitEvent;

            if (waitEvent.IsTimeout)
            {
                Console.WriteLine($"任务超时失败: {questName}");
            }
            else
            {
                Console.WriteLine($"任务完成: {questName}");
                GiveReward(questId);
            }
        }

        private void GiveReward(int questId)
        {
            Console.WriteLine($"发放任务 {questId} 的奖励");
        }
    }
}
```

**代码说明**：

- `WaitForEvent` 等待特定事件触发
- 可以使用 `predicate` 参数过滤事件
- `WaitForEventWithTimeout` 支持超时机制
- 通过 `EventData` 属性获取事件数据

## 步骤 5：协程组合与嵌套

实现一个复杂的游戏流程，展示如何组合多个协程。

```csharp
namespace MyGame.Systems
{
    public class GameFlowSystem : AbstractSystem
    {
        /// <summary>
        /// 游戏开始流程
        /// </summary>
        public void StartGame()
        {
            this.StartCoroutine(GameStartSequence());
        }

        /// <summary>
        /// 游戏开始序列
        /// </summary>
        private IEnumerator<IYieldInstruction> GameStartSequence()
        {
            Console.WriteLine("=== 游戏开始 ===");

            // 1. 显示标题
            yield return ShowTitle();

            // 2. 加载资源
            yield return LoadResources();

            // 3. 初始化玩家
            yield return InitializePlayer();

            // 4. 播放开场动画
            yield return PlayOpeningAnimation();

            Console.WriteLine("=== 游戏准备完成 ===");
        }

        /// <summary>
        /// 显示标题
        /// </summary>
        private IEnumerator<IYieldInstruction> ShowTitle()
        {
            Console.WriteLine("显示游戏标题...");
            yield return CoroutineHelper.WaitForSeconds(2.0);
            Console.WriteLine("标题显示完成");
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        private IEnumerator<IYieldInstruction> LoadResources()
        {
            Console.WriteLine("开始加载资源...");

            // 并行加载多个资源
            var loadTextures = LoadTexturesCoroutine();
            var loadAudio = LoadAudioCoroutine();
            var loadModels = LoadModelsCoroutine();

            // 等待所有资源加载完成
            yield return new WaitForAllCoroutines(
                this.GetCoroutineScheduler(),
                loadTextures,
                loadAudio,
                loadModels
            );

            Console.WriteLine("所有资源加载完成");
        }

        private IEnumerator<IYieldInstruction> LoadTexturesCoroutine()
        {
            Console.WriteLine("  加载纹理...");
            yield return CoroutineHelper.WaitForSeconds(1.0);
            Console.WriteLine("  纹理加载完成");
        }

        private IEnumerator<IYieldInstruction> LoadAudioCoroutine()
        {
            Console.WriteLine("  加载音频...");
            yield return CoroutineHelper.WaitForSeconds(1.5);
            Console.WriteLine("  音频加载完成");
        }

        private IEnumerator<IYieldInstruction> LoadModelsCoroutine()
        {
            Console.WriteLine("  加载模型...");
            yield return CoroutineHelper.WaitForSeconds(0.8);
            Console.WriteLine("  模型加载完成");
        }

        private IEnumerator<IYieldInstruction> InitializePlayer()
        {
            Console.WriteLine("初始化玩家...");
            yield return CoroutineHelper.WaitForSeconds(0.5);
            Console.WriteLine("玩家初始化完成");
        }

        private IEnumerator<IYieldInstruction> PlayOpeningAnimation()
        {
            Console.WriteLine("播放开场动画...");
            yield return CoroutineHelper.WaitForSeconds(3.0);
            Console.WriteLine("开场动画播放完成");
        }

        /// <summary>
        /// 获取协程调度器
        /// </summary>
        private CoroutineScheduler GetCoroutineScheduler()
        {
            // 从架构服务中获取
            return this.GetService<CoroutineScheduler>();
        }
    }
}
```

**代码说明**：

- 使用 `yield return` 调用其他协程实现嵌套
- `WaitForAllCoroutines` 并行执行多个协程
- 协程可以像函数一样组合和复用
- 清晰的流程控制，避免回调嵌套

## 完整代码

### GameArchitecture.cs

```csharp
using GFramework.Core.architecture;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void Init()
        {
            Interface = this;

            // 注册 Model
            RegisterModel(new PlayerModel());

            // 注册 System
            RegisterSystem(new TutorialSystem());
            RegisterSystem(new SkillSystem());
            RegisterSystem(new QuestSystem());
            RegisterSystem(new GameFlowSystem());
        }
    }
}
```

### 测试代码

```csharp
using MyGame;
using MyGame.Systems;

// 初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();
await architecture.WaitUntilReadyAsync();

// 测试技能系统
var skillSystem = architecture.GetSystem<SkillSystem>();
skillSystem.UseSkill("火球术", 3.0);
await Task.Delay(1000);
skillSystem.UseSkill("火球术", 3.0);  // 冷却中
await Task.Delay(3000);
skillSystem.UseSkill("火球术", 3.0);  // 冷却完成

// 测试任务系统
var questSystem = architecture.GetSystem<QuestSystem>();
questSystem.StartQuest(1, "击败史莱姆");

// 模拟任务完成
await Task.Delay(2000);
var eventBus = architecture.GetService<IEventBus>();
eventBus.Publish(new QuestCompletedEvent(1, "击败史莱姆"));

// 测试游戏流程
var gameFlowSystem = architecture.GetSystem<GameFlowSystem>();
gameFlowSystem.StartGame();
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```
协程开始执行
1 秒后执行
下一帧执行
5 帧后执行

使用技能: 火球术
技能 火球术 开始冷却 3.0 秒
技能 火球术 冷却中...
技能 火球术 冷却完成
使用技能: 火球术

任务开始: 击败史莱姆
任务完成: 击败史莱姆
发放任务 1 的奖励

=== 游戏开始 ===
显示游戏标题...
标题显示完成
开始加载资源...
  加载纹理...
  加载音频...
  加载模型...
  模型加载完成
  纹理加载完成
  音频加载完成
所有资源加载完成
初始化玩家...
玩家初始化完成
播放开场动画...
开场动画播放完成
=== 游戏准备完成 ===
```

**验证步骤**：

1. 协程按预期顺序执行
2. 技能冷却系统正常工作
3. 事件等待功能正确
4. 并行加载资源成功

## 下一步

恭喜！你已经掌握了协程系统的基本用法。接下来可以学习：

- [实现状态机](/zh-CN/tutorials/state-machine-tutorial) - 使用协程实现状态转换
- [资源管理最佳实践](/zh-CN/tutorials/resource-management) - 在协程中加载资源
- [使用命令系统](/zh-CN/tutorials/command-tutorial) - 协程与命令系统集成

## 相关文档

- [协程系统](/zh-CN/core/coroutine) - 协程系统详细说明
- [事件系统](/zh-CN/core/events) - 事件系统详解
- [生命周期管理](/zh-CN/core/lifecycle) - 组件生命周期
- [System 层](/zh-CN/core/system) - System 详细说明

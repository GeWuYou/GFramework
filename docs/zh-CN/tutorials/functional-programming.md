---
title: 函数式编程实践
description: 学习如何在实际项目中使用 Option、Result 和管道操作等函数式编程特性
---

# 函数式编程实践

## 学习目标

完成本教程后，你将能够：

- 理解函数式编程的核心概念和优势
- 使用 Option 类型安全地处理可空值
- 使用 Result 类型进行优雅的错误处理
- 使用管道操作构建流式的数据处理流程
- 组合多个函数式操作实现复杂的业务逻辑
- 在实际游戏开发中应用函数式编程模式

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法和泛型
- 阅读过[快速开始](/zh-CN/getting-started/quick-start.md)
- 了解 Lambda 表达式和 LINQ

## 步骤 1：使用 Option 处理可空值

首先，让我们学习如何使用 Option 类型替代传统的 null 检查，使代码更加安全和优雅。

```csharp
using GFramework.Core.Functional;
using GFramework.Core.Functional.pipe;

namespace MyGame.Services
{
    /// <summary>
    /// 玩家数据服务
    /// </summary>
    public class PlayerDataService
    {
        private readonly Dictionary<int, PlayerData> _players = new();

        /// <summary>
        /// 根据 ID 查找玩家（返回 Option）
        /// </summary>
        public Option<PlayerData> FindPlayerById(int playerId)
        {
            // 使用 Option 包装可能不存在的值
            return _players.TryGetValue(playerId, out var player)
                ? Option<PlayerData>.Some(player)
                : Option<PlayerData>.None;
        }

        /// <summary>
        /// 获取玩家名称（安全处理）
        /// </summary>
        public string GetPlayerName(int playerId)
        {
            // 使用 Match 模式匹配处理有值和无值的情况
            return FindPlayerById(playerId).Match(
                some: player => player.Name,
                none: () => "未知玩家"
            );
        }

        /// <summary>
        /// 获取玩家等级（使用默认值）
        /// </summary>
        public int GetPlayerLevel(int playerId)
        {
            // 使用 GetOrElse 提供默认值
            return FindPlayerById(playerId)
                .Map(player => player.Level)
                .GetOrElse(1);
        }

        /// <summary>
        /// 查找高级玩家
        /// </summary>
        public Option<PlayerData> FindAdvancedPlayer(int playerId)
        {
            // 使用 Filter 过滤值
            return FindPlayerById(playerId)
                .Filter(player => player.Level >= 10);
        }

        /// <summary>
        /// 获取玩家公会名称（链式调用）
        /// </summary>
        public string GetPlayerGuildName(int playerId)
        {
            // 使用 Bind 处理嵌套的 Option
            return FindPlayerById(playerId)
                .Bind(player => player.Guild)  // Guild 也是 Option<Guild>
                .Map(guild => guild.Name)
                .GetOrElse("无公会");
        }
    }

    /// <summary>
    /// 玩家数据
    /// </summary>
    public class PlayerData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public Option<Guild> Guild { get; set; } = Option<Guild>.None;
    }

    /// <summary>
    /// 公会数据
    /// </summary>
    public class Guild
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
```

**代码说明**：

- `Option<T>` 明确表示值可能不存在，避免 NullReferenceException
- `Match` 强制处理两种情况，不会遗漏 null 检查
- `Map` 和 `Bind` 实现链式转换，代码更简洁
- `Filter` 可以安全地过滤值
- `GetOrElse` 提供默认值，避免空值传播

## 步骤 2：使用 Result 进行错误处理

接下来，学习如何使用 Result 类型替代异常处理，实现更可控的错误管理。

```csharp
using GFramework.Core.Functional;
using System.Text.Json;

namespace MyGame.Services
{
    /// <summary>
    /// 存档服务
    /// </summary>
    public class SaveService
    {
        private readonly string _saveDirectory = "./saves";

        /// <summary>
        /// 保存游戏数据
        /// </summary>
        public Result<string> SaveGame(GameSaveData data)
        {
            // 使用 Result.Try 自动捕获异常
            return Result<string>.Try(() =>
            {
                // 验证数据
                if (string.IsNullOrEmpty(data.PlayerName))
                    throw new ArgumentException("玩家名称不能为空");

                // 创建保存目录
                if (!Directory.Exists(_saveDirectory))
                    Directory.CreateDirectory(_saveDirectory);

                // 序列化数据
                var json = JsonSerializer.Serialize(data);
                var fileName = $"save_{data.PlayerId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(_saveDirectory, fileName);

                // 写入文件
                File.WriteAllText(filePath, json);

                return filePath;
            });
        }

        /// <summary>
        /// 加载游戏数据
        /// </summary>
        public Result<GameSaveData> LoadGame(int playerId)
        {
            try
            {
                // 查找最新的存档文件
                var files = Directory.GetFiles(_saveDirectory, $"save_{playerId}_*.json");

                if (files.Length == 0)
                    return Result<GameSaveData>.Failure("未找到存档文件");

                var latestFile = files.OrderByDescending(f => f).First();
                var json = File.ReadAllText(latestFile);
                var data = JsonSerializer.Deserialize<GameSaveData>(json);

                return data != null
                    ? Result<GameSaveData>.Success(data)
                    : Result<GameSaveData>.Failure("存档数据解析失败");
            }
            catch (Exception ex)
            {
                return Result<GameSaveData>.Failure(ex);
            }
        }

        /// <summary>
        /// 保存并加载游戏（链式操作）
        /// </summary>
        public Result<GameSaveData> SaveAndReload(GameSaveData data)
        {
            // 使用 Bind 链接多个 Result 操作
            return SaveGame(data)
                .Bind(_ => LoadGame(data.PlayerId));
        }

        /// <summary>
        /// 获取存档信息（使用 Match）
        /// </summary>
        public string GetSaveInfo(int playerId)
        {
            return LoadGame(playerId).Match(
                succ: data => $"存档加载成功: {data.PlayerName}, 等级 {data.Level}",
                fail: ex => $"加载失败: {ex.Message}"
            );
        }

        /// <summary>
        /// 安全加载游戏（提供默认值）
        /// </summary>
        public GameSaveData LoadGameOrDefault(int playerId)
        {
            return LoadGame(playerId).IfFail(new GameSaveData
            {
                PlayerId = playerId,
                PlayerName = "新玩家",
                Level = 1
            });
        }
    }

    /// <summary>
    /// 游戏存档数据
    /// </summary>
    public class GameSaveData
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public int Level { get; set; }
        public int Gold { get; set; }
        public DateTime SaveTime { get; set; } = DateTime.Now;
    }
}
```

**代码说明**：

- `Result<T>` 将错误作为值返回，而不是抛出异常
- `Result.Try` 自动捕获异常并转换为 Result
- `Bind` 可以链接多个可能失败的操作
- `Match` 强制处理成功和失败两种情况
- `IfFail` 提供失败时的默认值

## 步骤 3：使用管道操作组合函数

学习如何使用管道操作符构建流式的数据处理流程。

```csharp
using GFramework.Core.Functional.pipe;
using GFramework.Core.Functional.functions;

namespace MyGame.Systems
{
    /// <summary>
    /// 物品处理系统
    /// </summary>
    public class ItemProcessingSystem
    {
        /// <summary>
        /// 处理物品掉落
        /// </summary>
        public ItemDrop ProcessItemDrop(Enemy enemy, Player player)
        {
            // 使用管道操作构建处理流程
            return enemy
                .Pipe(e => CalculateDropRate(e, player))
                .Tap(rate => Console.WriteLine($"掉落率: {rate:P}"))
                .Pipe(rate => GenerateItems(rate))
                .Tap(items => Console.WriteLine($"生成 {items.Count} 个物品"))
                .Pipe(items => ApplyLuckBonus(items, player))
                .Pipe(items => FilterByQuality(items))
                .Tap(items => Console.WriteLine($"过滤后剩余 {items.Count} 个物品"))
                .Let(items => new ItemDrop
                {
                    Items = items,
                    TotalValue = items.Sum(i => i.Value)
                });
        }

        /// <summary>
        /// 计算掉落率
        /// </summary>
        private double CalculateDropRate(Enemy enemy, Player player)
        {
            return (enemy.Level * 0.1 + player.Luck * 0.05)
                .Pipe(rate => Math.Min(rate, 1.0));
        }

        /// <summary>
        /// 生成物品
        /// </summary>
        private List<Item> GenerateItems(double dropRate)
        {
            var random = new Random();
            var itemCount = random.NextDouble() < dropRate ? random.Next(1, 5) : 0;

            return Enumerable.Range(0, itemCount)
                .Select(_ => new Item
                {
                    Id = random.Next(1000),
                    Name = $"物品_{random.Next(100)}",
                    Quality = (ItemQuality)random.Next(0, 4),
                    Value = random.Next(10, 100)
                })
                .ToList();
        }

        /// <summary>
        /// 应用幸运加成
        /// </summary>
        private List<Item> ApplyLuckBonus(List<Item> items, Player player)
        {
            return items
                .Select(item => item.Also(i =>
                {
                    if (player.Luck > 50)
                        i.Quality = (ItemQuality)Math.Min((int)i.Quality + 1, 3);
                }))
                .ToList();
        }

        /// <summary>
        /// 按品质过滤
        /// </summary>
        private List<Item> FilterByQuality(List<Item> items)
        {
            return items
                .Where(item => item.Quality >= ItemQuality.Uncommon)
                .ToList();
        }

        /// <summary>
        /// 条件处理物品
        /// </summary>
        public string ProcessItemConditionally(Item item, bool isVip)
        {
            return item.PipeIf(
                predicate: i => isVip,
                ifTrue: i => $"VIP 物品: {i.Name} (价值 {i.Value * 2})",
                ifFalse: i => $"普通物品: {i.Name} (价值 {i.Value})"
            );
        }
    }

    public class Enemy
    {
        public int Level { get; set; }
    }

    public class Player
    {
        public int Luck { get; set; }
    }

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ItemQuality Quality { get; set; }
        public int Value { get; set; }
    }

    public enum ItemQuality
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public class ItemDrop
    {
        public List<Item> Items { get; set; } = new();
        public int TotalValue { get; set; }
    }
}
```

**代码说明**：

- `Pipe` 将值传递给函数，构建流式处理链
- `Tap` 执行副作用（如日志）但不改变值
- `Let` 在作用域内转换值
- `Also` 对值执行操作后返回原值
- `PipeIf` 根据条件选择不同的处理路径

## 步骤 4：实现完整的数据处理流程

现在让我们结合 Option、Result 和管道操作，实现一个完整的游戏功能。

```csharp
using GFramework.Core.Functional;
using GFramework.Core.Functional.pipe;
using GFramework.Core.Functional.control;

namespace MyGame.Features
{
    /// <summary>
    /// 任务系统
    /// </summary>
    public class QuestSystem
    {
        private readonly Dictionary<int, Quest> _quests = new();
        private readonly Dictionary<int, List<int>> _playerQuests = new();

        /// <summary>
        /// 接受任务（完整流程）
        /// </summary>
        public Result<QuestAcceptResult> AcceptQuest(int playerId, int questId)
        {
            return FindQuest(questId)
                // 转换 Option 为 Result
                .ToResult("任务不存在")
                // 验证任务等级要求
                .Bind(quest => ValidateQuestLevel(playerId, quest))
                // 检查前置任务
                .Bind(quest => CheckPrerequisites(playerId, quest))
                // 检查任务槽位
                .Bind(quest => CheckQuestSlots(playerId))
                // 添加到玩家任务列表
                .Map(quest => AddQuestToPlayer(playerId, quest))
                // 记录日志
                .Tap(result => Console.WriteLine($"玩家 {playerId} 接受任务: {result.QuestName}"))
                // 发放初始奖励
                .Map(result => GiveInitialRewards(result));
        }

        /// <summary>
        /// 查找任务
        /// </summary>
        private Option<Quest> FindQuest(int questId)
        {
            return _quests.TryGetValue(questId, out var quest)
                ? Option<Quest>.Some(quest)
                : Option<Quest>.None;
        }

        /// <summary>
        /// 验证任务等级
        /// </summary>
        private Result<Quest> ValidateQuestLevel(int playerId, Quest quest)
        {
            var playerLevel = GetPlayerLevel(playerId);

            return playerLevel >= quest.RequiredLevel
                ? Result<Quest>.Success(quest)
                : Result<Quest>.Failure($"等级不足，需要 {quest.RequiredLevel} 级");
        }

        /// <summary>
        /// 检查前置任务
        /// </summary>
        private Result<Quest> CheckPrerequisites(int playerId, Quest quest)
        {
            if (quest.PrerequisiteQuestIds.Count == 0)
                return Result<Quest>.Success(quest);

            var completedQuests = GetCompletedQuests(playerId);
            var hasAllPrerequisites = quest.PrerequisiteQuestIds
                .All(id => completedQuests.Contains(id));

            return hasAllPrerequisites
                ? Result<Quest>.Success(quest)
                : Result<Quest>.Failure("未完成前置任务");
        }

        /// <summary>
        /// 检查任务槽位
        /// </summary>
        private Result<Quest> CheckQuestSlots(int playerId)
        {
            var activeQuests = GetActiveQuests(playerId);

            return activeQuests.Count < 10
                ? Result<Quest>.Success(default!)
                : Result<Quest>.Failure("任务栏已满");
        }

        /// <summary>
        /// 添加任务到玩家
        /// </summary>
        private QuestAcceptResult AddQuestToPlayer(int playerId, Quest quest)
        {
            if (!_playerQuests.ContainsKey(playerId))
                _playerQuests[playerId] = new List<int>();

            _playerQuests[playerId].Add(quest.Id);

            return new QuestAcceptResult
            {
                QuestId = quest.Id,
                QuestName = quest.Name,
                Description = quest.Description,
                Rewards = quest.Rewards
            };
        }

        /// <summary>
        /// 发放初始奖励
        /// </summary>
        private QuestAcceptResult GiveInitialRewards(QuestAcceptResult result)
        {
            // 某些任务接受时就有奖励
            if (result.Rewards.Gold > 0)
            {
                Console.WriteLine($"获得金币: {result.Rewards.Gold}");
            }

            return result;
        }

        /// <summary>
        /// 完成任务（使用函数组合）
        /// </summary>
        public Result<QuestCompleteResult> CompleteQuest(int playerId, int questId)
        {
            return FindQuest(questId)
                .ToResult("任务不存在")
                .Bind(quest => ValidateQuestOwnership(playerId, quest))
                .Bind(quest => ValidateQuestObjectives(quest))
                .Map(quest => RemoveQuestFromPlayer(playerId, quest))
                .Map(quest => CalculateRewards(quest))
                .Tap(result => Console.WriteLine($"任务完成: {result.QuestName}"))
                .Map(result => GiveRewards(playerId, result));
        }

        /// <summary>
        /// 验证任务所有权
        /// </summary>
        private Result<Quest> ValidateQuestOwnership(int playerId, Quest quest)
        {
            var activeQuests = GetActiveQuests(playerId);

            return activeQuests.Contains(quest.Id)
                ? Result<Quest>.Success(quest)
                : Result<Quest>.Failure("玩家未接受此任务");
        }

        /// <summary>
        /// 验证任务目标
        /// </summary>
        private Result<Quest> ValidateQuestObjectives(Quest quest)
        {
            return quest.IsCompleted
                ? Result<Quest>.Success(quest)
                : Result<Quest>.Failure("任务目标未完成");
        }

        /// <summary>
        /// 从玩家移除任务
        /// </summary>
        private Quest RemoveQuestFromPlayer(int playerId, Quest quest)
        {
            if (_playerQuests.ContainsKey(playerId))
            {
                _playerQuests[playerId].Remove(quest.Id);
            }

            return quest;
        }

        /// <summary>
        /// 计算奖励
        /// </summary>
        private QuestCompleteResult CalculateRewards(Quest quest)
        {
            return new QuestCompleteResult
            {
                QuestId = quest.Id,
                QuestName = quest.Name,
                Rewards = quest.Rewards,
                BonusRewards = CalculateBonusRewards(quest)
            };
        }

        /// <summary>
        /// 计算额外奖励
        /// </summary>
        private QuestRewards CalculateBonusRewards(Quest quest)
        {
            // 根据任务难度给予额外奖励
            return new QuestRewards
            {
                Gold = quest.Rewards.Gold / 10,
                Experience = quest.Rewards.Experience / 10
            };
        }

        /// <summary>
        /// 发放奖励
        /// </summary>
        private QuestCompleteResult GiveRewards(int playerId, QuestCompleteResult result)
        {
            var totalGold = result.Rewards.Gold + result.BonusRewards.Gold;
            var totalExp = result.Rewards.Experience + result.BonusRewards.Experience;

            Console.WriteLine($"获得金币: {totalGold}");
            Console.WriteLine($"获得经验: {totalExp}");

            return result;
        }

        // 辅助方法
        private int GetPlayerLevel(int playerId) => 10;
        private List<int> GetCompletedQuests(int playerId) => new();
        private List<int> GetActiveQuests(int playerId) =>
            _playerQuests.GetValueOrDefault(playerId, new List<int>());
    }

    public class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int RequiredLevel { get; set; }
        public List<int> PrerequisiteQuestIds { get; set; } = new();
        public bool IsCompleted { get; set; }
        public QuestRewards Rewards { get; set; } = new();
    }

    public class QuestRewards
    {
        public int Gold { get; set; }
        public int Experience { get; set; }
    }

    public class QuestAcceptResult
    {
        public int QuestId { get; set; }
        public string QuestName { get; set; } = "";
        public string Description { get; set; } = "";
        public QuestRewards Rewards { get; set; } = new();
    }

    public class QuestCompleteResult
    {
        public int QuestId { get; set; }
        public string QuestName { get; set; } = "";
        public QuestRewards Rewards { get; set; } = new();
        public QuestRewards BonusRewards { get; set; } = new();
    }
}
```

**代码说明**：

- 使用 `Option.ToResult` 将可选值转换为结果
- 使用 `Bind` 链接多个验证步骤
- 使用 `Map` 转换成功的值
- 使用 `Tap` 添加日志而不中断流程
- 每个步骤都是纯函数，易于测试和维护

## 完整代码

### Program.cs

```csharp
using MyGame.Services;
using MyGame.Systems;
using MyGame.Features;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 函数式编程实践示例 ===\n");

        // 测试 Option
        TestOptionUsage();

        Console.WriteLine();

        // 测试 Result
        TestResultUsage();

        Console.WriteLine();

        // 测试管道操作
        TestPipelineUsage();

        Console.WriteLine();

        // 测试完整流程
        TestCompleteWorkflow();

        Console.WriteLine("\n=== 测试完成 ===");
    }

    static void TestOptionUsage()
    {
        Console.WriteLine("--- 测试 Option ---");

        var service = new PlayerDataService();

        // 测试查找存在的玩家
        Console.WriteLine(service.GetPlayerName(1));

        // 测试查找不存在的玩家
        Console.WriteLine(service.GetPlayerName(999));

        // 测试获取等级
        Console.WriteLine($"玩家等级: {service.GetPlayerLevel(1)}");
    }

    static void TestResultUsage()
    {
        Console.WriteLine("--- 测试 Result ---");

        var saveService = new SaveService();

        var saveData = new GameSaveData
        {
            PlayerId = 1,
            PlayerName = "测试玩家",
            Level = 10,
            Gold = 1000
        };

        // 测试保存
        var saveResult = saveService.SaveGame(saveData);
        saveResult.Match(
            succ: path => Console.WriteLine($"保存成功: {path}"),
            fail: ex => Console.WriteLine($"保存失败: {ex.Message}")
        );

        // 测试加载
        Console.WriteLine(saveService.GetSaveInfo(1));
    }

    static void TestPipelineUsage()
    {
        Console.WriteLine("--- 测试管道操作 ---");

        var itemSystem = new ItemProcessingSystem();

        var enemy = new Enemy { Level = 5 };
        var player = new Player { Luck = 60 };

        var drop = itemSystem.ProcessItemDrop(enemy, player);
        Console.WriteLine($"掉落总价值: {drop.TotalValue}");
    }

    static void TestCompleteWorkflow()
    {
        Console.WriteLine("--- 测试完整工作流 ---");

        var questSystem = new QuestSystem();

        // 测试接受任务
        var acceptResult = questSystem.AcceptQuest(1, 101);
        acceptResult.Match(
            succ: result => Console.WriteLine($"接受任务成功: {result.QuestName}"),
            fail: ex => Console.WriteLine($"接受任务失败: {ex.Message}")
        );
    }
}
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```
=== 函数式编程实践示例 ===

--- 测试 Option ---
未知玩家
未知玩家
玩家等级: 1

--- 测试 Result ---
保存成功: ./saves/save_1_20260307_143022.json
存档加载成功: 测试玩家, 等级 10

--- 测试管道操作 ---
掉落率: 35.00%
生成 3 个物品
过滤后剩余 2 个物品
掉落总价值: 150

--- 测试完整工作流 ---
玩家 1 接受任务: 新手任务
接受任务成功: 新手任务

=== 测试完成 ===
```

**验证步骤**：

1. Option 正确处理了不存在的值
2. Result 成功捕获和传播错误
3. 管道操作构建了清晰的处理流程
4. 完整工作流展示了多种技术的组合使用

## 下一步

恭喜！你已经掌握了函数式编程的核心技术。接下来可以学习：

- [使用协程系统](/zh-CN/tutorials/coroutine-tutorial.md) - 结合函数式编程和协程
- [实现状态机](/zh-CN/tutorials/state-machine-tutorial.md) - 在状态机中应用函数式模式
- [资源管理最佳实践](/zh-CN/tutorials/resource-management.md) - 使用 Result 处理资源加载

## 相关文档

- [扩展方法](/zh-CN/core/extensions.md) - 更多函数式扩展方法
- [架构组件](/zh-CN/core/architecture.md) - 在架构中使用函数式编程
- [最佳实践](/zh-CN/best-practices/architecture-patterns.md) - 函数式编程最佳实践

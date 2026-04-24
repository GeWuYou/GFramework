---
title: 实现存档系统
description: 学习如何实现完整的游戏存档系统，支持多槽位和自动保存
---

# 实现存档系统

## 学习目标

完成本教程后，你将能够：

- 理解游戏存档系统的设计原则
- 定义存档数据结构
- 实现多槽位存档管理
- 实现自动保存功能
- 处理存档加载和保存错误
- 实现存档列表和删除功能

## 前置条件

- 已安装 GFramework.Game NuGet 包
- 了解 C# 基础语法和 async/await
- 阅读过[快速开始](/zh-CN/getting-started/quick-start.md)
- 了解[数据与存档系统](/zh-CN/game/data.md)

## 步骤 1：定义存档数据结构

首先，让我们定义游戏存档需要保存的数据结构。

```csharp
using GFramework.Game.Abstractions.Data;
using System;
using System.Collections.Generic;

namespace MyGame.Data
{
    /// <summary>
    /// 玩家数据
    /// </summary>
    public class PlayerData
    {
        public string Name { get; set; } = "Player";
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Gold { get; set; } = 0;
        public Vector3 Position { get; set; } = new();
    }

    /// <summary>
    /// 位置数据
    /// </summary>
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    /// <summary>
    /// 关卡进度数据
    /// </summary>
    public class ProgressData
    {
        public int CurrentLevel { get; set; } = 1;
        public List<int> CompletedLevels { get; set; } = new();
        public Dictionary<string, bool> Achievements { get; set; } = new();
        public float PlayTime { get; set; } = 0f;
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    public class InventoryData
    {
        public List<ItemData> Items { get; set; } = new();
        public List<string> EquippedItems { get; set; } = new();
    }

    /// <summary>
    /// 单个物品数据
    /// </summary>
    public class ItemData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// 完整的存档数据
    /// </summary>
    public class GameSaveData : IVersionedData
    {
        // 数据版本号
        public int Version { get; set; } = 1;

        // 存档元数据
        public DateTime SaveTime { get; set; }
        public string SaveName { get; set; } = "New Save";
        public float TotalPlayTime { get; set; }

        // 游戏数据
        public PlayerData Player { get; set; } = new();
        public ProgressData Progress { get; set; } = new();
        public InventoryData Inventory { get; set; } = new();
    }
}
```

**代码说明**：

- `GameSaveData` 实现 `IVersionedData` 支持版本管理
- 将数据分为玩家、进度、物品等模块
- 包含存档元数据（保存时间、名称等）
- 使用属性初始化器设置默认值

## 步骤 2：创建存档管理系统

实现一个系统来管理存档的创建、加载和保存。

```csharp
using GFramework.Core.System;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Data;
using MyGame.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGame.Systems
{
    /// <summary>
    /// 存档管理系统
    /// </summary>
    public class SaveSystem : AbstractSystem
    {
        private GameSaveData? _currentSave;
        private int _currentSlot = -1;

        /// <summary>
        /// 创建新存档
        /// </summary>
        public GameSaveData CreateNewSave(string saveName)
        {
            Console.WriteLine($"创建新存档: {saveName}");

            var saveData = new GameSaveData
            {
                SaveName = saveName,
                SaveTime = DateTime.Now,
                TotalPlayTime = 0f,
                Player = new PlayerData
                {
                    Name = "Player",
                    Level = 1,
                    Experience = 0,
                    Health = 100,
                    MaxHealth = 100,
                    Gold = 0
                },
                Progress = new ProgressData
                {
                    CurrentLevel = 1,
                    CompletedLevels = new List<int>(),
                    Achievements = new Dictionary<string, bool>(),
                    PlayTime = 0f
                },
                Inventory = new InventoryData
                {
                    Items = new List<ItemData>(),
                    EquippedItems = new List<string>()
                }
            };

            _currentSave = saveData;
            return saveData;
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public async Task<bool> SaveGameAsync(int slot)
        {
            if (_currentSave == null)
            {
                Console.WriteLine("错误: 没有当前存档数据");
                return false;
            }

            try
            {
                Console.WriteLine($"\n=== 保存游戏到槽位 {slot} ===");

                var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();

                // 更新保存时间
                _currentSave.SaveTime = DateTime.Now;
                _currentSlot = slot;

                // 保存到存储
                await saveRepo.SaveAsync(slot, _currentSave);

                Console.WriteLine($"存档已保存: {_currentSave.SaveName}");
                Console.WriteLine($"玩家: {_currentSave.Player.Name}, 等级 {_currentSave.Player.Level}");
                Console.WriteLine($"游戏时间: {_currentSave.TotalPlayTime:F1} 小时");
                Console.WriteLine($"保存时间: {_currentSave.SaveTime}\n");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public async Task<bool> LoadGameAsync(int slot)
        {
            try
            {
                Console.WriteLine($"\n=== 从槽位 {slot} 加载游戏 ===");

                var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();

                // 检查存档是否存在
                if (!await saveRepo.ExistsAsync(slot))
                {
                    Console.WriteLine($"槽位 {slot} 不存在存档\n");
                    return false;
                }

                // 加载存档
                _currentSave = await saveRepo.LoadAsync(slot);
                _currentSlot = slot;

                Console.WriteLine($"存档已加载: {_currentSave.SaveName}");
                Console.WriteLine($"玩家: {_currentSave.Player.Name}, 等级 {_currentSave.Player.Level}");
                Console.WriteLine($"当前关卡: {_currentSave.Progress.CurrentLevel}");
                Console.WriteLine($"金币: {_currentSave.Player.Gold}");
                Console.WriteLine($"物品数量: {_currentSave.Inventory.Items.Count}");
                Console.WriteLine($"保存时间: {_currentSave.SaveTime}\n");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载失败: {ex.Message}\n");
                return false;
            }
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public async Task<bool> DeleteSaveAsync(int slot)
        {
            try
            {
                Console.WriteLine($"\n删除槽位 {slot} 的存档");

                var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();

                if (!await saveRepo.ExistsAsync(slot))
                {
                    Console.WriteLine($"槽位 {slot} 不存在存档\n");
                    return false;
                }

                await saveRepo.DeleteAsync(slot);

                if (_currentSlot == slot)
                {
                    _currentSave = null;
                    _currentSlot = -1;
                }

                Console.WriteLine($"槽位 {slot} 的存档已删除\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除失败: {ex.Message}\n");
                return false;
            }
        }

        /// <summary>
        /// 列出所有存档
        /// </summary>
        public async Task<List<SaveSlotInfo>> ListSavesAsync()
        {
            var result = new List<SaveSlotInfo>();

            try
            {
                var saveRepo = this.GetUtility<ISaveRepository<GameSaveData>>();
                var slots = await saveRepo.ListSlotsAsync();

                Console.WriteLine($"\n=== 存档列表 ({slots.Count} 个) ===");

                foreach (var slot in slots)
                {
                    var saveData = await saveRepo.LoadAsync(slot);

                    var info = new SaveSlotInfo
                    {
                        Slot = slot,
                        SaveName = saveData.SaveName,
                        PlayerName = saveData.Player.Name,
                        Level = saveData.Player.Level,
                        SaveTime = saveData.SaveTime,
                        PlayTime = saveData.TotalPlayTime
                    };

                    result.Add(info);

                    Console.WriteLine($"槽位 {slot}: {info.SaveName}");
                    Console.WriteLine($"  玩家: {info.PlayerName}, 等级 {info.Level}");
                    Console.WriteLine($"  保存时间: {info.SaveTime}");
                    Console.WriteLine($"  游戏时间: {info.PlayTime:F1} 小时");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"列出存档失败: {ex.Message}\n");
            }

            return result;
        }

        /// <summary>
        /// 获取当前存档
        /// </summary>
        public GameSaveData? GetCurrentSave() => _currentSave;

        /// <summary>
        /// 获取当前槽位
        /// </summary>
        public int GetCurrentSlot() => _currentSlot;
    }

    /// <summary>
    /// 存档槽位信息
    /// </summary>
    public class SaveSlotInfo
    {
        public int Slot { get; set; }
        public string SaveName { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int Level { get; set; }
        public DateTime SaveTime { get; set; }
        public float PlayTime { get; set; }
    }
}
```

**代码说明**：

- `CreateNewSave` 创建新的存档数据
- `SaveGameAsync` 保存当前存档到指定槽位
- `LoadGameAsync` 从槽位加载存档
- `DeleteSaveAsync` 删除指定槽位的存档
- `ListSavesAsync` 列出所有存档信息
- 使用 try-catch 处理异常

## 步骤 3：实现自动保存功能

创建自动保存系统，定期保存游戏进度。

```csharp
using GFramework.Core.System;
using GFramework.Core.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyGame.Systems
{
    /// <summary>
    /// 自动保存系统
    /// </summary>
    public class AutoSaveSystem : AbstractSystem
    {
        private CancellationTokenSource? _autoSaveCts;
        private bool _isAutoSaveEnabled;
        private TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 启动自动保存
        /// </summary>
        public void StartAutoSave(int slot, TimeSpan? interval = null)
        {
            if (_isAutoSaveEnabled)
            {
                Console.WriteLine("自动保存已在运行");
                return;
            }

            if (interval.HasValue)
            {
                _autoSaveInterval = interval.Value;
            }

            _autoSaveCts = new CancellationTokenSource();
            _isAutoSaveEnabled = true;

            Console.WriteLine($"\n启动自动保存 (间隔: {_autoSaveInterval.TotalSeconds} 秒)");

            // 在后台线程运行自动保存
            Task.Run(async () =>
            {
                while (!_autoSaveCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // 等待指定间隔
                        await Task.Delay(_autoSaveInterval, _autoSaveCts.Token);

                        // 执行自动保存
                        await PerformAutoSaveAsync(slot);
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"自动保存错误: {ex.Message}");
                    }
                }
            }, _autoSaveCts.Token);
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void StopAutoSave()
        {
            if (!_isAutoSaveEnabled)
            {
                return;
            }

            Console.WriteLine("停止自动保存");

            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
            _isAutoSaveEnabled = false;
        }

        /// <summary>
        /// 执行自动保存
        /// </summary>
        private async Task PerformAutoSaveAsync(int slot)
        {
            Console.WriteLine($"\n[自动保存] 保存到槽位 {slot}...");

            var saveSystem = this.GetSystem<SaveSystem>();
            var success = await saveSystem.SaveGameAsync(slot);

            if (success)
            {
                Console.WriteLine("[自动保存] 完成");
            }
            else
            {
                Console.WriteLine("[自动保存] 失败");
            }
        }

        /// <summary>
        /// 检查自动保存是否启用
        /// </summary>
        public bool IsAutoSaveEnabled() => _isAutoSaveEnabled;

        /// <summary>
        /// 设置自动保存间隔
        /// </summary>
        public void SetAutoSaveInterval(TimeSpan interval)
        {
            _autoSaveInterval = interval;
            Console.WriteLine($"自动保存间隔已设置为 {interval.TotalSeconds} 秒");
        }
    }
}
```

**代码说明**：

- 使用 `CancellationTokenSource` 控制后台任务
- `StartAutoSave` 启动定时保存任务
- `StopAutoSave` 停止自动保存
- 使用 `Task.Delay` 实现定时触发
- 捕获并处理异常，避免自动保存失败影响游戏

## 步骤 4：实现游戏数据更新

创建一个系统来模拟游戏数据的变化。

```csharp
using GFramework.Core.System;
using GFramework.Core.Extensions;
using MyGame.Data;
using System;

namespace MyGame.Systems
{
    /// <summary>
    /// 游戏逻辑系统（模拟）
    /// </summary>
    public class GameLogicSystem : AbstractSystem
    {
        private Random _random = new();

        /// <summary>
        /// 模拟玩家升级
        /// </summary>
        public void LevelUp()
        {
            var saveSystem = this.GetSystem<SaveSystem>();
            var save = saveSystem.GetCurrentSave();

            if (save == null)
            {
                Console.WriteLine("没有当前存档");
                return;
            }

            save.Player.Level++;
            save.Player.Experience = 0;
            save.Player.MaxHealth += 10;
            save.Player.Health = save.Player.MaxHealth;

            Console.WriteLine($"\n玩家升级! 当前等级: {save.Player.Level}");
        }

        /// <summary>
        /// 模拟获得金币
        /// </summary>
        public void AddGold(int amount)
        {
            var saveSystem = this.GetSystem<SaveSystem>();
            var save = saveSystem.GetCurrentSave();

            if (save == null) return;

            save.Player.Gold += amount;
            Console.WriteLine($"\n获得金币 +{amount}, 当前: {save.Player.Gold}");
        }

        /// <summary>
        /// 模拟完成关卡
        /// </summary>
        public void CompleteLevel(int level)
        {
            var saveSystem = this.GetSystem<SaveSystem>();
            var save = saveSystem.GetCurrentSave();

            if (save == null) return;

            if (!save.Progress.CompletedLevels.Contains(level))
            {
                save.Progress.CompletedLevels.Add(level);
                Console.WriteLine($"\n完成关卡 {level}!");
            }

            save.Progress.CurrentLevel = level + 1;
        }

        /// <summary>
        /// 模拟获得物品
        /// </summary>
        public void AddItem(string itemId, string itemName, int quantity = 1)
        {
            var saveSystem = this.GetSystem<SaveSystem>();
            var save = saveSystem.GetCurrentSave();

            if (save == null) return;

            // 查找已有物品
            var existingItem = save.Inventory.Items.Find(i => i.Id == itemId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                save.Inventory.Items.Add(new ItemData
                {
                    Id = itemId,
                    Name = itemName,
                    Quantity = quantity
                });
            }

            Console.WriteLine($"\n获得物品: {itemName} x{quantity}");
        }

        /// <summary>
        /// 模拟游戏进行
        /// </summary>
        public void SimulateGameplay()
        {
            Console.WriteLine("\n=== 模拟游戏进行 ===");

            // 随机事件
            int eventType = _random.Next(0, 4);

            switch (eventType)
            {
                case 0:
                    AddGold(_random.Next(10, 100));
                    break;
                case 1:
                    LevelUp();
                    break;
                case 2:
                    CompleteLevel(_random.Next(1, 10));
                    break;
                case 3:
                    AddItem($"item_{_random.Next(1, 5)}", $"物品 {_random.Next(1, 5)}", 1);
                    break;
            }

            // 增加游戏时间
            var saveSystem = this.GetSystem<SaveSystem>();
            var save = saveSystem.GetCurrentSave();
            if (save != null)
            {
                save.TotalPlayTime += 0.1f;
                save.Progress.PlayTime += 0.1f;
            }
        }
    }
}
```

**代码说明**：

- 提供各种游戏事件的模拟方法
- 直接修改当前存档数据
- 用于测试存档系统功能

## 步骤 5：注册系统并测试

在架构中注册所有系统并进行测试。

```csharp
using GFramework.Core.Architecture;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Data;
using GFramework.Game.Storage;
using MyGame.Data;
using MyGame.Systems;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void Init()
        {
            Interface = this;

            // 注册存储系统
            var storage = new FileStorage("./game_data");
            RegisterUtility<IFileStorage>(storage);

            // 注册存档仓库
            var saveConfig = new SaveConfiguration
            {
                SaveRoot = "saves",
                SaveSlotPrefix = "slot_",
                SaveFileName = "save.json"
            };

            var saveRepo = new SaveRepository<GameSaveData>(storage, saveConfig);
            RegisterUtility<ISaveRepository<GameSaveData>>(saveRepo);

            // 注册系统
            RegisterSystem(new SaveSystem());
            RegisterSystem(new AutoSaveSystem());
            RegisterSystem(new GameLogicSystem());

            Console.WriteLine("游戏架构初始化完成");
        }
    }
}
```

### 测试代码

```csharp
using MyGame;
using MyGame.Systems;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 存档系统测试 ===\n");

        // 1. 初始化架构
        var architecture = new GameArchitecture();
        architecture.Initialize();
        await architecture.WaitUntilReadyAsync();

        // 2. 获取系统
        var saveSystem = architecture.GetSystem<SaveSystem>();
        var autoSaveSystem = architecture.GetSystem<AutoSaveSystem>();
        var gameLogic = architecture.GetSystem<GameLogicSystem>();

        // 3. 创建新存档
        Console.WriteLine("--- 测试 1: 创建新存档 ---");
        saveSystem.CreateNewSave("我的冒险");
        await saveSystem.SaveGameAsync(1);

        await Task.Delay(1000);

        // 4. 模拟游戏进行
        Console.WriteLine("--- 测试 2: 游戏进行 ---");
        for (int i = 0; i < 5; i++)
        {
            gameLogic.SimulateGameplay();
            await Task.Delay(500);
        }

        // 5. 手动保存
        Console.WriteLine("\n--- 测试 3: 手动保存 ---");
        await saveSystem.SaveGameAsync(1);

        await Task.Delay(1000);

        // 6. 创建第二个存档
        Console.WriteLine("--- 测试 4: 创建第二个存档 ---");
        saveSystem.CreateNewSave("新的旅程");
        gameLogic.AddGold(500);
        gameLogic.LevelUp();
        await saveSystem.SaveGameAsync(2);

        await Task.Delay(1000);

        // 7. 列出所有存档
        Console.WriteLine("--- 测试 5: 列出所有存档 ---");
        await saveSystem.ListSavesAsync();

        await Task.Delay(1000);

        // 8. 加载第一个存档
        Console.WriteLine("--- 测试 6: 加载存档 ---");
        await saveSystem.LoadGameAsync(1);

        await Task.Delay(1000);

        // 9. 启动自动保存
        Console.WriteLine("--- 测试 7: 启动自动保存 ---");
        autoSaveSystem.StartAutoSave(1, TimeSpan.FromSeconds(3));

        // 模拟游戏进行
        for (int i = 0; i < 10; i++)
        {
            gameLogic.SimulateGameplay();
            await Task.Delay(1000);
        }

        // 10. 停止自动保存
        autoSaveSystem.StopAutoSave();

        await Task.Delay(1000);

        // 11. 删除存档
        Console.WriteLine("--- 测试 8: 删除存档 ---");
        await saveSystem.DeleteSaveAsync(2);

        await Task.Delay(1000);

        // 12. 最终存档列表
        Console.WriteLine("--- 测试 9: 最终存档列表 ---");
        await saveSystem.ListSavesAsync();

        Console.WriteLine("=== 测试完成 ===");
    }
}
```

**代码说明**：

- 注册存储系统和存档仓库
- 注册所有游戏系统
- 测试存档的创建、保存、加载、删除
- 测试自动保存功能
- 测试多槽位管理

## 完整代码

所有代码文件已在上述步骤中提供。项目结构如下：

```
MyGame/
├── Data/
│   ├── PlayerData.cs
│   ├── ProgressData.cs
│   ├── InventoryData.cs
│   └── GameSaveData.cs
├── Systems/
│   ├── SaveSystem.cs
│   ├── AutoSaveSystem.cs
│   └── GameLogicSystem.cs
├── GameArchitecture.cs
└── Program.cs
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```
=== 存档系统测试 ===

游戏架构初始化完成

--- 测试 1: 创建新存档 ---
创建新存档: 我的冒险

=== 保存游戏到槽位 1 ===
存档已保存: 我的冒险
玩家: Player, 等级 1
游戏时间: 0.0 小时
保存时间: 2026-03-07 10:30:00

--- 测试 2: 游戏进行 ---

=== 模拟游戏进行 ===

获得金币 +45, 当前: 45

=== 模拟游戏进行 ===

玩家升级! 当前等级: 2

=== 模拟游戏进行 ===

完成关卡 3!

=== 模拟游戏进行 ===

获得物品: 物品 2 x1

=== 模拟游戏进行 ===

获得金币 +78, 当前: 123

--- 测试 3: 手动保存 ---

=== 保存游戏到槽位 1 ===
存档已保存: 我的冒险
玩家: Player, 等级 2
游戏时间: 0.5 小时
保存时间: 2026-03-07 10:30:05

--- 测试 4: 创建第二个存档 ---
创建新存档: 新的旅程

获得金币 +500, 当前: 500

玩家升级! 当前等级: 2

=== 保存游戏到槽位 2 ===
存档已保存: 新的旅程
玩家: Player, 等级 2
游戏时间: 0.0 小时
保存时间: 2026-03-07 10:30:06

--- 测试 5: 列出所有存档 ---

=== 存档列表 (2 个) ===
槽位 1: 我的冒险
  玩家: Player, 等级 2
  保存时间: 2026-03-07 10:30:05
  游戏时间: 0.5 小时
槽位 2: 新的旅程
  玩家: Player, 等级 2
  保存时间: 2026-03-07 10:30:06
  游戏时间: 0.0 小时

--- 测试 6: 加载存档 ---

=== 从槽位 1 加载游戏 ===
存档已加载: 我的冒险
玩家: Player, 等级 2
当前关卡: 4
金币: 123
物品数量: 1
保存时间: 2026-03-07 10:30:05

--- 测试 7: 启动自动保存 ---

启动自动保存 (间隔: 3 秒)

=== 模拟游戏进行 ===
...

[自动保存] 保存到槽位 1...

=== 保存游戏到槽位 1 ===
存档已保存: 我的冒险
...
[自动保存] 完成

停止自动保存

--- 测试 8: 删除存档 ---

删除槽位 2 的存档
槽位 2 的存档已删除

--- 测试 9: 最终存档列表 ---

=== 存档列表 (1 个) ===
槽位 1: 我的冒险
  玩家: Player, 等级 3
  保存时间: 2026-03-07 10:30:15
  游戏时间: 1.5 小时

=== 测试完成 ===
```

**验证步骤**：

1. 存档创建和保存成功
2. 游戏数据正确更新
3. 多槽位管理正常
4. 存档加载恢复数据
5. 自动保存定时触发
6. 存档删除功能正常
7. 存档列表显示正确

## 下一步

恭喜！你已经实现了一个完整的存档系统。接下来可以学习：

- [Godot 完整项目搭建](/zh-CN/tutorials/godot-complete-project.md) - 在 Godot 中使用存档系统
- [使用协程系统](/zh-CN/tutorials/coroutine-tutorial.md) - 异步加载存档
- [数据与存档系统](/zh-CN/game/data.md) - 数据系统详细说明

## 相关文档

- [数据与存档系统](/zh-CN/game/data.md) - 数据系统详细说明
- [对象池系统](/zh-CN/core/pool.md) - 结合对象池复用资源
- [协程系统](/zh-CN/core/coroutine.md) - 异步加载资源
- [System 层](/zh-CN/core/system.md) - System 详细说明
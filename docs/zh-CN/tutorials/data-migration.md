---
title: 实现数据版本迁移
description: 学习如何实现数据版本迁移系统，处理不同版本间的数据升级
---

# 实现数据版本迁移

## 学习目标

完成本教程后，你将能够：

- 理解数据版本迁移的重要性和应用场景
- 定义版本化数据结构
- 实现数据迁移接口
- 注册和管理迁移策略
- 处理多版本连续升级
- 测试迁移流程的正确性

## 前置条件

- 已安装 GFramework.Game NuGet 包
- 了解 C# 基础语法和接口实现
- 阅读过[快速开始](/zh-CN/getting-started/quick-start)
- 了解[数据与存档系统](/zh-CN/game/data)
- 建议先完成[实现存档系统](/zh-CN/tutorials/save-system)教程

## 为什么需要数据迁移

在游戏开发过程中，数据结构经常会发生变化：

- **新增功能**：添加新的游戏系统需要新的数据字段
- **重构优化**：改进数据结构以提升性能或可维护性
- **修复问题**：修正早期设计的缺陷
- **平衡调整**：调整游戏数值和配置

数据迁移系统能够：

- 自动将旧版本数据升级到新版本
- 保证玩家存档的兼容性
- 避免数据丢失和游戏崩溃
- 提供平滑的版本过渡体验

## 步骤 1：定义版本化数据结构

首先，让我们定义一个支持版本控制的游戏数据结构。

```csharp
using GFramework.Game.Abstractions.data;
using System;
using System.Collections.Generic;

namespace MyGame.Data
{
    /// <summary>
    /// 玩家数据 - 版本 1（初始版本）
    /// </summary>
    public class PlayerSaveData : IVersionedData
    {
        // IVersionedData 接口要求的属性
        public int Version { get; set; } = 1;
        public DateTime LastModified { get; set; } = DateTime.Now;

        // 基础数据
        public string PlayerName { get; set; } = "Player";
        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        // 版本 1 的简单位置数据
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }
}
```

**代码说明**：

- 实现 `IVersionedData` 接口以支持版本管理
- `Version` 属性标识当前数据版本（从 1 开始）
- `LastModified` 记录最后修改时间
- 初始版本使用简单的 X、Y 坐标表示位置

## 步骤 2：定义新版本数据结构

随着游戏开发，我们需要添加新功能，数据结构也需要升级。

```csharp
namespace MyGame.Data
{
    /// <summary>
    /// 玩家数据 - 版本 2（添加 Z 轴和经验值）
    /// </summary>
    public class PlayerSaveDataV2 : IVersionedData
    {
        public int Version { get; set; } = 2;
        public DateTime LastModified { get; set; } = DateTime.Now;

        // 基础数据
        public string PlayerName { get; set; } = "Player";
        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        // 版本 2：添加 Z 轴支持 3D 游戏
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }  // 新增

        // 版本 2：添加经验值系统
        public int Experience { get; set; }  // 新增
        public int ExperienceToNextLevel { get; set; } = 100;  // 新增
    }

    /// <summary>
    /// 玩家数据 - 版本 3（重构为结构化数据）
    /// </summary>
    public class PlayerSaveDataV3 : IVersionedData
    {
        public int Version { get; set; } = 3;
        public DateTime LastModified { get; set; } = DateTime.Now;

        // 基础数据
        public string PlayerName { get; set; } = "Player";
        public int Level { get; set; } = 1;
        public int Gold { get; set; } = 0;

        // 版本 3：使用结构化的位置数据
        public Vector3Data Position { get; set; } = new();

        // 版本 3：使用结构化的经验值数据
        public ExperienceData Experience { get; set; } = new();

        // 版本 3：新增技能系统
        public List<string> UnlockedSkills { get; set; } = new();
    }

    /// <summary>
    /// 3D 位置数据
    /// </summary>
    public class Vector3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    /// <summary>
    /// 经验值数据
    /// </summary>
    public class ExperienceData
    {
        public int Current { get; set; }
        public int ToNextLevel { get; set; } = 100;
    }
}
```

**代码说明**：

- **版本 2**：添加 Z 轴坐标和经验值系统
- **版本 3**：重构为更清晰的结构化数据
- 每个版本的 `Version` 属性递增
- 保持向后兼容，新字段提供默认值

## 步骤 3：实现数据迁移器

创建迁移器来处理版本间的数据转换。

```csharp
using GFramework.Game.Abstractions.setting;
using System;

namespace MyGame.Data.Migrations
{
    /// <summary>
    /// 从版本 1 迁移到版本 2
    /// </summary>
    public class PlayerDataMigration_V1_to_V2 : ISettingsMigration
    {
        public Type SettingsType => typeof(PlayerSaveData);
        public int FromVersion => 1;
        public int ToVersion => 2;

        public ISettingsSection Migrate(ISettingsSection oldData)
        {
            if (oldData is not PlayerSaveData v1)
            {
                throw new ArgumentException($"Expected PlayerSaveData, got {oldData.GetType().Name}");
            }

            Console.WriteLine($"[迁移] 版本 1 -> 2: {v1.PlayerName}");

            // 创建版本 2 数据
            var v2 = new PlayerSaveDataV2
            {
                Version = 2,
                LastModified = DateTime.Now,

                // 复制现有数据
                PlayerName = v1.PlayerName,
                Level = v1.Level,
                Gold = v1.Gold,
                PositionX = v1.PositionX,
                PositionY = v1.PositionY,

                // 新字段：Z 轴默认为 0
                PositionZ = 0f,

                // 新字段：根据等级计算经验值
                Experience = 0,
                ExperienceToNextLevel = 100 * v1.Level
            };

            Console.WriteLine($"  - 添加 Z 轴坐标: {v2.PositionZ}");
            Console.WriteLine($"  - 初始化经验值系统: {v2.Experience}/{v2.ExperienceToNextLevel}");

            return v2;
        }
    }

    /// <summary>
    /// 从版本 2 迁移到版本 3
    /// </summary>
    public class PlayerDataMigration_V2_to_V3 : ISettingsMigration
    {
        public Type SettingsType => typeof(PlayerSaveDataV2);
        public int FromVersion => 2;
        public int ToVersion => 3;

        public ISettingsSection Migrate(ISettingsSection oldData)
        {
            if (oldData is not PlayerSaveDataV2 v2)
            {
                throw new ArgumentException($"Expected PlayerSaveDataV2, got {oldData.GetType().Name}");
            }

            Console.WriteLine($"[迁移] 版本 2 -> 3: {v2.PlayerName}");

            // 创建版本 3 数据
            var v3 = new PlayerSaveDataV3
            {
                Version = 3,
                LastModified = DateTime.Now,

                // 复制基础数据
                PlayerName = v2.PlayerName,
                Level = v2.Level,
                Gold = v2.Gold,

                // 迁移位置数据到结构化格式
                Position = new Vector3Data
                {
                    X = v2.PositionX,
                    Y = v2.PositionY,
                    Z = v2.PositionZ
                },

                // 迁移经验值数据到结构化格式
                Experience = new ExperienceData
                {
                    Current = v2.Experience,
                    ToNextLevel = v2.ExperienceToNextLevel
                },

                // 新字段：根据等级解锁基础技能
                UnlockedSkills = GenerateDefaultSkills(v2.Level)
            };

            Console.WriteLine($"  - 重构位置数据: ({v3.Position.X}, {v3.Position.Y}, {v3.Position.Z})");
            Console.WriteLine($"  - 重构经验值数据: {v3.Experience.Current}/{v3.Experience.ToNextLevel}");
            Console.WriteLine($"  - 初始化技能系统: {v3.UnlockedSkills.Count} 个技能");

            return v3;
        }

        /// <summary>
        /// 根据等级生成默认技能
        /// </summary>
        private List<string> GenerateDefaultSkills(int level)
        {
            var skills = new List<string> { "basic_attack" };

            if (level >= 5)
                skills.Add("power_strike");

            if (level >= 10)
                skills.Add("shield_block");

            if (level >= 15)
                skills.Add("ultimate_skill");

            return skills;
        }
    }
}
```

**代码说明**：

- 实现 `ISettingsMigration` 接口
- `SettingsType` 指定要迁移的数据类型
- `FromVersion` 和 `ToVersion` 定义迁移的版本范围
- `Migrate` 方法执行实际的数据转换
- 为新字段提供合理的默认值或计算值
- 添加日志输出便于调试

## 步骤 4：注册迁移策略

创建迁移管理器来注册和执行迁移。

```csharp
using GFramework.Game.Abstractions.setting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGame.Data.Migrations
{
    /// <summary>
    /// 数据迁移管理器
    /// </summary>
    public class DataMigrationManager
    {
        private readonly Dictionary<(Type type, int from), ISettingsMigration> _migrations = new();

        /// <summary>
        /// 注册迁移器
        /// </summary>
        public void RegisterMigration(ISettingsMigration migration)
        {
            var key = (migration.SettingsType, migration.FromVersion);

            if (_migrations.ContainsKey(key))
            {
                Console.WriteLine($"警告: 迁移器已存在 {migration.SettingsType.Name} " +
                                $"v{migration.FromVersion}->v{migration.ToVersion}");
                return;
            }

            _migrations[key] = migration;
            Console.WriteLine($"注册迁移器: {migration.SettingsType.Name} " +
                            $"v{migration.FromVersion} -> v{migration.ToVersion}");
        }

        /// <summary>
        /// 执行迁移（支持跨多个版本）
        /// </summary>
        public ISettingsSection MigrateToLatest(ISettingsSection data, int targetVersion)
        {
            if (data is not IVersionedData versioned)
            {
                Console.WriteLine("数据不支持版本控制，跳过迁移");
                return data;
            }

            var currentVersion = versioned.Version;

            if (currentVersion == targetVersion)
            {
                Console.WriteLine($"数据已是最新版本 v{targetVersion}");
                return data;
            }

            if (currentVersion > targetVersion)
            {
                Console.WriteLine($"警告: 数据版本 v{currentVersion} 高于目标版本 v{targetVersion}");
                return data;
            }

            Console.WriteLine($"\n开始迁移: v{currentVersion} -> v{targetVersion}");

            var current = data;
            var currentVer = currentVersion;

            // 逐步迁移到目标版本
            while (currentVer < targetVersion)
            {
                var key = (current.GetType(), currentVer);

                if (!_migrations.TryGetValue(key, out var migration))
                {
                    Console.WriteLine($"错误: 找不到迁移器 {current.GetType().Name} v{currentVer}");
                    break;
                }

                current = migration.Migrate(current);
                currentVer = migration.ToVersion;
                      Console.WriteLine($"迁移完成: v{currentVersion} -> v{currentVer}\n");
            return current;
        }

        /// <summary>
        /// 获取迁移路径
        /// </summary>
        public List<string> GetMigrationPath(Type dataType, int fromVersion, int toVersion)
        {
            var path = new List<string>();
            var currentVer = fromVersion;
            var currentType = dataType;

            while (currentVer < toVersion)
            {
                var key = (currentType, currentVer);

                if (!_migrations.TryGetValue(key, out var migration))
                {
                    path.Add($"v{currentVer} -> ? (缺失迁移器)");
                    break;
                }

                path.Add($"v{migration.FromVersion} -> v{migration.ToVersion}");
                currentVer = migration.ToVersion;
                currentType = migration.SettingsType;
            }

            return path;
        }
    }
}
```

**代码说明**：

- 使用字典存储迁移器，键为 (类型, 源版本)
- `RegisterMigration` 注册单个迁移器
- `MigrateToLatest` 自动执行多步迁移
- `GetMigrationPath` 显示迁移路径，便于调试
- 支持跨多个版本的连续迁移

## 步骤 5：测试迁移流程

创建完整的测试程序验证迁移功能。

```csharp
using MyGame.Data;
using MyGame.Data.Migrations;
using System;

namespace MyGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 数据迁移系统测试 ===\n");

            // 1. 创建迁移管理器
            var migrationManager = new DataMigrationManager();

            // 2. 注册所有迁移器
            Console.WriteLine("--- 注册迁移器 ---");
            migrationManager.RegisterMigration(new PlayerDataMigration_V1_to_V2());
            migrationManager.RegisterMigration(new PlayerDataMigration_V2_to_V3());
            Console.WriteLine();

            // 3. 测试场景 1：从版本 1 迁移到版本 3
            Console.WriteLine("--- 测试 1: V1 -> V3 迁移 ---");
            var v1Data = new PlayerSaveData
            {
                Version = 1,
                PlayerName = "老玩家",
                Level = 12,
                Gold = 5000,
                PositionX = 100.5f,
                PositionY = 200.3f
            };

            Console.WriteLine("原始数据 (V1):");
            Console.WriteLine($"  玩家: {v1Data.PlayerName}");
            Console.WriteLine($"  等级: {v1Data.Level}");
            Console.WriteLine($"  金币: {v1Data.Gold}");
            Console.WriteLine($"  位置: ({v1Data.PositionX}, {v1Data.PositionY})");
            Console.WriteLine();

            // 显示迁移路径
            var path = migrationManager.GetMigrationPath(typeof(PlayerSaveData), 1, 3);
            Console.WriteLine("迁移路径:");
            foreach (var step in path)
            {
                Console.WriteLine($"  {step}");
            }
            Console.WriteLine();

            // 执行迁移
            var v3Data = (PlayerSaveDataV3)migrationManager.MigrateToLatest(v1Data, 3);

            Console.WriteLine("迁移后数据 (V3):");
            Console.WriteLine($"  玩家: {v3Data.PlayerName}");
            Console.WriteLine($"  等级: {v3Data.Level}");
            Console.WriteLine($"  金币: {v3Data.Gold}");
            Console.WriteLine($"  位置: ({v3Data.Position.X}, {v3Data.Position.Y}, {v3Data.Position.Z})");
            Console.WriteLine($"  经验值: {v3Data.Experience.Current}/{v3Data.Experience.ToNextLevel}");
            Console.WriteLine($"  技能: {string.Join(", ", v3Data.UnlockedSkills)}");
            Console.WriteLine();

            // 4. 测试场景 2：从版本 2 迁移到版本 3
            Console.WriteLine("--- 测试 2: V2 -> V3 迁移 ---");
            var v2Data = new PlayerSaveDataV2
            {
                Version = 2,
                PlayerName = "中期玩家",
                Level = 8,
                Gold = 2000,
                PositionX = 50.0f,
                PositionY = 75.0f,
                PositionZ = 10.0f,
                Experience = 350,
                ExperienceToNextLevel = 800
            };

            Console.WriteLine("原始数据 (V2):");
            Console.WriteLine($"  玩家: {v2Data.PlayerName}");
            Console.WriteLine($"  等级: {v2Data.Level}");
            Console.WriteLine($"  位置: ({v2Data.PositionX}, {v2Data.PositionY}, {v2Data.PositionZ})");
            Console.WriteLine($"  经验值: {v2Data.Experience}/{v2Data.ExperienceToNextLevel}");
            Console.WriteLine();

            var v3Data2 = (PlayerSaveDataV3)migrationManager.MigrateToLatest(v2Data, 3);

            Console.WriteLine("迁移后数据 (V3):");
            Console.WriteLine($"  玩家: {v3Data2.PlayerName}");
            Console.WriteLine($"  等级: {v3Data2.Level}");
            Console.WriteLine($"  位置: ({v3Data2.Position.X}, {v3Data2.Position.Y}, {v3Data2.Position.Z})");
            Console.WriteLine($"  经验值: {v3Data2.Experience.Current}/{v3Data2.Experience.ToNextLevel}");
            Console.WriteLine($"  技能: {string.Join(", ", v3Data2.UnlockedSkills)}");
            Console.WriteLine();

            // 5. 测试场景 3：已是最新版本
            Console.WriteLine("--- 测试 3: 已是最新版本 ---");
            var v3DataLatest = new PlayerSaveDataV3
            {
                Version = 3,
                PlayerName = "新玩家",
                Level = 1
            };

            migrationManager.MigrateToLatest(v3DataLatest, 3);
            Console.WriteLine();

            Console.WriteLine("=== 测试完成 ===");
        }
    }
}
```

**代码说明**：

- 创建不同版本的测试数据
- 测试单步迁移（V2 -> V3）
- 测试多步迁移（V1 -> V3）
- 测试已是最新版本的情况
- 显示迁移前后的数据对比

## 完整代码

所有代码文件已在上述步骤中提供。项目结构如下：

```
MyGame/
├── Data/
│   ├── PlayerSaveData.cs          # 版本 1 数据结构
│   ├── PlayerSaveDataV2.cs        # 版本 2 数据结构
│   ├── PlayerSaveDataV3.cs        # 版本 3 数据结构
│   └── Migrations/
│       ├── PlayerDataMigration_V1_to_V2.cs
│       ├── PlayerDataMigration_V2_to_V3.cs
│       └── DataMigrationManager.cs
└── Program.cs
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```
=== 数据迁移系统测试 ===

--- 注册迁移器 ---
注册迁移器: PlayerSaveData v1 -> v2
注册迁移器: PlayerSaveDataV2 v2 -> v3

--- 测试 1: V1 -> V3 迁移 ---
原始数据 (V1):
  玩家: 老玩家
  等级: 12
  金币: 5000
  位置: (100.5, 200.3)

迁移路径:
  v1 -> v2
  v2 -> v3

开始迁移: v1 -> v3
[迁移] 版本 1 -> 2: 老玩家
  - 添加 Z 轴坐标: 0
  - 初始化经验值系统: 0/1200
[迁移] 版本 2 -> 3: 老玩家
  - 重构位置数据: (100.5, 200.3, 0)
  - 重构经验值数据: 0/1200
  - 初始化技能系统: 3 个技能
迁移完成: v1 -> v3

迁移后数据 (V3):
  玩家: 老玩家
  等级: 12
  金币: 5000
  位置: (100.5, 200.3, 0)
  经验值: 0/1200
  技能: basic_attack, power_strike, shield_block

--- 测试 2: V2 -> V3 迁移 ---
原始数据 (V2):
  玩家: 中期玩家
  等级: 8
  金币: 2000
  位置: (50, 75, 10)
  经验值: 350/800

开始迁移: v2 -> v3
[迁移] 版本 2 -> 3: 中期玩家
  - 重构位置数据: (50, 75, 10)
  - 重构经验值数据: 350/800
  - 初始化技能系统: 2 个技能
迁移完成: v2 -> v3

迁移后数据 (V3):
  玩家: 中期玩家
  等级: 8
  位置: (50, 75, 10)
  经验值: 350/800
  技能: basic_attack, power_strike

--- 测试 3: 已是最新版本 ---
数据已是最新版本 v3

=== 测试完成 ===
```

**验证步骤**：

1. 迁移器成功注册
2. V1 数据正确迁移到 V3
3. V2 数据正确迁移到 V3
4. 新字段获得合理的默认值
5. 已是最新版本的数据不会重复迁移
6. 迁移路径清晰可追踪

## 下一步

恭喜！你已经实现了一个完整的数据版本迁移系统。接下来可以学习：

- [实现存档系统](/zh-CN/tutorials/save-system) - 结合存档系统使用迁移
- [Godot 完整项目搭建](/zh-CN/tutorials/godot-complete-project) - 在实际项目中应用
- [数据与存档系统](/zh-CN/game/data) - 深入了解数据系统

## 最佳实践

### 1. 版本号管理

```csharp
// 使用常量管理版本号
public static class DataVersions
{
    public const int PlayerData_V1 = 1;
    public const int PlayerData_V2 = 2;
    public const int PlayerData_V3 = 3;
    public const int PlayerData_Latest = PlayerData_V3;
}
```

### 2. 迁移测试

```csharp
// 为每个迁移器编写单元测试
[Test]
public void TestMigration_V1_to_V2()
{
    var v1 = new PlayerSaveData { Level = 10 };
    var migration = new PlayerDataMigration_V1_to_V2();
    var v2 = (PlayerSaveDataV2)migration.Migrate(v1);

    Assert.AreEqual(10, v2.Level);
    Assert.AreEqual(0, v2.PositionZ);
    Assert.AreEqual(1000, v2.ExperienceToNextLevel);
}
```

### 3. 数据备份

```csharp
// 迁移前自动备份
public ISettingsSection MigrateWithBackup(ISettingsSection data)
{
    // 备份原始数据
    var backup = SerializeData(data);
    SaveBackup(backup);

    try
    {
        // 执行迁移
        var migrated = MigrateToLatest(data, targetVersion);
        return migrated;
    }
    catch (Exception ex)
    {
        // 迁移失败，恢复备份
        RestoreBackup(backup);
        throw;
    }
}
```

### 4. 迁移日志

```csharp
// 记录详细的迁移日志
public class MigrationLogger
{
    public void LogMigration(string playerName, int from, int to)
    {
        var log = $"[{DateTime.Now}] {playerName}: v{from} -> v{to}";
        File.AppendAllText("migration.log", log + "\n");
    }
}
```

### 5. 向后兼容

- 新版本保留所有旧字段
- 为新字段提供合理的默认值
- 避免删除或重命名字段
- 使用 `[Obsolete]` 标记废弃字段

### 6. 性能优化

```csharp
// 批量迁移优化
public async Task<List<ISettingsSection>> MigrateBatchAsync(
    List<ISettingsSection> dataList,
    int targetVersion)
{
    var tasks = dataList.Select(data =>
        Task.Run(() => MigrateToLatest(data, targetVersion)));

    return (await Task.WhenAll(tasks)).ToList();
}
```

## 常见问题

### 1. 如何处理跨多个版本的迁移？

迁移管理器会自动按顺序应用所有必要的迁移。例如从 V1 到 V3，会先执行 V1->V2，再执行 V2->V3。

### 2. 迁移失败如何处理？

建议在迁移前备份原始数据，迁移失败时可以恢复。同时在迁移过程中添加详细的日志记录。

### 3. 如何处理不兼容的数据变更？

对于破坏性变更，建议：

- 提供数据转换工具
- 在迁移中添加数据验证
- 通知用户可能的数据丢失
- 提供回滚机制

### 4. 是否需要保留所有历史版本的数据结构？

建议保留，这样可以：

- 支持从任意旧版本迁移
- 便于调试和测试
- 作为文档记录数据演变

### 5. 如何测试迁移功能？

- 创建各个版本的测试数据
- 验证迁移后的数据完整性
- 测试迁移链的正确性
- 使用真实的历史数据进行测试

## 相关文档

- [数据与存档系统](/zh-CN/game/data) - 数据系统详细说明
- [实现存档系统](/zh-CN/tutorials/save-system) - 存档系统教程
- [架构系统](/zh-CN/core/architecture) - 架构设计原则

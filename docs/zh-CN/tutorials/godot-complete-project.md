---
title: Godot 完整项目搭建
description: 从零开始使用 GFramework 构建一个完整的 Godot 游戏项目
---

# Godot 完整项目搭建

## 学习目标

完成本教程后，你将能够：

- 在 Godot 项目中集成 GFramework
- 创建完整的游戏架构
- 实现场景管理和 UI 系统
- 使用协程和事件系统
- 实现游戏存档功能
- 构建一个可运行的完整游戏

## 前置条件

- 已安装 Godot 4.x
- 已安装 .NET SDK 8.0+
- 了解 C# 和 Godot 基础
- 阅读过前面的教程：
    - [使用协程系统](/zh-CN/tutorials/coroutine-tutorial)
    - [实现状态机](/zh-CN/tutorials/state-machine-tutorial)
    - [实现存档系统](/zh-CN/tutorials/save-system)

## 项目概述

我们将创建一个简单的 2D 射击游戏，包含以下功能：

- 主菜单和游戏场景
- 玩家控制和射击
- 敌人生成和 AI
- 分数和生命值系统
- 游戏存档和加载
- 暂停菜单

## 步骤 1：创建 Godot 项目并配置

首先创建 Godot 项目并添加 GFramework 依赖。

### 1.1 创建项目

1. 打开 Godot，创建新项目 "MyShooterGame"
2. 选择 C# 作为脚本语言
3. 创建项目后，在项目根目录创建 `.csproj` 文件

### 1.2 添加 NuGet 包

编辑 `MyShooterGame.csproj`：

```xml
<Project Sdk="Godot.NET.Sdk/4.3.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- GFramework 包 -->
    <PackageReference Include="GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GFramework.Game" Version="1.0.0" />
    <PackageReference Include="GFramework.Godot" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### 1.3 创建项目结构

```
MyShooterGame/
├── Scripts/
│   ├── Architecture/
│   │   └── GameArchitecture.cs
│   ├── Models/
│   │   ├── PlayerModel.cs
│   │   └── GameModel.cs
│   ├── Systems/
│   │   ├── GameplaySystem.cs
│   │   └── SpawnSystem.cs
│   ├── Controllers/
│   │   └── PlayerController.cs
│   └── Data/
│       └── GameSaveData.cs
├── Scenes/
│   ├── Main.tscn
│   ├── Menu.tscn
│   ├── Game.tscn
│   ├── Player.tscn
│   └── Enemy.tscn
└── UI/
    ├── MainMenu.tscn
    ├── HUD.tscn
    └── PauseMenu.tscn
```

**代码说明**：

- 使用 Godot.NET.Sdk 4.3.0
- 添加 GFramework 的三个核心包
- 按功能组织代码结构

## 步骤 2：创建游戏架构

实现游戏的核心架构和数据模型。

### 2.1 定义数据模型

```csharp
// Scripts/Models/PlayerModel.cs
using GFramework.Core.model;
using GFramework.Core.Abstractions.property;

namespace MyShooterGame.Models
{
    public class PlayerModel : AbstractModel
    {
        public BindableProperty<int> Health { get; } = new(100);
        public BindableProperty<int> MaxHealth { get; } = new(100);
        public BindableProperty<int> Score { get; } = new(0);
        public BindableProperty<int> Lives { get; } = new(3);
        public BindableProperty<bool> IsAlive { get; } = new(true);

        protected override void OnInit()
        {
            // 监听生命值变化
            Health.RegisterOnValueChanged(health =>
            {
                if (health <= 0)
                {
                    IsAlive.Value = false;
                }
            });
        }

        public void Reset()
        {
            Health.Value = MaxHealth.Value;
            Score.Value = 0;
            Lives.Value = 3;
            IsAlive.Value = true;
        }

        public void TakeDamage(int damage)
        {
            Health.Value = Math.Max(0, Health.Value - damage);
        }

        public void AddScore(int points)
        {
            Score.Value += points;
        }

        public void LoseLife()
        {
            Lives.Value = Math.Max(0, Lives.Value - 1);
            if (Lives.Value > 0)
            {
                Health.Value = MaxHealth.Value;
                IsAlive.Value = true;
            }
        }
    }
}
```

```csharp
// Scripts/Models/GameModel.cs
using GFramework.Core.model;
using GFramework.Core.Abstractions.property;

namespace MyShooterGame.Models
{
    public class GameModel : AbstractModel
    {
        public BindableProperty<bool> IsPlaying { get; } = new(false);
        public BindableProperty<bool> IsPaused { get; } = new(false);
        public BindableProperty<int> CurrentWave { get; } = new(1);
        public BindableProperty<int> EnemiesAlive { get; } = new(0);
        public BindableProperty<float> GameTime { get; } = new(0f);

        protected override void OnInit()
        {
            // 初始化
        }

        public void StartGame()
        {
            IsPlaying.Value = true;
            IsPaused.Value = false;
            CurrentWave.Value = 1;
            EnemiesAlive.Value = 0;
            GameTime.Value = 0f;
        }

        public void PauseGame()
        {
            IsPaused.Value = true;
        }

        public void ResumeGame()
        {
            IsPaused.Value = false;
        }

        public void EndGame()
        {
            IsPlaying.Value = false;
            IsPaused.Value = false;
        }
    }
}
```

### 2.2 定义存档数据

```csharp
// Scripts/Data/GameSaveData.cs
using GFramework.Game.Abstractions.data;
using System;

namespace MyShooterGame.Data
{
    public class GameSaveData : IVersionedData
    {
        public int Version { get; set; } = 1;
        public DateTime SaveTime { get; set; }

        // 玩家数据
        public int HighScore { get; set; }
        public int TotalKills { get; set; }
        public float TotalPlayTime { get; set; }

        // 设置
        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 1.0f;
    }
}
```

### 2.3 创建游戏架构

```csharp
// Scripts/Architecture/GameArchitecture.cs
using GFramework.Godot.architecture;
using GFramework.Core.Abstractions.architecture;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.storage;
using GFramework.Game.data;
using GFramework.Game.storage;
using MyShooterGame.Models;
using MyShooterGame.Systems;
using MyShooterGame.Data;
using Godot;

namespace MyShooterGame.Architecture
{
    public class GameArchitecture : AbstractArchitecture
    {
        public static GameArchitecture Interface { get; private set; }

        public GameArchitecture()
        {
            Interface = this;
        }

        protected override void InstallModules()
        {
            GD.Print("=== 初始化游戏架构 ===");

            // 注册存储系统
            var storage = new FileStorage("user://saves");
            RegisterUtility<IFileStorage>(storage);

            // 注册存档仓库
            var saveConfig = new SaveConfiguration
            {
                SaveRoot = "",
                SaveSlotPrefix = "save_",
                SaveFileName = "data.json"
            };
            var saveRepo = new SaveRepository<GameSaveData>(storage, saveConfig);
            RegisterUtility<ISaveRepository<GameSaveData>>(saveRepo);

            // 注册 Model
            RegisterModel(new PlayerModel());
            RegisterModel(new GameModel());

            // 注册 System
            RegisterSystem(new GameplaySystem());
            RegisterSystem(new SpawnSystem());

            GD.Print("游戏架构初始化完成");
        }
    }
}
```

**代码说明**：

- `PlayerModel` 管理玩家状态
- `GameModel` 管理游戏状态
- `GameSaveData` 定义存档结构
- `GameArchitecture` 注册所有组件

## 步骤 3：实现游戏系统

创建游戏逻辑系统。

### 3.1 游戏逻辑系统

```csharp
// Scripts/Systems/GameplaySystem.cs
using GFramework.Core.system;
using GFramework.Core.extensions;
using MyShooterGame.Models;
using Godot;

namespace MyShooterGame.Systems
{
    public class GameplaySystem : AbstractSystem
    {
        public void StartNewGame()
        {
            GD.Print("开始新游戏");

            var playerModel = this.GetModel<PlayerModel>();
            var gameModel = this.GetModel<GameModel>();

            // 重置数据
            playerModel.Reset();
            gameModel.StartGame();
        }

        public void GameOver()
        {
            GD.Print("游戏结束");

            var gameModel = this.GetModel<GameModel>();
            gameModel.EndGame();

            // 保存最高分
            SaveHighScore();
        }

        public void PauseGame()
        {
            var gameModel = this.GetModel<GameModel>();
            gameModel.PauseGame();
            GetTree().Paused = true;
        }

        public void ResumeGame()
        {
            var gameModel = this.GetModel<GameModel>();
            gameModel.ResumeGame();
            GetTree().Paused = false;
        }

        private void SaveHighScore()
        {
            // 实现最高分保存逻辑
        }

        private SceneTree GetTree()
        {
            return (SceneTree)Engine.GetMainLoop();
        }
    }
}
```

### 3.2 敌人生成系统

```csharp
// Scripts/Systems/SpawnSystem.cs
using GFramework.Core.system;
using GFramework.Core.extensions;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using MyShooterGame.Models;
using Godot;
using System.Collections.Generic;

namespace MyShooterGame.Systems
{
    public class SpawnSystem : AbstractSystem
    {
        private PackedScene _enemyScene;
        private Node2D _spawnRoot;
        private CoroutineHandle? _spawnCoroutine;

        public void Initialize(Node2D spawnRoot, PackedScene enemyScene)
        {
            _spawnRoot = spawnRoot;
            _enemyScene = enemyScene;
        }

        public void StartSpawning()
        {
            if (_spawnCoroutine.HasValue)
            {
                this.StopCoroutine(_spawnCoroutine.Value);
            }

            _spawnCoroutine = this.StartCoroutine(SpawnEnemiesCoroutine());
        }

        public void StopSpawning()
        {
            if (_spawnCoroutine.HasValue)
            {
                this.StopCoroutine(_spawnCoroutine.Value);
                _spawnCoroutine = null;
            }
        }

        private IEnumerator<IYieldInstruction> SpawnEnemiesCoroutine()
        {
            var gameModel = this.GetModel<GameModel>();

            while (gameModel.IsPlaying.Value)
            {
                // 等待 2 秒
                yield return CoroutineHelper.WaitForSeconds(2.0);

                // 生成敌人
                if (!gameModel.IsPaused.Value)
                {
                    SpawnEnemy();
                }
            }
        }

        private void SpawnEnemy()
        {
            if (_enemyScene == null || _spawnRoot == null)
                return;

            var enemy = _enemyScene.Instantiate<Node2D>();
            _spawnRoot.AddChild(enemy);

            // 随机位置
            var random = new Random();
            enemy.Position = new Vector2(
                random.Next(100, 900),
                -50
            );

            var gameModel = this.GetModel<GameModel>();
            gameModel.EnemiesAlive.Value++;

            GD.Print($"生成敌人，当前数量: {gameModel.EnemiesAlive.Value}");
        }
    }
}
```

**代码说明**：

- `GameplaySystem` 管理游戏流程
- `SpawnSystem` 使用协程定时生成敌人
- 系统之间通过 Model 共享数据

## 步骤 4：创建玩家控制器

实现玩家的移动和射击。

```csharp
// Scripts/Controllers/PlayerController.cs
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using GFramework.SourceGenerators.Abstractions.rule;
using MyShooterGame.Architecture;
using MyShooterGame.Models;
using Godot;

namespace MyShooterGame.Controllers
{
    [ContextAware]
    public partial class PlayerController : CharacterBody2D, IController
    {
        [Export] public float Speed = 300f;
        [Export] public PackedScene BulletScene;

        private float _shootCooldown = 0f;
        private const float ShootInterval = 0.2f;

        public override void _Ready()
        {
            // 监听玩家死亡（使用扩展方法访问架构（[ContextAware] 实现 IContextAware 接口））
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.IsAlive.RegisterOnValueChanged(isAlive =>
            {
                if (!isAlive)
                {
                    OnPlayerDied();
                }
            });
        }

        public override void _Process(double delta)
        {
            _shootCooldown -= (float)delta;

            // 射击
            if (Input.IsActionPressed("shoot") && _shootCooldown <= 0)
            {
                Shoot();
                _shootCooldown = ShootInterval;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // 移动
            var velocity = Vector2.Zero;

            if (Input.IsActionPressed("move_left"))
                velocity.X -= 1;
            if (Input.IsActionPressed("move_right"))
                velocity.X += 1;
            if (Input.IsActionPressed("move_up"))
                velocity.Y -= 1;
            if (Input.IsActionPressed("move_down"))
                velocity.Y += 1;

            Velocity = velocity.Normalized() * Speed;
            MoveAndSlide();

            // 限制在屏幕内
            var screenSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, screenSize.X),
                Mathf.Clamp(Position.Y, 0, screenSize.Y)
            );
        }

        private void Shoot()
        {
            if (BulletScene == null)
                return;

            var bullet = BulletScene.Instantiate<Node2D>();
            GetParent().AddChild(bullet);
            bullet.GlobalPosition = GlobalPosition + new Vector2(0, -20);

            GD.Print("发射子弹");
        }

        public void TakeDamage(int damage)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.TakeDamage(damage);

            GD.Print($"玩家受伤，剩余生命: {playerModel.Health.Value}");
        }

        private void OnPlayerDied()
        {
            GD.Print("玩家死亡");

            var playerModel = this.GetModel<PlayerModel>();
            playerModel.LoseLife();

            if (playerModel.Lives.Value > 0)
            {
                // 重生
                Position = new Vector2(400, 500);
            }
            else
            {
                // 游戏结束
                var gameplaySystem = this.GetSystem<GameplaySystem>();
                gameplaySystem.GameOver();
            }
        }
    }
}
```

**代码说明**：

- 实现 `IController` 接口访问架构
- 使用 Godot 的输入系统
- 通过 Model 更新游戏状态
- 监听属性变化响应事件

## 步骤 5：创建游戏场景

### 5.1 主场景 (Main.tscn)

创建主场景并添加架构初始化脚本：

```csharp
// Scripts/Main.cs
using Godot;
using MyShooterGame.Architecture;

public partial class Main : Node
{
    private GameArchitecture _architecture;

    public override async void _Ready()
    {
        GD.Print("初始化游戏");

        // 创建并初始化架构
        _architecture = new GameArchitecture();
        await _architecture.InitializeAsync();

        GD.Print("架构初始化完成，切换到菜单");

        // 加载菜单场景
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
    }
}
```

### 5.2 菜单场景 (Menu.tscn)

创建菜单UI并添加控制脚本：

```csharp
// Scripts/UI/MenuController.cs
using Godot;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using GFramework.SourceGenerators.Abstractions.rule;
using MyShooterGame.Architecture;
using MyShooterGame.Systems;

[ContextAware]
public partial class MenuController : Control, IController
{
    public override void _Ready()
    {
        // 连接按钮信号
        GetNode<Button>("VBoxContainer/StartButton").Pressed += OnStartPressed;
        GetNode<Button>("VBoxContainer/QuitButton").Pressed += OnQuitPressed;
    }

    private void OnStartPressed()
    {
        GD.Print("开始游戏");

        // 初始化游戏（使用扩展方法访问架构（[ContextAware] 实现 IContextAware 接口））
        var gameplaySystem = this.GetSystem<GameplaySystem>();
        gameplaySystem.StartNewGame();

        // 切换到游戏场景
        GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

### 5.3 游戏场景 (Game.tscn)

创建游戏场景并添加控制脚本：

```csharp
// Scripts/GameScene.cs
using Godot;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.extensions;
using GFramework.SourceGenerators.Abstractions.rule;
using MyShooterGame.Architecture;
using MyShooterGame.Systems;
using MyShooterGame.Models;

[ContextAware]
public partial class GameScene : Node2D, IController
{
    [Export] public PackedScene EnemyScene;

    private SpawnSystem _spawnSystem;

    public override void _Ready()
    {
        // 初始化生成系统（使用扩展方法访问架构（[ContextAware] 实现 IContextAware 接口））
        _spawnSystem = this.GetSystem<SpawnSystem>();
        _spawnSystem.Initialize(this, EnemyScene);
        _spawnSystem.StartSpawning();

        // 监听游戏状态
        var gameModel = this.GetModel<GameModel>();
        gameModel.IsPlaying.RegisterOnValueChanged(isPlaying =>
        {
            if (!isPlaying)
            {
                OnGameOver();
            }
        });

        GD.Print("游戏场景已加载");
    }

    public override void _Process(double delta)
    {
        // 更新游戏时间
        var gameModel = this.GetModel<GameModel>();
        if (gameModel.IsPlaying.Value && !gameModel.IsPaused.Value)
        {
            gameModel.GameTime.Value += (float)delta;
        }

        // 暂停
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        var gameplaySystem = this.GetSystem<GameplaySystem>();
        var gameModel = this.GetModel<GameModel>();

        if (gameModel.IsPaused.Value)
        {
            gameplaySystem.ResumeGame();
        }
        else
        {
            gameplaySystem.PauseGame();
        }
    }

    private void OnGameOver()
    {
        GD.Print("游戏结束，返回菜单");
        _spawnSystem.StopSpawning();

        // 延迟返回菜单
        GetTree().CreateTimer(2.0).Timeout += () =>
        {
            GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
        };
    }
}
```

**代码说明**：

- `Main` 初始化架构
- `MenuController` 处理菜单交互
- `GameScene` 管理游戏场景
- 所有脚本通过架构访问系统和模型

## 完整代码

项目结构和所有代码文件已在上述步骤中提供。

## 运行结果

运行游戏后，你将看到：

1. **启动**：
    - 架构初始化
    - 自动进入主菜单

2. **主菜单**：
    - 显示开始和退出按钮
    - 点击开始进入游戏

3. **游戏场景**：
    - 玩家可以移动和射击
    - 敌人定时生成
    - HUD 显示分数和生命值
    - 按 ESC 暂停游戏

4. **游戏结束**：
    - 玩家生命值为 0 时游戏结束
    - 显示最终分数
    - 自动返回主菜单

**验证步骤**：

1. 架构正确初始化
2. 场景切换正常
3. 玩家控制响应
4. 敌人生成系统工作
5. 数据模型正确更新
6. 暂停功能正常

## 下一步

恭喜！你已经完成了一个基础的 Godot 游戏项目。接下来可以：

- 添加更多游戏功能（道具、关卡等）
- 实现完整的 UI 系统
- 添加音效和音乐
- 优化性能和体验
- 发布到不同平台

## 相关文档

- [Godot 架构集成](/zh-CN/godot/architecture) - 架构详细说明
- [Godot 场景系统](/zh-CN/godot/scene) - 场景管理
- [Godot UI 系统](/zh-CN/godot/ui) - UI 管理
- [Godot 扩展](/zh-CN/godot/extensions) - 扩展功能

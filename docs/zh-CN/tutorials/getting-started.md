# 从零开始 - GFramework 游戏开发教程

> 这是一个完整的从零开始的教程，将带领你创建一个使用 GFramework 的简单游戏项目。

## 📋 目录

- [环境准备](#环境准备)
- [项目创建](#项目创建)
- [架构设计](#架构设计)
- [功能实现](#功能实现)
- [测试验证](#测试验证)
- [项目打包](#项目打包)
- [进阶功能](#进阶功能)

## 环境准备

### 系统要求

- **操作系统**: Windows 10+, macOS 10.15+, 或 Linux
- **.NET SDK**: 6.0 或更高版本
- **Godot 引擎**: 4.5.1 或更高版本
- **IDE**: Visual Studio 2022+, JetBrains Rider, 或 VS Code

### 安装 .NET SDK

1. 访问 [.NET 官网](https://dotnet.microsoft.com/download)
2. 下载并安装 .NET 6.0 SDK
3. 验证安装：

```bash
dotnet --version
# 应该显示 6.0.x 或更高版本
```

### 安装 Godot

1. 访问 [Godot 官网](https://godotengine.org/download)
2. 下载 Godot 4.5.1
3. 解压到合适的位置并启动
4. 在编辑器设置中确认 .NET 支持

### 验证环境

创建一个测试项目验证环境：

```bash
# 创建测试项目
dotnet new console -n TestProject
cd TestProject

# 如果使用 Godot，添加 Godot 引用
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Godot

# 编译测试
dotnet build
```

## 项目创建

### 1. 创建新的 Godot 项目

1. 打开 Godot 编辑器
2. 点击 "新建项目"
3. 创建项目文件夹，命名为 "MyGFrameworkGame"
4. 选择 C# 作为脚本语言

### 2. 配置项目结构

在项目根目录创建以下文件夹结构：

```
MyGFrameworkGame/
├── src/
│   ├── Game/           # 游戏逻辑
│   │   ├── Models/     # 数据模型
│   │   ├── Systems/    # 业务系统
│   │   ├── Controllers/ # 控制器
│   │   └── Utilities/  # 工具类
│   └── Game.Core/      # 核心游戏组件
├── assets/             # 游戏资源
│   ├── scenes/
│   ├── textures/
│   ├── audio/
│   └── ui/
└── project.godot       # Godot 项目文件
```

### 3. 配置项目文件

创建 `src/Game/Game.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>MyGFrameworkGame</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Game.Core\Game.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Game" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Godot" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.SourceGenerators" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.SourceGenerators.Attributes" Version="1.0.0" />
  </ItemGroup>

</Project>
```

创建 `src/Game.Core/Game.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>MyGFrameworkGame.Core</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Core.Abstractions" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Game" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Game.Abstractions" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### 4. 配置 Godot 项目

在 `project.godot` 中添加 C# 配置：

```ini
; Engine configuration file.
; It's best edited using the editor UI and not directly,
; since the parameters that go here are not all obvious.
;
; Format:
;   [section] ; section goes between []
;   param=value ; assign values to parameters

[application]

config/name="My GFramework Game"
config/description="A game built with GFramework"
run/main_scene="res://src/Game/MainScene.tscn"
config/features=PackedStringArray("4.5", "Forward Plus")

[dotnet]

project/assembly_name="MyGFrameworkGame"
project/solution_directory="src/"

```

## 架构设计

### 1. 定义游戏架构

创建 `src/Game.Core/Architecture/GameArchitecture.cs`:

```csharp
using GFramework.Core.architecture;
using MyGFrameworkGame.Core.Models;
using MyGFrameworkGame.Core.Systems;
using MyGFrameworkGame.Core.Utilities;

namespace MyGFrameworkGame.Core.Architecture
{
    public class GameArchitecture : AbstractArchitecture
    {
        protected override void Init()
        {
            // 注册游戏模型
            RegisterModel(new PlayerModel());
            RegisterModel(new GameModel());
            RegisterModel(new ScoreModel());
            
            // 注册游戏系统
            RegisterSystem(new PlayerControllerSystem());
            RegisterSystem(new EnemySpawnerSystem());
            RegisterSystem(new CollisionSystem());
            RegisterSystem(new ScoreSystem());
            
            // 注册工具类
            RegisterUtility(new StorageUtility());
            RegisterUtility(new AudioUtility());
            RegisterUtility(new AssetLoadUtility());
        }
        
        protected override void InstallModules()
        {
            // 如果需要 Godot 特定模块
            // InstallGodotModule(new AudioModule());
        }
    }
}
```

### 2. 创建核心模型

创建 `src/Game.Core/Models/PlayerModel.cs`:

```csharp
using GFramework.Core.model;

namespace MyGFrameworkGame.Core.Models
{
    public class PlayerModel : AbstractModel
    {
        public BindableProperty<int> Health { get; } = new(100);
        public BindableProperty<int> MaxHealth { get; } = new(100);
        public BindableProperty<float> Speed { get; } = new(300.0f);
        public BindableProperty<int> Score { get; } = new(0);
        public BindableProperty<bool> IsAlive { get; } = new(true);
        public BindableProperty<bool> CanShoot { get; } = new(true);
        
        protected override void OnInit()
        {
            // 监听生命值变化
            Health.Register(health => {
                if (health <= 0)
                {
                    IsAlive.Value = false;
                    SendEvent(new PlayerDeathEvent());
                }
            });
            
            // 监听分数变化
            Score.Register(score => {
                if (score % 100 == 0)
                {
                    SendEvent(new MilestoneEvent { Score = score });
                }
            });
        }
        
        public void TakeDamage(int damage)
        {
            if (IsAlive.Value)
            {
                var newHealth = Math.Max(0, Health.Value - damage);
                Health.Value = newHealth;
            }
        }
        
        public void Heal(int amount)
        {
            var newHealth = Math.Min(MaxHealth.Value, Health.Value + amount);
            Health.Value = newHealth;
        }
        
        public bool CanShoot()
        {
            return IsAlive.Value && CanShoot.Value;
        }
    }
    
    // 游戏事件
    public struct PlayerDeathEvent { }
    public struct MilestoneEvent { public int Score; }
}
```

创建 `src/Game.Core/Models/GameModel.cs`:

```csharp
using GFramework.Core.model;

namespace MyGFrameworkGame.Core.Models
{
    public class GameModel : AbstractModel
    {
        public BindableProperty<GameState> State { get; } = new(GameState.Menu);
        public BindableProperty<int> CurrentLevel { get; } = new(1);
        public BindableProperty<int> EnemyCount { get; } = new(0);
        public BindableProperty<float> GameTime { get; } = new(0.0f);
        public BindableProperty<bool> IsPaused { get; } = new(false);
        
        protected override void OnInit()
        {
            State.Register(state => {
                switch (state)
                {
                    case GameState.Playing:
                        SendEvent(new GameStartEvent());
                        break;
                    case GameState.Paused:
                        SendEvent(new GamePauseEvent());
                        break;
                    case GameState.GameOver:
                        SendEvent(new GameOverEvent());
                        break;
                    case GameState.Menu:
                        SendEvent(new GameMenuEvent());
                        break;
                }
            });
        }
        
        public void StartGame()
        {
            State.Value = GameState.Playing;
            EnemyCount.Value = 0;
            GameTime.Value = 0.0f;
        }
        
        public void PauseGame()
        {
            if (State.Value == GameState.Playing)
            {
                State.Value = GameState.Paused;
            }
        }
        
        public void ResumeGame()
        {
            if (State.Value == GameState.Paused)
            {
                State.Value = GameState.Playing;
            }
        }
        
        public void GameOver()
        {
            State.Value = GameState.GameOver;
        }
        
        public void ReturnToMenu()
        {
            State.Value = GameState.Menu;
        }
    }
    
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }
    
    // 游戏事件
    public struct GameStartEvent { }
    public struct GamePauseEvent { }
    public struct GameResumeEvent { }
    public struct GameOverEvent { }
    public struct GameMenuEvent { }
}
```

### 3. 创建核心系统

创建 `src/Game.Core/Systems/PlayerControllerSystem.cs`:

```csharp
using Godot;
using GFramework.Core.system;
using GFramework.Core.events;
using MyGFrameworkGame.Core.Models;

namespace MyGFrameworkGame.Core.Systems
{
    public class PlayerControllerSystem : AbstractSystem
    {
        private PlayerModel _playerModel;
        private GameModel _gameModel;
        
        protected override void OnInit()
        {
            _playerModel = GetModel<PlayerModel>();
            _gameModel = GetModel<GameModel>();
            
            // 监听输入事件
            RegisterEvent<PlayerInputEvent>(OnPlayerInput);
            RegisterEvent<GameStartEvent>(OnGameStart);
            RegisterEvent<GameOverEvent>(OnGameOver);
        }
        
        private void OnPlayerInput(PlayerInputEvent inputEvent)
        {
            if (_gameModel.State.Value != GameState.Playing)
                return;
                
            switch (inputEvent.Action)
            {
                case "move":
                    HandleMovement(inputEvent.Direction);
                    break;
                case "shoot":
                    HandleShoot();
                    break;
                case "pause":
                    _gameModel.PauseGame();
                    break;
            }
        }
        
        private void HandleMovement(Vector2 direction)
        {
            if (!_playerModel.IsAlive.Value)
                return;
                
            // 发送移动事件，由 Godot 控制器处理
            SendEvent(new PlayerMoveEvent { Direction = direction.Normalized() * _playerModel.Speed.Value });
        }
        
        private void HandleShoot()
        {
            if (!_playerModel.CanShoot())
                return;
                
            // 发送射击事件
            SendEvent(new PlayerShootEvent());
            _playerModel.CanShoot.Value = false;
            
            // 重置射击冷却
            this.StartCoroutine(ShootCooldown());
        }
        
        private System.Collections.IEnumerator ShootCooldown()
        {
            yield return new WaitForSeconds(0.3f);
            _playerModel.CanShoot.Value = true;
        }
        
        private void OnGameStart(GameStartEvent e)
        {
            // 重置玩家状态
            _playerModel.Health.Value = _playerModel.MaxHealth.Value;
            _playerModel.Score.Value = 0;
            _playerModel.IsAlive.Value = true;
            _playerModel.CanShoot.Value = true;
        }
        
        private void OnGameOver(GameOverEvent e)
        {
            // 游戏结束时的处理
            SendEvent(new SaveScoreEvent { Score = _playerModel.Score.Value });
        }
    }
    
    // 输入事件
    public struct PlayerInputEvent
    {
        public string Action { get; set; }
        public Vector2 Direction { get; set; }
    }
    
    // 移动事件
    public struct PlayerMoveEvent
    {
        public Vector2 Direction { get; set; }
    }
    
    // 射击事件
    public struct PlayerShootEvent { }
    
    // 保存分数事件
    public struct SaveScoreEvent { public int Score; }
    
    // 协程等待类（简化版）
    public class WaitForSeconds
    {
        public float Seconds { get; }
        public WaitForSeconds(float seconds) => Seconds = seconds;
    }
}
```

## 功能实现

### 1. 创建主场景

创建 `src/Game/MainScene.cs`:

```csharp
using Godot;
using GFramework.Godot.extensions;
using GFramework.Godot.architecture;
using MyGFrameworkGame.Core.Architecture;
using MyGFrameworkGame.Core.Models;
using MyGFrameworkGame.Core.Systems;

namespace MyGFrameworkGame
{
    [ContextAware]
    [Log]
    public partial class MainScene : Node2D
    {
        private GameArchitecture _architecture;
        private Player _player;
        private UI _ui;
        private EnemySpawner _enemySpawner;
        
        public override void _Ready()
        {
            Logger.Info("Main scene ready");
            
            // 初始化架构
            InitializeArchitecture();
            
            // 创建游戏对象
            CreateGameObjects();
            
            // 注册事件监听
            RegisterEventListeners();
            
            // 开始游戏
            StartGame();
        }
        
        private void InitializeArchitecture()
        {
            Logger.Info("Initializing game architecture");
            
            _architecture = new GameArchitecture();
            _architecture.Initialize();
            
            // 设置上下文
            SetContext(_architecture.Context);
        }
        
        private void CreateGameObjects()
        {
            Logger.Info("Creating game objects");
            
            // 创建玩家
            var playerScene = GD.Load<PackedScene>("res://assets/scenes/Player.tscn");
            _player = playerScene.Instantiate<Player>();
            AddChild(_player);
            
            // 创建 UI
            var uiScene = GD.Load<PackedScene>("res://assets/scenes/UI.tscn");
            _ui = uiScene.Instantiate<UI>();
            AddChild(_ui);
            
            // 创建敌人生成器
            _enemySpawner = new EnemySpawner();
            AddChild(_enemySpawner);
        }
        
        private void RegisterEventListeners()
        {
            Logger.Info("Registering event listeners");
            
            // 游戏状态事件
            this.RegisterEvent<GameStartEvent>(OnGameStart)
                .UnRegisterWhenNodeExitTree(this);
                
            this.RegisterEvent<GameOverEvent>(OnGameOver)
                .UnRegisterWhenNodeExitTree(this);
                
            this.RegisterEvent<GamePauseEvent>(OnGamePause)
                .UnRegisterWhenNodeExitTree(this);
                
            // 玩家事件
            this.RegisterEvent<PlayerDeathEvent>(OnPlayerDeath)
                .UnRegisterWhenNodeExitTree(this);
                
            // 分数事件
            this.RegisterEvent<SaveScoreEvent>(OnSaveScore)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        private void StartGame()
        {
            Logger.Info("Starting game");
            var gameModel = Context.GetModel<GameModel>();
            gameModel.StartGame();
        }
        
        private void OnGameStart(GameStartEvent e)
        {
            Logger.Info("Game started");
            _ui.ShowGameplayUI();
        }
        
        private void OnGameOver(GameOverEvent e)
        {
            Logger.Info("Game over");
            _ui.ShowGameOverScreen();
        }
        
        private void OnGamePause(GamePauseEvent e)
        {
            Logger.Info("Game paused");
            _ui.ShowPauseMenu();
            GetTree().Paused = true;
        }
        
        private void OnPlayerDeath(PlayerDeathEvent e)
        {
            Logger.Info("Player died");
            var gameModel = Context.GetModel<GameModel>();
            gameModel.GameOver();
        }
        
        private void OnSaveScore(SaveScoreEvent e)
        {
            Logger.Info($"Saving score: {e.Score}");
            _ui.UpdateFinalScore(e.Score);
        }
        
        public override void _Input(InputEvent @event)
        {
            // 处理退出游戏
            if (@event.IsActionPressed("ui_cancel"))
            {
                var gameModel = Context.GetModel<GameModel>();
                if (gameModel.State.Value == GameState.Playing)
                {
                    gameModel.PauseGame();
                }
                else if (gameModel.State.Value == GameState.Paused)
                {
                    gameModel.ResumeGame();
                    GetTree().Paused = false;
                }
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _architecture?.Destroy();
            }
            base.Dispose(disposing);
        }
    }
}
```

### 2. 创建玩家控制器

创建 `src/Game/Player.cs`:

```csharp
using Godot;
using GFramework.Godot.extensions;
using GFramework.Core.events;
using MyGFrameworkGame.Core.Models;
using MyGFrameworkGame.Core.Systems;

namespace MyGFrameworkGame
{
    [ContextAware]
    [Log]
    public partial class Player : CharacterBody2D
    {
        [Export] public float Speed { get; set; } = 300.0f;
        
        private PlayerModel _playerModel;
        private GameModel _gameModel;
        private AnimatedSprite2D _animatedSprite;
        private CollisionShape2D _collisionShape;
        private Timer _invincibilityTimer;
        
        public override void _Ready()
        {
            Logger.Info("Player ready");
            
            // 获取组件引用
            _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
            _invincibilityTimer = GetNode<Timer>("InvincibilityTimer");
            
            // 设置上下文
            _playerModel = Context.GetModel<PlayerModel>();
            _gameModel = Context.GetModel<GameModel>();
            
            // 注册事件监听
            RegisterEventListeners();
            
            // 设置动画
            _animatedSprite.Play("idle");
        }
        
        private void RegisterEventListeners()
        {
            // 移动事件
            this.RegisterEvent<PlayerMoveEvent>(OnPlayerMove)
                .UnRegisterWhenNodeExitTree(this);
                
            // 射击事件
            this.RegisterEvent<PlayerShootEvent>(OnPlayerShoot)
                .UnRegisterWhenNodeExitTree(this);
                
            // 玩家状态变化
            _playerModel.Health.Register(OnHealthChanged)
                .UnRegisterWhenNodeExitTree(this);
                
            _playerModel.IsAlive.Register(OnAliveChanged)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        public override void _Process(double delta)
        {
            // 处理输入并发送事件
            HandleInput();
            
            // 更新位置
            var collision = MoveAndCollide(Velocity * (float)delta);
            if (collision != null)
            {
                HandleCollision(collision);
            }
        }
        
        private void HandleInput()
        {
            var inputVector = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            
            if (inputVector != Vector2.Zero)
            {
                // 发送移动事件
                SendEvent(new PlayerInputEvent
                {
                    Action = "move",
                    Direction = inputVector
                });
                
                // 更新动画
                UpdateAnimation(inputVector);
            }
            
            // 射击输入
            if (Input.IsActionJustPressed("shoot"))
            {
                SendEvent(new PlayerInputEvent { Action = "shoot" });
            }
        }
        
        private void OnPlayerMove(PlayerMoveEvent e)
        {
            Velocity = e.Direction * Speed;
        }
        
        private void OnPlayerShoot(PlayerShootEvent e)
        {
            Shoot();
        }
        
        private void OnHealthChanged(int newHealth)
        {
            Logger.Debug($"Player health changed: {newHealth}");
            
            // 受伤效果
            if (newHealth < _playerModel.MaxHealth.Value)
            {
                FlashRed();
                StartInvincibility();
            }
        }
        
        private void OnAliveChanged(bool isAlive)
        {
            if (!isAlive)
            {
                Die();
            }
        }
        
        private void Shoot()
        {
            Logger.Debug("Player shooting");
            
            // 创建子弹
            var bulletScene = GD.Load<PackedScene>("res://assets/scenes/Bullet.tscn");
            var bullet = bulletScene.Instantiate<Bullet>();
            
            // 设置子弹位置和方向
            bullet.Position = Position;
            bullet.Direction = Vector2.Up; // 向上射击
            
            // 添加到场景
            GetTree().Root.AddChild(bullet);
            
            // 播放射击音效
            PlayShootSound();
        }
        
        private void UpdateAnimation(Vector2 inputVector)
        {
            if (inputVector.Y < 0)
            {
                _animatedSprite.Play("up");
            }
            else if (inputVector.Y > 0)
            {
                _animatedSprite.Play("down");
            }
            else if (inputVector.X != 0)
            {
                _animatedSprite.Play("side");
                _animatedSprite.FlipH = inputVector.X < 0;
            }
            else
            {
                _animatedSprite.Play("idle");
            }
        }
        
        private void FlashRed()
        {
            _animatedSprite.Modulate = Colors.Red;
            this.CreateSignalBuilder(_invincibilityTimer.SignalName.Timeout)
                .Connect(() => _animatedSprite.Modulate = Colors.White)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        private void StartInvincibility()
        {
            SetCollisionLayerValue(1, false); // 关闭碰撞
            _invincibilityTimer.Start();
        }
        
        private void HandleCollision(KinematicCollision2D collision)
        {
            var collider = collision.GetCollider();
            
            if (collider is Enemy enemy && _playerModel.IsAlive.Value)
            {
                // 受到伤害
                _playerModel.TakeDamage(enemy.Damage);
            }
        }
        
        private void Die()
        {
            Logger.Info("Player died");
            
            // 播放死亡动画
            _animatedSprite.Play("death");
            
            // 禁用碰撞
            _collisionShape.Disabled = true;
            
            // 发送玩家死亡事件
            SendEvent(new PlayerDeathEvent());
            
            // 延迟移除
            this.CreateSignalBuilder(_animatedSprite.SignalName.AnimationFinished)
                .WithFlags(ConnectFlags.OneShot)
                .Connect(() => QueueFreeX())
                .UnRegisterWhenNodeExitTree(this);
        }
        
        private void PlayShootSound()
        {
            var audioPlayer = new AudioStreamPlayer();
            var sound = GD.Load<AudioStream>("res://assets/audio/shoot.wav");
            audioPlayer.Stream = sound;
            AddChild(audioPlayer);
            audioPlayer.Play();
            
            // 音效播放完成后移除
            this.CreateSignalBuilder(audioPlayer.SignalName.Finished)
                .WithFlags(ConnectFlags.OneShot)
                .Connect(() => audioPlayer.QueueFreeX())
                .UnRegisterWhenNodeExitTree(this);
        }
    }
}
```

### 3. 创建 UI 系统

创建 `src/Game/UI.cs`:

```csharp
using Godot;
using GFramework.Godot.extensions;
using GFramework.Core.events;
using MyGFrameworkGame.Core.Models;

namespace MyGFrameworkGame
{
    [ContextAware]
    [Log]
    public partial class UI : CanvasLayer
    {
        private Control _gameplayUI;
        private Control _pauseMenu;
        private Control _gameOverScreen;
        private Label _scoreLabel;
        private Label _healthLabel;
        private Label _finalScoreLabel;
        private Button _resumeButton;
        private Button _quitButton;
        private Button _restartButton;
        
        private PlayerModel _playerModel;
        private GameModel _gameModel;
        
        public override void _Ready()
        {
            Logger.Info("UI ready");
            
            // 获取 UI 组件引用
            InitializeUIComponents();
            
            // 设置上下文
            _playerModel = Context.GetModel<PlayerModel>();
            _gameModel = Context.GetModel<GameModel>();
            
            // 注册事件监听
            RegisterEventListeners();
            
            // 注册按钮事件
            RegisterButtonEvents();
            
            // 显示主菜单
            ShowMainMenu();
        }
        
        private void InitializeUIComponents()
        {
            // 游戏 UI
            _gameplayUI = GetNode<Control>("GameplayUI");
            _scoreLabel = GetNode<Label>("GameplayUI/ScoreLabel");
            _healthLabel = GetNode<Label>("GameplayUI/HealthLabel");
            
            // 暂停菜单
            _pauseMenu = GetNode<Control>("PauseMenu");
            _resumeButton = GetNode<Button>("PauseMenu/VBoxContainer/ResumeButton");
            _quitButton = GetNode<Button>("PauseMenu/VBoxContainer/QuitButton");
            
            // 游戏结束画面
            _gameOverScreen = GetNode<Control>("GameOverScreen");
            _finalScoreLabel = GetNode<Label>("GameOverScreen/CenterContainer/VBoxContainer/FinalScoreLabel");
            _restartButton = GetNode<Button>("GameOverScreen/CenterContainer/VBoxContainer/RestartButton");
        }
        
        private void RegisterEventListeners()
        {
            // 监听分数变化
            _playerModel.Score.Register(UpdateScoreDisplay)
                .UnRegisterWhenNodeExitTree(this);
                
            // 监听生命值变化
            _playerModel.Health.Register(UpdateHealthDisplay)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        private void RegisterButtonEvents()
        {
            // 暂停菜单按钮
            this.CreateSignalBuilder(_resumeButton.SignalName.Pressed)
                .Connect(() => {
                    _gameModel.ResumeGame();
                    GetTree().Paused = false;
                    ShowGameplayUI();
                })
                .UnRegisterWhenNodeExitTree(this);
                
            this.CreateSignalBuilder(_quitButton.SignalName.Pressed)
                .Connect(() => {
                    GetTree().Quit();
                })
                .UnRegisterWhenNodeExitTree(this);
                
            // 重新开始按钮
            this.CreateSignalBuilder(_restartButton.SignalName.Pressed)
                .Connect(() => {
                    RestartGame();
                })
                .UnRegisterWhenNodeExitTree(this);
        }
        
        public void ShowMainMenu()
        {
            HideAllUI();
            // TODO: 实现主菜单
            Logger.Info("Showing main menu");
        }
        
        public void ShowGameplayUI()
        {
            HideAllUI();
            _gameplayUI.Visible = true;
            UpdateUI();
        }
        
        public void ShowPauseMenu()
        {
            _pauseMenu.Visible = true;
        }
        
        public void ShowGameOverScreen()
        {
            HideAllUI();
            _gameOverScreen.Visible = true;
        }
        
        private void HideAllUI()
        {
            _gameplayUI.Visible = false;
            _pauseMenu.Visible = false;
            _gameOverScreen.Visible = false;
        }
        
        private void UpdateUI()
        {
            UpdateScoreDisplay(_playerModel.Score.Value);
            UpdateHealthDisplay(_playerModel.Health.Value);
        }
        
        private void UpdateScoreDisplay(int score)
        {
            _scoreLabel.Text = $"Score: {score}";
        }
        
        private void UpdateHealthDisplay(int health)
        {
            _healthLabel.Text = $"Health: {health}";
            
            // 根据生命值改变颜色
            if (health <= 20)
            {
                _healthLabel.Modulate = Colors.Red;
            }
            else if (health <= 50)
            {
                _healthLabel.Modulate = Colors.Yellow;
            }
            else
            {
                _healthLabel.Modulate = Colors.White;
            }
        }
        
        public void UpdateFinalScore(int score)
        {
            _finalScoreLabel.Text = $"Final Score: {score}";
        }
        
        private void RestartGame()
        {
            Logger.Info("Restarting game");
            
            // 重新加载场景
            GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
        }
    }
}
```

## 测试验证

### 1. 创建测试项目

创建 `tests/Game.Tests/Game.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>MyGFrameworkGame.Tests</RootNamespace>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.3.2" />
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Core.Tests" Version="1.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Game.Core\Game.Core.csproj" />
    </ItemGroup>

</Project>
```

### 2. 创建单元测试

创建 `tests/Game.Tests/PlayerModelTests.cs`:

```csharp
using NUnit.Framework;
using GFramework.Core.Tests;
using MyGFrameworkGame.Core.Models;

namespace MyGFrameworkGame.Tests
{
    [TestFixture]
    public class PlayerModelTests : ArchitectureTestsBase<TestArchitecture>
    {
        private PlayerModel _playerModel;
        
        protected override TestArchitecture CreateArchitecture()
        {
            return new TestArchitecture();
        }
        
        [SetUp]
        public void Setup()
        {
            Architecture.Initialize();
            _playerModel = Architecture.Context.GetModel<PlayerModel>();
        }
        
        [Test]
        public void PlayerModel_InitialValues_ShouldBeCorrect()
        {
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(100));
            Assert.That(_playerModel.MaxHealth.Value, Is.EqualTo(100));
            Assert.That(_playerModel.Speed.Value, Is.EqualTo(300.0f));
            Assert.That(_playerModel.Score.Value, Is.EqualTo(0));
            Assert.That(_playerModel.IsAlive.Value, Is.True);
            Assert.That(_playerModel.CanShoot.Value, Is.True);
        }
        
        [Test]
        public void TakeDamage_ValidDamage_ShouldReduceHealth()
        {
            // Act
            _playerModel.TakeDamage(20);
            
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(80));
            Assert.That(_playerModel.IsAlive.Value, Is.True);
        }
        
        [Test]
        public void TakeDamage_LethalDamage_ShouldKillPlayer()
        {
            // Act
            _playerModel.TakeDamage(150);
            
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(0));
            Assert.That(_playerModel.IsAlive.Value, Is.False);
        }
        
        [Test]
        public void TakeDamage_WhenDead_ShouldNotChangeHealth()
        {
            // Arrange
            _playerModel.TakeDamage(100); // Kill player
            var healthAfterDeath = _playerModel.Health.Value;
            
            // Act
            _playerModel.TakeDamage(10);
            
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(healthAfterDeath));
        }
        
        [Test]
        public void Heal_ValidAmount_ShouldRestoreHealth()
        {
            // Arrange
            _playerModel.TakeDamage(30); // Health = 70
            
            // Act
            _playerModel.Heal(20);
            
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(90));
        }
        
        [Test]
        public void Heal_ExcessAmount_ShouldNotExceedMaxHealth()
        {
            // Arrange
            _playerModel.TakeDamage(50); // Health = 50
            
            // Act
            _playerModel.Heal(100); // Try to heal more than max
            
            // Assert
            Assert.That(_playerModel.Health.Value, Is.EqualTo(100)); // Should cap at max
        }
        
        [Test]
        public void CanShoot_WhenAliveAndNotOnCooldown_ShouldReturnTrue()
        {
            // Act & Assert
            Assert.That(_playerModel.CanShoot(), Is.True);
        }
        
        [Test]
        public void CanShoot_WhenDead_ShouldReturnFalse()
        {
            // Arrange
            _playerModel.TakeDamage(100);
            
            // Act & Assert
            Assert.That(_playerModel.CanShoot(), Is.False);
        }
        
        [Test]
        public void CanShoot_WhenOnCooldown_ShouldReturnFalse()
        {
            // Arrange
            _playerModel.CanShoot.Value = false;
            
            // Act & Assert
            Assert.That(_playerModel.CanShoot(), Is.False);
        }
    }
    
    // 测试架构
    public class TestArchitecture : Architecture
    {
        protected override void Init()
        {
            RegisterModel(new PlayerModel());
        }
    }
}
```

### 3. 运行测试

```bash
cd tests/Game.Tests
dotnet test
```

### 4. 集成测试

创建 `tests/Game.Tests/GameIntegrationTests.cs`:

```csharp
using NUnit.Framework;
using Godot;
using MyGFrameworkGame;

namespace MyGFrameworkGame.Tests
{
    [TestFixture]
    public class GameIntegrationTests
    {
        private SceneTree _sceneTree;
        private MainScene _mainScene;
        
        [SetUp]
        public void Setup()
        {
            // 创建测试场景树
            _sceneTree = new SceneTree();
            _sceneTree.Root = new Node();
            
            // 创建主场景
            _mainScene = new MainScene();
            _sceneTree.Root.AddChild(_mainScene);
        }
        
        [TearDown]
        public void TearDown()
        {
            _mainScene?.QueueFree();
            _sceneTree?.Quit();
        }
        
        [Test]
        public void MainScene_Ready_ShouldInitializeCorrectly()
        {
            // Act
            _mainScene._Ready();
            
            // Wait a frame for initialization
            _sceneTree.ProcessFrame();
            
            // Assert
            Assert.That(_mainScene.GetChildCount(), Is.GreaterThan(0));
            // 验证架构已初始化
            var context = _mainScene.Context;
            Assert.That(context, Is.Not.Null);
        }
        
        [Test]
        public void Game_StartGame_ShouldChangeGameStateToPlaying()
        {
            // Arrange
            _mainScene._Ready();
            _sceneTree.ProcessFrame();
            
            var gameModel = _mainScene.Context.GetModel<GameModel>();
            
            // Act
            gameModel.StartGame();
            
            // Assert
            Assert.That(gameModel.State.Value, Is.EqualTo(GameState.Playing));
        }
    }
}
```

## 项目打包

### 1. 配置导出设置

1. 在 Godot 编辑器中，点击 "项目" → "导出"
2. 添加 Windows/macOS/Linux 导出预设
3. 配置导出选项：
   - **目标**: 选择目标平台
   - **调试信息**: 发布版本选择 "无"
   - **优化级别**: "速度优化"
   - **PCK 加密**: 可选的安全选项

### 2. 创建导出脚本

创建 `scripts/export.py`:

```python
# Godot 导出脚本
import os

def export_game():
    # Windows 导出
    os.system("godot --headless --export-release \"Windows Desktop\" build/windows/game.exe")
    
    # macOS 导出
    os.system("godot --headless --export-release \"macOS\" build/macOS/game.zip")
    
    # Linux 导出
    os.system("godot --headless --export-release \"Linux/X11\" build/linux/game.x86_64")

if __name__ == "__main__":
    export_game()
```

### 3. 自动化构建

创建 `.github/workflows/build.yml`:

```yaml
name: Build Game

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '6.0.x'
        
    - name: Restore dependencies
      run: dotnet restore src/Game/Game.csproj
      
    - name: Build
      run: dotnet build src/Game/Game.csproj --no-restore
      
    - name: Test
      run: dotnet test tests/Game.Tests/Game.Tests.csproj --no-build --verbosity normal
      
    - name: Setup Godot
      uses: chickensoft-games/setup-godot@v1
      with:
        version: 4.5.1
        
    - name: Export Game
      run: |
        mkdir -p build
        godot --headless --export-release "Windows Desktop" build/game.exe
```

## 进阶功能

### 1. 添加音效系统

创建 `src/Game.Core/Systems/AudioSystem.cs`:

```csharp
using Godot;
using GFramework.Core.system;
using MyGFrameworkGame.Core.Models;

namespace MyGFrameworkGame.Core.Systems
{
    public class AudioSystem : AbstractSystem
    {
        private AudioStreamPlayer _musicPlayer;
        private AudioStreamPlayer _sfxPlayer;
        
        protected override void OnInit()
        {
            // 注册音频事件监听
            RegisterEvent<PlayerShootEvent>(OnPlayerShoot);
            RegisterEvent<PlayerDeathEvent>(OnPlayerDeath);
            RegisterEvent<GameStartEvent>(OnGameStart);
        }
        
        private void OnPlayerShoot(PlayerShootEvent e)
        {
            PlaySound("shoot");
        }
        
        private void OnPlayerDeath(PlayerDeathEvent e)
        {
            PlaySound("death");
        }
        
        private void OnGameStart(GameStartEvent e)
        {
            PlayMusic("background");
        }
        
        private void PlaySound(string soundName)
        {
            var sound = GD.Load<AudioStream>($"res://assets/audio/{soundName}.wav");
            _sfxPlayer.Stream = sound;
            _sfxPlayer.Play();
        }
        
        private void PlayMusic(string musicName)
        {
            var music = GD.Load<AudioStream>($"res://assets/audio/{musicName}.ogg");
            _musicPlayer.Stream = music;
            _musicPlayer.Play();
        }
    }
}
```

### 2. 添加存档系统

创建 `src/Game.Core/Systems/SaveSystem.cs`:

```csharp
using System.IO;
using GFramework.Core.system;
using GFramework.Core.command;
using MyGFrameworkGame.Core.Models;
using Newtonsoft.Json;

namespace MyGFrameworkGame.Core.Systems
{
    public class SaveSystem : AbstractSystem
    {
        private const string SavePath = "user://saves/";
        
        protected override void OnInit()
        {
            // 创建保存目录
            if (!DirAccess.DirExistsAbsolute(SavePath))
            {
                DirAccess.Open("user://").MakeDir("saves");
            }
            
            // 注册保存命令
            RegisterCommand<SaveGameCommand>(SaveGame);
            RegisterCommand<LoadGameCommand>(LoadGame);
        }
        
        private void SaveGame(SaveGameCommand command)
        {
            var playerModel = GetModel<PlayerModel>();
            var gameModel = GetModel<GameModel>();
            
            var saveData = new SaveData
            {
                PlayerHealth = playerModel.Health.Value,
                PlayerScore = playerModel.Score.Value,
                CurrentLevel = gameModel.CurrentLevel.Value,
                Timestamp = System.DateTime.Now
            };
            
            var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            var file = FileAccess.Open($"{SavePath}save_{command.SlotId}.json", FileAccess.ModeFlags.Write);
            file.StoreString(json);
            file.Close();
        }
        
        private void LoadGame(LoadGameCommand command)
        {
            var filePath = $"{SavePath}save_{command.SlotId}.json";
            
            if (!FileAccess.FileExists(filePath))
            {
                GD.PrintErr($"Save file not found: {filePath}");
                return;
            }
            
            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            file.Close();
            
            var saveData = JsonConvert.DeserializeObject<SaveData>(json);
            
            // 恢复游戏状态
            var playerModel = GetModel<PlayerModel>();
            var gameModel = GetModel<GameModel>();
            
            playerModel.Health.Value = saveData.PlayerHealth;
            playerModel.Score.Value = saveData.PlayerScore;
            gameModel.CurrentLevel.Value = saveData.CurrentLevel;
        }
    }
    
    public class SaveData
    {
        public int PlayerHealth { get; set; }
        public int PlayerScore { get; set; }
        public int CurrentLevel { get; set; }
        public System.DateTime Timestamp { get; set; }
    }
    
    public class SaveGameCommand : AbstractCommand
    {
        public int SlotId { get; set; }
    }
    
    public class LoadGameCommand : AbstractCommand
    {
        public int SlotId { get; set; }
    }
}
```

### 3. 添加本地化支持

创建 `src/Game.Core/Systems/LocalizationSystem.cs`:

```csharp
using System.Collections.Generic;
using Godot;
using GFramework.Core.system;

namespace MyGFrameworkGame.Core.Systems
{
    public class LocalizationSystem : AbstractSystem
    {
        private Dictionary<string, string> _translations = new();
        private string _currentLanguage = "en";
        
        protected override void OnInit()
        {
            LoadLanguage(_currentLanguage);
        }
        
        public void SetLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            LoadLanguage(languageCode);
        }
        
        public string GetText(string key)
        {
            return _translations.GetValueOrDefault(key, key);
        }
        
        private void LoadLanguage(string languageCode)
        {
            var translationFile = GD.Load<Translation>($"res://assets/locales/{languageCode}.po");
            if (translationFile != null)
            {
                TranslationServer.SetLocale(languageCode);
                GD.Print($"Loaded language: {languageCode}");
            }
            else
            {
                GD.PrintErr($"Failed to load language: {languageCode}");
            }
        }
    }
}
```

---

## 总结

恭喜！你已经成功创建了一个完整的 GFramework 游戏项目。这个项目包含了：

- ✅ **完整的架构设计** - 使用 GFramework 的五层架构
- ✅ **丰富的游戏功能** - 玩家控制、UI、音效、存档等
- ✅ **完善的测试** - 单元测试和集成测试
- ✅ **自动化构建** - CI/CD 流水线配置

### 下一步建议

1. **扩展游戏内容**：添加更多敌人、关卡、道具
2. **优化性能**：使用对象池、减少内存分配
3. **添加网络功能**：多人游戏、排行榜
4. **完善 UI**：更丰富的界面和动画
5. **发布游戏**：在各个平台发布你的作品
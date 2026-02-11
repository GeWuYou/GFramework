# 基础教程

这是一个完整的从零开始的教程，将带领你创建一个使用 GFramework 的简单游戏项目。

## 📋 目录

- [环境准备](#环境准备)
- [项目创建](#项目创建)
- [架构设计](#架构设计)
- [功能实现](#功能实现)
- [测试验证](#测试验证)

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

## 架构设计

### 1. 定义游戏架构

创建 `src/Game.Core/Architecture/GameArchitecture.cs`:

```csharp
using GFramework.Core.architecture;
using MyGFrameworkGame.Core.Models;
using MyGFrameworkGame.Core.Systems;

namespace MyGFrameworkGame.Core.Architecture
{
    public class GameArchitecture : AbstractArchitecture
    {
        protected override void Init()
        {
            // 注册游戏模型
            RegisterModel(new PlayerModel());
            RegisterModel(new GameModel());
            
            // 注册游戏系统
            RegisterSystem(new PlayerControllerSystem());
            RegisterSystem(new CollisionSystem());
            
            // 注册工具类
            RegisterUtility(new StorageUtility());
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
        public BindableProperty<int> Score { get; } = new(0);
        public BindableProperty<bool> IsAlive { get; } = new(true);
        
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
        }
        
        public void TakeDamage(int damage)
        {
            if (IsAlive.Value)
            {
                Health.Value = Math.Max(0, Health.Value - damage);
            }
        }
    }
    
    // 游戏事件
    public struct PlayerDeathEvent { }
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

namespace MyGFrameworkGame
{
    [ContextAware]
    [Log]
    public partial class MainScene : Node2D
    {
        private GameArchitecture _architecture;
        private Player _player;
        
        public override void _Ready()
        {
            Logger.Info("Main scene ready");
            
            // 初始化架构
            InitializeArchitecture();
            
            // 创建游戏对象
            CreateGameObjects();
            
            // 注册事件监听
            RegisterEventListeners();
        }
        
        private void InitializeArchitecture()
        {
            _architecture = new GameArchitecture();
            _architecture.Initialize();
            SetContext(_architecture.Context);
        }
        
        private void CreateGameObjects()
        {
            var playerScene = GD.Load<PackedScene>("res://assets/scenes/Player.tscn");
            _player = playerScene.Instantiate<Player>();
            AddChild(_player);
        }
        
        private void RegisterEventListeners()
        {
            this.RegisterEvent<PlayerDeathEvent>(OnPlayerDeath)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        private void OnPlayerDeath(PlayerDeathEvent e)
        {
            Logger.Info("Player died");
            // 处理玩家死亡逻辑
        }
    }
}
```

### 2. 创建玩家控制器

创建 `src/Game/Player.cs`:

```csharp
using Godot;
using GFramework.Godot.extensions;
using MyGFrameworkGame.Core.Models;

namespace MyGFrameworkGame
{
    [ContextAware]
    [Log]
    public partial class Player : CharacterBody2D
    {
        private PlayerModel _playerModel;
        
        public override void _Ready()
        {
            _playerModel = Context.GetModel<PlayerModel>();
            
            // 注册事件监听
            this.RegisterEvent<PlayerMoveEvent>(OnPlayerMove)
                .UnRegisterWhenNodeExitTree(this);
        }
        
        public override void _Process(double delta)
        {
            HandleInput();
            MoveAndSlide();
        }
        
        private void HandleInput()
        {
            var inputVector = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            if (inputVector != Vector2.Zero)
            {
                SendEvent(new PlayerMoveEvent { Direction = inputVector });
            }
        }
        
        private void OnPlayerMove(PlayerMoveEvent e)
        {
            Velocity = e.Direction * 300.0f;
        }
    }
    
    public struct PlayerMoveEvent 
    { 
        public Vector2 Direction { get; set; } 
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
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.3.2" />
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
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
using MyGFrameworkGame.Core.Models;
using GFramework.Core.architecture;

namespace MyGFrameworkGame.Tests
{
    [TestFixture]
    public class PlayerModelTests
    {
        private TestArchitecture _architecture;
        private PlayerModel _playerModel;
        
        [SetUp]
        public void Setup()
        {
            _architecture = new TestArchitecture();
            _architecture.Initialize();
            _playerModel = _architecture.GetModel<PlayerModel>();
        }
        
        [Test]
        public void PlayerModel_InitialValues_ShouldBeCorrect()
        {
            Assert.That(_playerModel.Health.Value, Is.EqualTo(100));
            Assert.That(_playerModel.Score.Value, Is.EqualTo(0));
            Assert.That(_playerModel.IsAlive.Value, Is.True);
        }
        
        [Test]
        public void TakeDamage_ValidDamage_ShouldReduceHealth()
        {
            _playerModel.TakeDamage(20);
            Assert.That(_playerModel.Health.Value, Is.EqualTo(80));
            Assert.That(_playerModel.IsAlive.Value, Is.True);
        }
        
        [Test]
        public void TakeDamage_LethalDamage_ShouldKillPlayer()
        {
            _playerModel.TakeDamage(150);
            Assert.That(_playerModel.Health.Value, Is.EqualTo(0));
            Assert.That(_playerModel.IsAlive.Value, Is.False);
        }
    }
    
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

## 总结

恭喜！你已经成功创建了一个完整的 GFramework 游戏项目基础框架。这个项目包含了：

- ✅ **完整的架构设计** - 使用 GFramework 的五层架构
- ✅ **核心游戏功能** - 玩家控制、基本游戏循环
- ✅ **完善的测试** - 单元测试验证核心逻辑

### 下一步建议

1. **扩展游戏内容**：添加敌人、道具、关卡系统
2. **完善 UI 系统**：创建菜单、HUD、游戏结束界面
3. **添加音效系统**：背景音乐、音效播放
4. **实现存档功能**：游戏进度保存和加载
5. **优化性能**：使用对象池、减少内存分配

### 学习资源

- [GFramework 主文档](../)
- [Core 模块文档](../core)
- [Godot 集成文档](../godot/)

享受游戏开发的乐趣吧！🎮
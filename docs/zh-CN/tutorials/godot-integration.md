# Godot 集成教程

> 深入学习如何将 GFramework 与 Godot 引擎完美集成，创建高性能的游戏应用。

## 📋 目录

- [Godot 特定功能](#godot-特定功能)
- [节点生命周期管理](#节点生命周期管理)
- [信号系统集成](#信号系统集成)
- [资源管理优化](#资源管理优化)
- [性能优化技巧](#性能优化技巧)
- [常见集成模式](#常见集成模式)
- [调试与测试](#调试与测试)

## Godot 特定功能

### 1. 节点生命周期绑定

GFramework.Godot 提供了与 Godot 节点生命周期的无缝集成，确保框架初始化与 Godot 场景树同步。

```csharp
using GFramework.Godot.architecture;

public class GodotGameArchitecture : AbstractArchitecture
{
    protected override void Init()
    {
        // 注册核心组件
        RegisterModel(new PlayerModel());
        RegisterSystem(new PlayerControllerSystem());
        RegisterUtility(new AudioUtility());
    }
    
    protected override void InstallModules()
    {
        // 安装 Godot 特定模块
        InstallGodotModule(new AudioModule());
        InstallGodotModule(new InputModule());
    }
}
```

### 2. Godot 模块系统

创建与 Godot 节点深度集成的模块：

```csharp
[ContextAware]
[Log]
public partial class AudioModule : AbstractGodotModule
{
    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _sfxPlayer;
    
    // 模块节点本身
    public override Node Node => this;
    
    public override void Install(IArchitecture architecture)
    {
        // 注册音频系统
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
    
    public override void OnAttach(Architecture architecture)
    {
        // 模块附加时的初始化
        Logger.Info("Audio module attached");
        
        // 创建音频播放器
        CreateAudioPlayers();
    }
    
    public override void OnDetach(Architecture architecture)
    {
        // 模块分离时的清理
        Logger.Info("Audio module detached");
        CleanupAudioPlayers();
    }
    
    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                // 架构准备就绪，开始播放背景音乐
                PlayBackgroundMusic();
                break;
        }
    }
    
    private void CreateAudioPlayers()
    {
        _musicPlayer = new AudioStreamPlayer();
        AddChild(_musicPlayer);
        
        _sfxPlayer = new AudioStreamPlayer();
        AddChild(_sfxPlayer);
    }
    
    private void CleanupAudioPlayers()
    {
        _musicPlayer?.QueueFree();
        _sfxPlayer?.QueueFree();
    }
    
    private void PlayBackgroundMusic()
    {
        var music = GD.Load<AudioStream>("res://assets/audio/background.ogg");
        _musicPlayer.Stream = music;
        _musicPlayer.Play();
    }
}
```

### 3. 节点池化系统

实现高效的 Godot 节点池化，避免频繁的创建和销毁：

```csharp
using GFramework.Godot.pool;

public class BulletPoolSystem : AbstractNodePoolSystem<string, Bullet>
{
    private Dictionary<string, PackedScene> _scenes = new();
    
    public BulletPoolSystem()
    {
        // 预加载场景
        _scenes["player"] = GD.Load<PackedScene>("res://assets/scenes/PlayerBullet.tscn");
        _scenes["enemy"] = GD.Load<PackedScene>("res://assets/scenes/EnemyBullet.tscn");
    }
    
    protected override Bullet CreateItem(string key)
    {
        if (_scenes.TryGetValue(key, out var scene))
        {
            return scene.Instantiate<Bullet>();
        }
        
        throw new ArgumentException($"Unknown bullet type: {key}");
    }
    
    protected override void OnSpawn(Bullet item, string key)
    {
        // 重置子弹状态
        item.Reset();
        item.Position = Vector2.Zero;
        item.Visible = true;
        item.SetCollisionLayerValue(1, true);
        item.SetCollisionMaskValue(1, true);
    }
    
    protected override void OnDespawn(Bullet item)
    {
        // 隐藏子弹
        item.Visible = false;
        item.SetCollisionLayerValue(1, false);
        item.SetCollisionMaskValue(1, false);
        
        // 移除父节点
        item.GetParent()?.RemoveChild(item);
    }
    
    protected override bool CanDespawn(Bullet item)
    {
        // 只有不在使用中的子弹才能回收
        return !item.IsActive && item.GetParent() != null;
    }
}
```

## 节点生命周期管理

### 1. 自动生命周期绑定

使用 GFramework 的扩展方法自动管理节点生命周期：

```csharp
[ContextAware]
[Log]
public partial class PlayerController : CharacterBody2D, IController
{
    private PlayerModel _playerModel;
    
    public override void _Ready()
    {
        // 设置上下文
        _playerModel = this.GetModel<PlayerModel>();
        
        // 注册事件监听，自动与节点生命周期绑定
        this.RegisterEvent<PlayerInputEvent>(OnPlayerInput)
            .UnRegisterWhenNodeExitTree(this);
            
        // 监听属性变化，自动清理
        _playerModel.Health.Register(OnHealthChanged)
            .UnRegisterWhenNodeExitTree(this);
            
        // 连接 Godot 信号，自动清理
        this.CreateSignalBuilder(AnimationPlayer.SignalName.AnimationFinished)
            .Connect(OnAnimationFinished)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnPlayerInput(PlayerInputEvent e)
    {
        // 处理玩家输入
        ProcessInput(e);
    }
    
    private void OnHealthChanged(int newHealth)
    {
        // 更新 UI 显示
        UpdateHealthDisplay(newHealth);
        
        // 播放受伤动画
        if (newHealth < _playerModel.PreviousHealth)
        {
            AnimationPlayer.Play("hurt");
        }
    }
    
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "hurt")
        {
            AnimationPlayer.Play("idle");
        }
    }
}
```

### 2. 延迟初始化模式

对于需要在特定时机初始化的组件：

```csharp
[ContextAware]
[Log]
public partial class AdvancedController : Node, IController
{
    private bool _initialized = false;
    
    public override void _Ready()
    {
        // 不立即初始化，等待特定条件
        this.WaitUntil(() => IsInitializationReady())
            .Then(InitializeController);
    }
    
    private bool IsInitializationReady()
    {
        // 检查所有依赖是否准备就绪
        return HasRequiredComponents() && Context.HasModel<GameModel>();
    }
    
    private void InitializeController()
    {
        if (_initialized) return;
        
        Logger.Info("Initializing advanced controller");
        
        // 执行初始化逻辑
        SetupComponents();
        RegisterEventListeners();
        StartUpdateLoop();
        
        _initialized = true;
    }
    
    private void SetupComponents()
    {
        // 设置组件
    }
    
    private void RegisterEventListeners()
    {
        // 注册事件监听器
        this.RegisterEvent<GameStateChangeEvent>(OnGameStateChanged)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void StartUpdateLoop()
    {
        // 启动更新循环
        SetProcess(true);
    }
}
```

### 3. 安全的节点操作

使用 GFramework 提供的安全扩展方法：

```csharp
public partial class SafeNodeOperations : Node
{
    public void SafeOperations()
    {
        // 安全获取子节点
        var player = GetNodeX<Player>("Player");
        var ui = FindChildX<UI>("UI");
        
        // 安全添加子节点
        var bullet = bulletScene.Instantiate<Bullet>();
        AddChildX(bullet);
        
        // 安全的异步操作
        this.WaitUntilReadyAsync()
            .Then(() => {
                // 节点准备就绪后的操作
                InitializeAfterReady();
            });
            
        // 安全的场景树遍历
        this.ForEachChild<Node>(child => {
            if (child is Sprite2D sprite)
            {
                ProcessSprite(sprite);
            }
        });
    }
    
    private void ProcessSprite(Sprite2D sprite)
    {
        // 处理精灵
    }
    
    private void InitializeAfterReady()
    {
        // 初始化逻辑
    }
}
```

## 信号系统集成

### 1. SignalBuilder 流畅 API

使用 GFramework 的 SignalBuilder 进行类型安全的信号连接：

```csharp
[ContextAware]
[Log]
public partial class SignalController : Node, IController
{
    private Button _button;
    private Timer _timer;
    private ProgressBar _progressBar;
    
    public override void _Ready()
    {
        InitializeComponents();
        SetupSignalConnections();
    }
    
    private void InitializeComponents()
    {
        _button = GetNode<Button>("Button");
        _timer = GetNode<Timer>("Timer");
        _progressBar = GetNode<ProgressBar>("ProgressBar");
    }
    
    private void SetupSignalConnections()
    {
        // 单个信号连接
        this.CreateSignalBuilder(_button.SignalName.Pressed)
            .Connect(OnButtonPressed)
            .UnRegisterWhenNodeExitTree(this);
            
        // 带参数的信号连接
        this.CreateSignalBuilder(_timer.SignalName.Timeout)
            .Connect(OnTimerTimeout)
            .UnRegisterWhenNodeExitTree(this);
            
        // 多信号连接
        this.CreateSignalBuilder()
            .AddSignal(_button.SignalName.Pressed, OnButtonPressed)
            .AddSignal(_timer.SignalName.Timeout, OnTimerTimeout)
            .AddSignal(_progressBar.SignalName.ValueChanged, OnProgressChanged)
            .UnRegisterWhenNodeExitTree(this);
            
        // 带标志的信号连接
        this.CreateSignalBuilder(_timer.SignalName.Timeout)
            .WithFlags(ConnectFlags.OneShot) // 单次触发
            .CallImmediately() // 立即调用一次
            .Connect(OnTimerTimeout)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnButtonPressed()
    {
        Logger.Info("Button pressed");
        
        // 启动计时器
        _timer.Start();
        
        // 发送框架事件
        this.SendEvent(new ButtonClickEvent { ButtonId = "main_button" });
    }
    
    private void OnTimerTimeout()
    {
        Logger.Info("Timer timeout");
        
        // 更新进度条
        _progressBar.Value += 10;
        
        // 发送框架事件
        this.SendEvent(new TimerTimeoutEvent());
    }
    
    private void OnProgressChanged(double value)
    {
        Logger.Debug($"Progress changed: {value}");
        
        // 发送框架事件
        this.SendEvent(new ProgressChangeEvent { Value = value });
    }
}
```

### 2. 信号与框架事件桥接

实现 Godot 信号与 GFramework 事件系统的双向桥接：

```csharp
[ContextAware]
[Log]
public partial class SignalEventBridge : Node, IController
{
    private Button _uiButton;
    private HealthBar _healthBar;
    
    public override void _Ready()
    {
        InitializeComponents();
        SetupSignalToEventBridge();
        SetupEventToSignalBridge();
    }
    
    private void InitializeComponents()
    {
        _uiButton = GetNode<Button>("UI/Button");
        _healthBar = GetNode<HealthBar>("UI/HealthBar");
    }
    
    private void SetupSignalToEventBridge()
    {
        // Godot 信号 -> 框架事件
        this.CreateSignalBuilder(_uiButton.SignalName.Pressed)
            .Connect(() => {
                this.SendEvent(new UIActionEvent { 
                    Action = "button_click", 
                    Source = "main_button" 
                });
            })
            .UnRegisterWhenNodeExitTree(this);
            
        this.CreateSignalBuilder(_healthBar.SignalName.HealthDepleted)
            .Connect(() => {
                this.SendEvent(new PlayerDeathEvent { Source = "health_system" });
            })
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void SetupEventToSignalBridge()
    {
        // 框架事件 -> Godot 信号
        this.RegisterEvent<PlayerHealthChangeEvent>(OnPlayerHealthChange)
            .UnRegisterWhenNodeExitTree(this);
            
        this.RegisterEvent<GamePauseEvent>(OnGamePause)
            .UnRegisterWhenNodeExitTree(this);
            
        this.RegisterEvent<ScoreUpdateEvent>(OnScoreUpdate)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnPlayerHealthChange(PlayerHealthChangeEvent e)
    {
        // 更新 Godot UI 组件
        _healthBar.SetValue(e.NewHealth, e.MaxHealth);
        
        // 发送 Godot 信号
        EmitSignal(SignalName.PlayerHealthChanged, e.NewHealth, e.MaxHealth);
    }
    
    private void OnGamePause(GamePauseEvent e)
    {
        // 更新 UI 状态
        GetTree().Paused = true;
        
        // 发送 Godot 信号
        EmitSignal(SignalName.GameStateChanged, "paused");
    }
    
    private void OnScoreUpdate(ScoreUpdateEvent e)
    {
        // 更新分数显示
        var scoreLabel = GetNode<Label>("UI/ScoreLabel");
        scoreLabel.Text = $"Score: {e.Score}";
        
        // 发送 Godot 信号
        EmitSignal(SignalName.ScoreChanged, e.Score);
    }
    
    // 定义 Godot 信号
    [Signal]
    public delegate void PlayerHealthChangedEventHandler(int newHealth, int maxHealth);
    
    [Signal]
    public delegate void GameStateChangedEventHandler(string newState);
    
    [Signal]
    public delegate void ScoreChangedEventHandler(int score);
}
```

## 资源管理优化

### 1. 智能资源加载

实现基于优先级的资源加载策略：

```csharp
[ContextAware]
[Log]
public partial class SmartResourceLoader : Node, IController
{
    private ResourceLoadUtility _resourceLoader;
    private Dictionary<string, Resource> _resourceCache = new();
    private Queue<ResourceLoadRequest> _loadQueue = new();
    private bool _isLoading = false;
    
    public override void _Ready()
    {
        _resourceLoader = new ResourceLoadUtility();
        PreloadEssentialResources();
    }
    
    private void PreloadEssentialResources()
    {
        Logger.Info("Preloading essential resources");
        
        var essentialResources = new[]
        {
            "res://assets/textures/player.png",
            "res://assets/textures/enemy.png",
            "res://assets/audio/shoot.wav",
            "res://assets/scenes/player.tscn"
        };
        
        foreach (var path in essentialResources)
        {
            LoadResourceAsync(path, ResourcePriority.Essential);
        }
    }
    
    public void LoadResourceAsync(string path, ResourcePriority priority = ResourcePriority.Normal)
    {
        if (_resourceCache.ContainsKey(path))
        {
            Logger.Debug($"Resource already loaded: {path}");
            return;
        }
        
        var request = new ResourceLoadRequest
        {
            Path = path,
            Priority = priority,
            RequestTime = DateTime.UtcNow
        };
        
        _loadQueue.Enqueue(request);
        ProcessLoadQueue();
    }
    
    private async void ProcessLoadQueue()
    {
        if (_isLoading || _loadQueue.Count == 0) return;
        
        _isLoading = true;
        
        while (_loadQueue.Count > 0)
        {
            var request = _loadQueue.Dequeue();
            
            try
            {
                Logger.Debug($"Loading resource: {request.Path}");
                
                // 使用 Godot 的异步资源加载
                var resource = await LoadResourceAsync(request.Path);
                
                if (resource != null)
                {
                    _resourceCache[request.Path] = resource;
                    Logger.Info($"Resource loaded: {request.Path}");
                    
                    // 发送资源加载完成事件
                    this.SendEvent(new ResourceLoadedEvent { 
                        Path = request.Path, 
                        Resource = resource 
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load resource {request.Path}: {ex.Message}");
                this.SendEvent(new ResourceLoadFailedEvent { 
                    Path = request.Path, 
                    Error = ex.Message 
                });
            }
        }
        
        _isLoading = false;
    }
    
    private async Task<Resource> LoadResourceAsync(string path)
    {
        // 使用 GD.LoadAsync 进行异步加载
        var result = GD.LoadAsync(path);
        
        // 等待加载完成
        while (!result.IsCompleted)
        {
            await Task.Delay(10);
        }
        
        return result.Result;
    }
    
    public T GetResource<T>(string path) where T : Resource
    {
        if (_resourceCache.TryGetValue(path, out var resource))
        {
            return resource as T;
        }
        
        // 同步加载资源（作为后备）
        return GD.Load<T>(path);
    }
    
    public void PreloadLevelResources(string levelName)
    {
        Logger.Info($"Preloading level resources: {levelName}");
        
        var levelConfig = GD.Load<LevelConfig>($"res://assets/levels/{levelName}.json");
        
        foreach (var assetPath in levelConfig.AssetPaths)
        {
            LoadResourceAsync(assetPath, ResourcePriority.Level);
        }
    }
    
    public void UnloadUnusedResources()
    {
        Logger.Info("Unloading unused resources");
        
        var unusedResources = new List<string>();
        
        foreach (var kvp in _resourceCache)
        {
            if (kvp.Value.GetReferenceCount() <= 1) // 只有缓存引用
            {
                unusedResources.Add(kvp.Key);
            }
        }
        
        foreach (var path in unusedResources)
        {
            _resourceCache.Remove(path);
            Logger.Debug($"Unloaded resource: {path}");
        }
    }
}

public enum ResourcePriority
{
    Essential,   // 必需资源，立即加载
    High,       // 高优先级
    Normal,     // 普通优先级
    Low,        // 低优先级
    Background  // 后台加载
}

public class ResourceLoadRequest
{
    public string Path { get; set; }
    public ResourcePriority Priority { get; set; }
    public DateTime RequestTime { get; set; }
}
```

### 2. 资源预加载策略

实现基于场景的资源预加载：

```csharp
[ContextAware]
[Log]
public partial class SceneResourcePreloader : Node, IController
{
    private Dictionary<string, SceneResourceSet> _sceneResources = new();
    
    public override void _Ready()
    {
        InitializeSceneResources();
    }
    
    private void InitializeSceneResources()
    {
        // 定义各场景需要的资源
        _sceneResources["MainMenu"] = new SceneResourceSet
        {
            ScenePath = "res://assets/scenes/MainMenu.tscn",
            RequiredAssets = new[]
            {
                "res://assets/textures/title.png",
                "res://assets/audio/menu_music.ogg",
                "res://assets/fonts/main_font.ttf"
            }
        };
        
        _sceneResources["GameLevel"] = new SceneResourceSet
        {
            ScenePath = "res://assets/scenes/GameLevel.tscn",
            RequiredAssets = new[]
            {
                "res://assets/textures/player.png",
                "res://assets/textures/enemy.png",
                "res://assets/textures/bullet.png",
                "res://assets/audio/shoot.wav",
                "res://assets/audio/explosion.wav",
                "res://assets/audio/background_music.ogg"
            }
        };
    }
    
    public void PreloadSceneResources(string sceneName)
    {
        if (!_sceneResources.TryGetValue(sceneName, out var resourceSet))
        {
            Logger.Warning($"Unknown scene: {sceneName}");
            return;
        }
        
        Logger.Info($"Preloading resources for scene: {sceneName}");
        
        foreach (var assetPath in resourceSet.RequiredAssets)
        {
            this.SendEvent(new ResourceLoadRequestEvent { 
                Path = assetPath, 
                Priority = ResourcePriority.High 
            });
        }
    }
    
    public async Task<Control> LoadSceneAsync(string sceneName)
    {
        if (!_sceneResources.TryGetValue(sceneName, out var resourceSet))
        {
            throw new ArgumentException($"Unknown scene: {sceneName}");
        }
        
        Logger.Info($"Loading scene: {sceneName}");
        
        // 预加载场景资源
        PreloadSceneResources(sceneName);
        
        // 等待资源加载完成
        await WaitForResources(resourceSet.RequiredAssets);
        
        // 加载场景
        var scene = GD.Load<PackedScene>(resourceSet.ScenePath);
        return scene.Instantiate<Control>();
    }
    
    private async Task WaitForResources(string[] assetPaths)
    {
        // 这里应该与资源加载系统集成
        // 简化实现，等待固定时间
        await Task.Delay(1000);
    }
}

public class SceneResourceSet
{
    public string ScenePath { get; set; }
    public string[] RequiredAssets { get; set; }
}
```

## 性能优化技巧

### 1. 节点性能优化

```csharp
[ContextAware]
[Log]
public partial class PerformanceOptimizedController : Node, IController
{
    private Timer _updateTimer;
    private bool _isPerformanceCritical = false;
    private float _updateInterval = 0.016f; // 60 FPS
    
    public override void _Ready()
    {
        SetupPerformanceOptimization();
    }
    
    private void SetupPerformanceOptimization()
    {
        // 使用计时器控制更新频率
        _updateTimer = new Timer();
        _updateTimer.WaitTime = _updateInterval;
        _updateTimer.Timeout += OnTimedUpdate;
        AddChild(_updateTimer);
        _updateTimer.Start();
        
        // 监听性能事件
        this.RegisterEvent<PerformanceModeChangeEvent>(OnPerformanceModeChange)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnPerformanceModeChange(PerformanceModeChangeEvent e)
    {
        _isPerformanceCritical = e.IsCritical;
        
        // 根据性能模式调整更新频率
        _updateInterval = _isPerformanceCritical ? 0.033f : 0.016f; // 30 FPS vs 60 FPS
        _updateTimer.WaitTime = _updateInterval;
    }
    
    private void OnTimedUpdate()
    {
        if (!_isPerformanceCritical)
        {
            // 正常更新所有功能
            UpdateFull();
        }
        else
        {
            // 性能关键时只更新核心功能
            UpdateCriticalOnly();
        }
    }
    
    private void UpdateFull()
    {
        // 完整更新逻辑
        UpdatePlayer();
        UpdateEnemies();
        UpdateUI();
        UpdateParticles();
        UpdateAudio();
    }
    
    private void UpdateCriticalOnly()
    {
        // 只更新关键功能
        UpdatePlayer();
        UpdateBasicUI();
    }
    
    public override void _Process(double delta)
    {
        // 禁用默认的 _Process，使用计时器控制
        // 这可以避免不必要的更新调用
    }
}
```

### 2. 内存管理优化

```csharp
[ContextAware]
[Log]
public partial class MemoryOptimizedController : Node, IController
{
    private ObjectPool<Effect> _effectPool;
    private Dictionary<string, Texture2D> _textureAtlas;
    private int _frameCount = 0;
    
    public override void _Ready()
    {
        InitializeMemoryOptimization();
    }
    
    private void InitializeMemoryOptimization()
    {
        // 创建对象池
        _effectPool = new ObjectPool<Effect>(
            createFunc: () => new Effect(),
            actionOnGet: effect => effect.Reset(),
            actionOnRelease: effect => effect.Cleanup(),
            collectionCheck: true
        );
        
        // 创建纹理图集
        CreateTextureAtlas();
        
        // 设置定期清理
        var cleanupTimer = new Timer();
        cleanupTimer.WaitTime = 60.0f; // 每分钟清理一次
        cleanupTimer.Timeout += PerformMemoryCleanup;
        AddChild(cleanupTimer);
        cleanupTimer.Start();
    }
    
    private void CreateTextureAtlas()
    {
        _textureAtlas = new Dictionary<string, Texture2D>();
        
        // 预加载常用纹理到图集
        var textures = new[]
        {
            "player", "enemy", "bullet", "explosion", "powerup"
        };
        
        foreach (var textureName in textures)
        {
            var texture = GD.Load<Texture2D>($"res://assets/textures/{textureName}.png");
            _textureAtlas[textureName] = texture;
        }
    }
    
    public void SpawnEffect(Vector2 position, string effectType)
    {
        var effect = _effectPool.Get();
        effect.Initialize(position, effectType, _textureAtlas[effectType]);
        AddChild(effect);
        
        // 自动回收效果
        effect.StartLifetime(2.0f, () => {
            RemoveChild(effect);
            _effectPool.Release(effect);
        });
    }
    
    private void PerformMemoryCleanup()
    {
        Logger.Info("Performing memory cleanup");
        
        // 清理未使用的资源
        ResourceLoader.UnloadUnused();
        
        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // 记录内存使用情况
        var memoryUsage = GC.GetTotalMemory(false);
        Logger.Debug($"Memory usage after cleanup: {memoryUsage / 1024 / 1024} MB");
    }
    
    public override void _Process(double delta)
    {
        _frameCount++;
        
        // 每100帧检查一次内存使用
        if (_frameCount % 100 == 0)
        {
            CheckMemoryUsage();
        }
    }
    
    private void CheckMemoryUsage()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var threshold = 100 * 1024 * 1024; // 100 MB
        
        if (memoryUsage > threshold)
        {
            Logger.Warning($"High memory usage detected: {memoryUsage / 1024 / 1024} MB");
            
            // 触发内存清理
            this.SendEvent(new HighMemoryUsageEvent { 
                CurrentUsage = memoryUsage,
                Threshold = threshold 
            });
        }
    }
}

public class Effect : Node2D
{
    private Sprite2D _sprite;
    private float _lifetime;
    private Action _onComplete;
    
    public void Initialize(Vector2 position, string type, Texture2D texture)
    {
        Position = position;
        
        _sprite = new Sprite2D();
        _sprite.Texture = texture;
        AddChild(_sprite);
    }
    
    public void StartLifetime(float duration, Action onComplete)
    {
        _lifetime = duration;
        _onComplete = onComplete;
    }
    
    public override void _Process(double delta)
    {
        _lifetime -= (float)delta;
        
        if (_lifetime <= 0)
        {
            _onComplete?.Invoke();
        }
    }
    
    public void Reset()
    {
        Visible = true;
        Modulate = Colors.White;
        _lifetime = 0;
        _onComplete = null;
    }
    
    public void Cleanup()
    {
        _sprite?.QueueFree();
    }
}
```

## 常见集成模式

### 1. 单例模式集成

```csharp
[ContextAware]
[Log]
public partial class GameManager : Node, IController
{
    private static GameManager _instance;
    
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("GameManager not initialized");
            }
            return _instance;
        }
    }
    
    public override void _Ready()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        
        _instance = this;
        
        // 设置为不会被自动删除
        ProcessMode = ProcessModeEnum.Always;
        
        Logger.Info("GameManager initialized as singleton");
    }
    
    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
```

### 2. 状态机集成

```csharp
[ContextAware]
[Log]
public partial class StateMachineController : Node, IController
{
    private IGameState _currentState;
    private Dictionary<Type, IGameState> _states = new();
    
    public override void _Ready()
    {
        InitializeStates();
        ChangeState<MenuState>();
    }
    
    private void InitializeStates()
    {
        _states[typeof(MenuState)] = new MenuState();
        _states[typeof(PlayingState)] = new PlayingState();
        _states[typeof(PausedState)] = new PausedState();
        _states[typeof(GameOverState)] = new GameOverState();
    }
    
    public void ChangeState<T>() where T : IGameState
    {
        var newState = _states[typeof(T)];
        
        if (newState == _currentState) return;
        
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
        
        Logger.Info($"State changed to: {typeof(T).Name}");
    }
    
    public override void _Process(double delta)
    {
        _currentState?.Update(this, delta);
    }
    
    public override void _Input(InputEvent @event)
    {
        _currentState?.HandleInput(this, @event);
    }
}

public interface IGameState
{
    void Enter(StateMachineController controller);
    void Update(StateMachineController controller, double delta);
    void HandleInput(StateMachineController controller, InputEvent @event);
    void Exit(StateMachineController controller);
}

public class MenuState : IGameState
{
    public void Enter(StateMachineController controller)
    {
        // 显示主菜单
        controller.GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, 
            "res://assets/scenes/MainMenu.tscn");
    }
    
    public void Update(StateMachineController controller, double delta) { }
    
    public void HandleInput(StateMachineController controller, InputEvent @event)
    {
        if (@event.IsActionPressed("start_game"))
        {
            controller.ChangeState<PlayingState>();
        }
    }
    
    public void Exit(StateMachineController controller) { }
}

public class PlayingState : IGameState
{
    public void Enter(StateMachineController controller)
    {
        controller.GetTree().Paused = false;
        controller.this.SendEvent(new GameStartEvent());
    }
    
    public void Update(StateMachineController controller, double delta) { }
    
    public void HandleInput(StateMachineController controller, InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            controller.ChangeState<PausedState>();
        }
    }
    
    public void Exit(StateMachineController controller)
    {
        controller.this.SendEvent(new GamePauseEvent());
    }
}
```

## 调试与测试

### 1. Godot 集成测试

```csharp
using NUnit.Framework;
using Godot;
using GFramework.Godot.Extensions;

[TestFixture]
public class GodotIntegrationTests
{
    private SceneTree _sceneTree;
    private TestScene _testScene;
    
    [SetUp]
    public void Setup()
    {
        // 创建测试场景树
        _sceneTree = new SceneTree();
        _sceneTree.Root = new Node();
        
        // 创建测试场景
        _testScene = new TestScene();
        _sceneTree.Root.AddChild(_testScene);
    }
    
    [TearDown]
    public void TearDown()
    {
        _testScene?.QueueFree();
        _sceneTree?.Quit();
    }
    
    [Test]
    public void Controller_WithGFrameworkIntegration_ShouldWorkCorrectly()
    {
        // Act
        _testScene._Ready();
        _sceneTree.ProcessFrame();
        
        // Assert
        Assert.That(_testScene.Context, Is.Not.Null);
        Assert.That(_testScene.IsInitialized, Is.True);
    }
    
    [Test]
    public void Signal_Builder_ShouldConnectCorrectly()
    {
        // Arrange
        _testScene._Ready();
        var button = _testScene.GetNode<Button>("TestButton");
        
        // Act
        button.EmitSignal(Button.SignalName.Pressed);
        _sceneTree.ProcessFrame();
        
        // Assert
        Assert.That(_testScene.ButtonPressedCount, Is.EqualTo(1));
    }
}

[ContextAware]
[Log]
public partial class TestScene : Node2D
{
    public bool IsInitialized { get; private set; }
    public int ButtonPressedCount { get; private set; }
    
    public override void _Ready()
    {
        SetContext(new TestContext());
        
        var button = new Button();
        button.Name = "TestButton";
        AddChild(button);
        
        this.CreateSignalBuilder(button.SignalName.Pressed)
            .Connect(() => ButtonPressedCount++)
            .UnRegisterWhenNodeExitTree(this);
            
        IsInitialized = true;
    }
}

public class TestContext : IContext
{
    // 简化的测试上下文实现
}
```

### 2. 性能基准测试

```csharp
[TestFixture]
public class GodotPerformanceTests
{
    [Test]
    public void NodeExtensions_Performance_Comparison()
    {
        var node = new Node();
        var iterations = 10000;
        
        // 测试扩展方法性能
        var stopwatch1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var child = node.GetNodeX<Node>("NonExistent");
        }
        stopwatch1.Stop();
        
        // 测试原始方法性能
        var stopwatch2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                var child = node.GetNode<Node>("NonExistent");
            }
            catch
            {
                // 忽略异常
            }
        }
        stopwatch2.Stop();
        
        GD.Print($"Extension method: {stopwatch1.ElapsedMilliseconds}ms");
        GD.Print($"Original method: {stopwatch2.ElapsedMilliseconds}ms");
        
        // 扩展方法应该更快或相等
        Assert.That(stopwatch1.ElapsedMilliseconds, Is.LessThanOrEqualTo(stopwatch2.ElapsedMilliseconds));
    }
}
```

---

## 总结

通过本教程，你已经学会了：

- ✅ **深度集成** - GFramework 与 Godot 的完美结合
- ✅ **生命周期管理** - 自动化的节点生命周期控制
- ✅ **信号系统** - 类型安全的信号连接和桥接
- ✅ **性能优化** - 内存管理和运行时优化技巧
- ✅ **调试测试** - 完整的测试和调试方案

这些技术将帮助你创建高性能、可维护的 Godot 游戏。

---

**教程版本**: 1.0.0  
**更新日期**: 2026-01-12
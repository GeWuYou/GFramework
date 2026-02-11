# GFramework.Godot API 参考

> GFramework.Godot 模块的完整 API 参考文档，包含 Godot 特定扩展和集成的详细说明。

## 📋 目录

- [架构集成](#架构集成)
- [Node 扩展方法](#node-扩展方法)
- [信号系统](#信号系统)
- [节点池化](#节点池化)
- [资源管理](#资源管理)
- [日志系统](#日志系统)
- [池化管理](#池化管理)
- [音频系统](#音频系统)

## 架构集成

### AbstractArchitecture

Godot 特定的架构基类，继承自 Core.Architecture。

#### 新增方法

```csharp
// Godot 模块安装
protected void InstallGodotModule(IGodotModule module);

protected void InstallGodotModule<T>() where T : IGodotModule, new();
```

#### 使用示例

```csharp
public class GameArchitecture : AbstractArchitecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
        RegisterUtility(new StorageUtility());
    }
    
    protected override void InstallModules()
    {
        InstallGodotModule(new AudioModule());
        InstallGodotModule(new InputModule());
    }
}
```

### IGodotModule

Godot 模块接口，继承自 IArchitectureModule。

#### 属性

```csharp
Node Node { get; }
```

#### 方法

```csharp
void Install(IArchitecture architecture);
void OnAttach(Architecture architecture);
void OnDetach(Architecture architecture);
void OnPhase(ArchitecturePhase phase, IArchitecture architecture);
```

#### 使用示例

```csharp
public class AudioModule : AbstractGodotModule
{
    // 模块节点本身
    public override Node Node => this;
    
    private AudioStreamPlayer _musicPlayer;
    
    public override void Install(IArchitecture architecture)
    {
        // 注册音频系统
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
    
    public override void OnAttach(Architecture architecture)
    {
        // 创建音频播放器
        _musicPlayer = new AudioStreamPlayer();
        AddChild(_musicPlayer);
        
        Logger.Info("Audio module attached");
    }
    
    public override void OnDetach(Architecture architecture)
    {
        // 清理音频播放器
        _musicPlayer?.QueueFree();
        
        Logger.Info("Audio module detached");
    }
    
    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                PlayBackgroundMusic();
                break;
        }
    }
    
    private void PlayBackgroundMusic()
    {
        var music = GD.Load<AudioStream>("res://audio/background.ogg");
        _musicPlayer.Stream = music;
        _musicPlayer.Play();
    }
}
```

### AbstractGodotModule

Godot 模块抽象基类，实现了 IGodotModule 接口。

#### 使用示例

```csharp
[ContextAware]
[Log]
public partial class SaveModule : AbstractGodotModule
{
    private Timer _autoSaveTimer;
    
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new SaveSystem());
        architecture.RegisterUtility(new SaveUtility());
    }
    
    public override void OnAttach(Architecture architecture)
    {
        // 创建自动保存计时器
        _autoSaveTimer = new Timer();
        _autoSaveTimer.WaitTime = 300; // 5分钟
        _autoSaveTimer.Timeout += OnAutoSave;
        AddChild(_autoSaveTimer);
        _autoSaveTimer.Start();
        
        Logger.Info("Save module attached with auto-save enabled");
    }
    
    private void OnAutoSave()
    {
        var saveSystem = Context.GetSystem<SaveSystem>();
        saveSystem.SaveGame("autosave");
        Logger.Debug("Auto-save completed");
    }
}
```

## Node 扩展方法

### 安全节点获取

#### GetNodeX<T>()

安全获取子节点，自动类型转换和 null 检查。

```csharp
public static T GetNodeX<T>(this Node node, string path) where T : class;
```

#### 使用示例

```csharp
// 安全获取节点
var player = GetNodeX<Player>("Player");
var healthBar = GetNodeX<ProgressBar>("UI/HealthBar");

// 如果节点不存在或类型不匹配，会抛出异常
// 使用时不需要额外的 null 检查
```

#### GetChildX<T>()

安全查找子节点，支持递归查找。

```csharp
public static T GetChildX<T>(this Node node, string path) where T : class;
```

#### 使用示例

```csharp
// 递归查找子节点
var player = FindChildX<Player>("Player");
var sprite = FindChildX<Sprite2D>("Enemy/Sprite");
```

### 节点验证

#### IsValidNode()

检查节点是否有效且在场景树中。

```csharp
public static bool IsValidNode(this Node node);
```

#### IsInvalidNode()

检查节点是否无效或不在场景树中。

```csharp
public static bool IsInvalidNode(this Node node);
```

#### 使用示例

```csharp
Node someNode = GetNode("SomeNode");

if (IsValidNode(someNode))
{
    someNode.DoSomething();
}

if (IsInvalidNode(someNode))
{
    // 节点无效，需要重新获取
}
```

### 安全节点操作

#### AddChildX()

安全添加子节点，包含验证。

```csharp
public static void AddChildX(this Node parent, Node child, bool forceReadableName = false, InternalMode internalMode = 0);
```

#### QueueFreeX()

安全销毁节点，包含验证。

```csharp
public static void QueueFreeX(this Node node);
```

#### FreeX()

立即销毁节点，谨慎使用。

```csharp
public static void FreeX(this Node node);
```

#### 使用示例

```csharp
// 创建并添加子节点
var bullet = bulletScene.Instantiate<Bullet>();
AddChildX(bullet);

// 安全销毁节点
bullet.QueueFreeX();

// 立即销毁（谨慎使用）
tempNode.FreeX();
```

### 异步节点操作

#### WaitUntilReady()

等待节点准备就绪。

```csharp
public static async Task WaitUntilReady(this Node node);
```

#### WaitUntil()

等待条件满足。

```csharp
public static async Task WaitUntil(this Node node, Func<bool> condition);
```

#### WaitUntilTimeout()

等待指定时间或条件满足。

```csharp
public static async Task WaitUntilTimeout(this Node node, float timeoutSeconds);
```

#### 使用示例

```csharp
// 等待节点准备就绪
await this.WaitUntilReady();

// 等待条件满足
await this.WaitUntil(() => SomeCondition());

// 等待 2 秒
await this.WaitUntilTimeout(2.0f);
```

### 场景树操作

#### GetParentX<T>()

安全获取父节点。

```csharp
public static T GetParentX<T>(this Node node) where T : class;
```

#### ForEachChild()

遍历所有子节点。

```csharp
public static void ForEachChild<T>(this Node node, Action<T> action) where T : Node;
```

#### 使用示例

```csharp
// 获取父节点
var parent = GetParentX<GameLevel>();

// 遍历所有子节点
this.ForEachChild<Node>(child => {
    if (child is Sprite2D sprite)
    {
        ProcessSprite(sprite);
    }
});
```

## 信号系统

### SignalBuilder

信号构建器，提供流畅的信号连接 API。

#### 构造函数

```csharp
public SignalBuilder(Node node);
```

#### 连接方法

```csharp
public SignalBuilder Connect(Callable callable);
public SignalBuilder Connect<T1>(Callable<T1> callable);
public SignalBuilder Connect<T1, T2>(Callable<T1, T2> callable);
public SignalBuilder Connect<T1, T2, T3>(Callable<T1, T2, T3> callable);
public SignalBuilder Connect<T1, T2, T3, T4>(Callable<T1, T2, T3, T4> callable);
```

#### 配置方法

```csharp
public SignalBuilder WithFlags(ConnectFlags flags);
public SignalBuilder CallImmediately();
```

#### 生命周期方法

```csharp
public SignalBuilder UnRegisterWhenNodeExitTree(Node node);
public SignalBuilder AddToUnregisterList(IUnRegisterList unregisterList);
```

#### 使用示例

```csharp
// 基础连接
this.CreateSignalBuilder(Button.SignalName.Pressed)
    .Connect(OnButtonPressed)
    .UnRegisterWhenNodeExitTree(this);

// 带标志的连接
this.CreateSignalBuilder(Timer.SignalName.Timeout)
    .WithFlags(ConnectFlags.OneShot)
    .Connect(OnTimerTimeout);

// 立即调用连接
this.CreateSignalBuilder(CustomSignal.SignalName.CustomEvent)
    .CallImmediately()
    .Connect(OnCustomEvent);

// 多参数连接
this.CreateSignalBuilder()
    .AddSignal(SignalName.Parameter1, OnParameter1)
    .AddSignal(SignalName.Parameter2, OnParameter2)
    .AddSignal(SignalName.Parameter3, OnParameter3)
    .UnRegisterWhenNodeExitTree(this);
```

### SignalExtension

信号扩展方法，简化信号创建。

#### CreateSignalBuilder()

创建信号构建器。

```csharp
public static SignalBuilder CreateSignalBuilder(this Node node, string signalName);
```

#### ConnectSignal()

直接连接信号。

```csharp
public static IUnRegister ConnectSignal(this Node node, string signalName, Callable callable);
public static IUnRegister ConnectSignal<T1>(this Node node, string signalName, Callable<T1> callable);
```

#### 使用示例

```csharp
// 创建信号构建器连接
this.CreateSignalBuilder(Button.SignalName.Pressed)
    .Connect(OnButtonPressed);

// 直接连接信号
this.ConnectSignal(Button.SignalName.Pressed, Callable.From(OnButtonPressed));

// 多参数信号连接
this.ConnectSignal(ComplexSignal.SignalName.DataChanged, 
    Callable.From(OnDataChanged));
```

### 信号与框架事件桥接

#### SignalEventBridge

信号事件桥接器，将 Godot 信号转换为框架事件。

#### 使用示例

```csharp
[ContextAware]
[Log]
public partial class GameController : Node, IController
{
    public override void _Ready()
    {
        // Godot 信号 -> 框架事件
        this.CreateSignalBuilder(Button.SignalName.Pressed)
            .Connect(() => {
                Context.SendEvent(new ButtonClickEvent { ButtonId = "start" });
            })
            .UnRegisterWhenNodeExitTree(this);
            
        // 框架事件 -> Godot 信号
        this.RegisterEvent<PlayerHealthChangeEvent>(OnPlayerHealthChange)
            .UnRegisterWhenNodeExitTree(this);
    }
    
    private void OnPlayerHealthChange(PlayerHealthChangeEvent e)
    {
        // 更新 Godot UI
        var healthBar = GetNode<ProgressBar>("UI/HealthBar");
        healthBar.Value = (float)e.NewHealth / e.MaxHealth * 100;
        
        // 发送 Godot 信号
        EmitSignal(SignalName.HealthUpdated, e.NewHealth, e.MaxHealth);
    }
    
    [Signal]
    public delegate void HealthUpdatedEventHandler(int newHealth, int maxHealth);
}
```

## 节点池化

### AbstractNodePoolSystem<TKey, TNode>

节点池化系统基类，TNode 必须是 Node。

#### 抽象方法

```csharp
protected abstract TNode CreateItem(TKey key);
protected abstract void OnSpawn(TNode item, TKey key);
protected abstract void OnDespawn(TNode item);
protected abstract bool CanDespawn(TNode item);
```

#### 公共方法

```csharp
public TNode Spawn(TKey key);
public void Despawn(TNode item);
public void DespawnAll();
public int GetActiveCount(TKey key);
public int GetTotalCount(TKey key);
```

#### 使用示例

```csharp
public class BulletPoolSystem : AbstractNodePoolSystem<string, Bullet>
{
    private readonly Dictionary<string, PackedScene> _scenes = new();
    
    public BulletPoolSystem()
    {
        _scenes["player"] = GD.Load<PackedScene>("res://scenes/PlayerBullet.tscn");
        _scenes["enemy"] = GD.Load<PackedScene>("res://scenes/EnemyBullet.tscn");
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
        item.Reset();
        item.Position = Vector2.Zero;
        item.Visible = true;
        item.SetCollisionLayerValue(1, true);
        item.SetCollisionMaskValue(1, true);
    }
    
    protected override void OnDespawn(Bullet item)
    {
        item.Visible = false;
        item.SetCollisionLayerValue(1, false);
        item.SetCollisionMaskValue(1, false);
        
        // 移除父节点
        item.GetParent()?.RemoveChild(item);
    }
    
    protected override bool CanDespawn(Bullet item)
    {
        return !item.IsActive && item.GetParent() != null;
    }
}

// 使用示例
var bulletPool = new BulletPoolSystem();

// 从池中获取子弹
var bullet = bulletPool.Spawn("player");
AddChild(bullet);

// 回收子弹
bulletPool.Despawn(bullet);
```

### IPoolableNode

可池化节点接口。

```csharp
public interface IPoolableNode
{
    void Reset();
    void SetActive(bool active);
    bool IsActive { get; }
}
```

#### 使用示例

```csharp
public partial class Bullet : Area2D, IPoolableNode
{
    [Export] public float Speed { get; set; } = 500.0f;
    [Export] public float Damage { get; set; } = 10.0f;
    
    private bool _isActive;
    
    public bool IsActive => _isActive;
    
    public void Reset()
    {
        Position = Vector2.Zero;
        Rotation = 0;
        Velocity = Vector2.Zero;
        _isActive = false;
    }
    
    public void SetActive(bool active)
    {
        _isActive = active;
        Visible = active;
        SetProcess(active);
    }
    
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }
    
    private void OnBodyEntered(Node body)
    {
        if (body is Enemy enemy && _isActive)
        {
            enemy.TakeDamage(Damage);
            // 子弹命中敌人，回收到池中
            var bulletPool = GetNode<BulletPoolSystem>("/root/BulletPool");
            bulletPool?.Despawn(this);
        }
    }
    
    public override void _Process(double delta)
    {
        if (!_isActive) return;
        
        Position += Transform.X * (float)(Speed * delta);
    }
}
```

## 资源管理

### ResourceLoadUtility

资源加载工具类，简化和缓存 Godot 资源加载。

#### 构造函数

```csharp
public ResourceLoadUtility();
```

#### 方法

```csharp
public T LoadResource<T>(string path) where T : Resource;
public async Task<T> LoadResourceAsync<T>(string path) where T : Resource;
public void PreloadResource<T>(string path) where T : Resource;
public bool HasResource<T>(string path) where T : Resource;
```

#### 使用示例

```csharp
var resourceLoader = new ResourceLoadUtility();

// 同步加载资源
var playerTexture = resourceLoader.LoadResource<Texture2D>("res://textures/player.png");
var playerScene = resourceLoader.LoadResource<PackedScene>("res://scenes/Player.tscn");

// 异步加载资源
var musicStream = await resourceLoader.LoadResourceAsync<AudioStream>("res://audio/background.ogg");

// 预加载资源
resourceLoader.PreloadResource<Texture2D>("res://textures/enemy.png");
resourceLoader.PreloadResource<PackedScene>("res://scenes/enemy.tscn");
```

### AbstractResourceFactoryUtility

抽象资源工厂工具基类。

#### 抽象方法

```csharp
protected abstract void RegisterFactories();
protected abstract T CreateFactory<T>(string path, Dictionary<string, object> metadata = null) where T : class;
```

#### 使用示例

```csharp
public class GameResourceFactory : AbstractResourceFactoryUtility
{
    protected override void RegisterFactories()
    {
        RegisterFactory<PlayerData>("res://data/players/{id}.json");
        RegisterFactory<WeaponConfig>("res://data/weapons/{id}.json");
        RegisterFactory<LevelConfig>("res://data/levels/{id}.json");
    }
    
    public PlayerData CreatePlayer(string playerId)
    {
        return CreateFactory<PlayerData>($"res://data/players/{playerId}.json");
    }
    
    public WeaponConfig CreateWeapon(string weaponId)
    {
        var metadata = new Dictionary<string, object>
        {
            ["weaponId"] = weaponId,
            ["loadTime"] = DateTime.Now
        };
        
        return CreateFactory<WeaponConfig>($"res://data/weapons/{weaponId}.json", metadata);
    }
}
```

## 日志系统

### GodotLogger

Godot 特定的日志实现。

#### 构造函数

```csharp
public GodotLogger(string categoryName);
public GodotLogger(string categoryName, LogLevel minLevel);
```

#### 方法

```csharp
public void Log<T>(LogLevel level, T message, params object[] args);
public void Debug<T>(T message, params object[] args);
public void Info<T>(T message, params object[] args);
public void Warning<T>(T message, params object[] args);
public void Error<T>(T message, params object[] args);
public void Error<T>(Exception exception, T message, params object[] args);
```

#### 使用示例

```csharp
// 创建日志器
var logger = new GodotLogger("GameController");

// 不同级别的日志
logger.Debug("Player position: {0}", player.Position);
logger.Info("Game started");
logger.Warning("Low health: {0}", player.Health);
logger.Error("Failed to load resource: {0}", resourcePath);

// 带异常的错误日志
logger.Error(exception, "An error occurred while processing player input");
```

### GodotLoggerFactory

Godot 日志工厂。

#### 方法

```csharp
public ILogger CreateLogger(string categoryName);
public ILogger CreateLogger(Type type);
```

#### 使用示例

```csharp
var factory = new GodotLoggerFactory();

// 创建日志器
var gameLogger = factory.CreateLogger("GameController");
var playerLogger = factory.CreateLogger(typeof(PlayerController));

// 在架构中使用
public class GameArchitecture : AbstractArchitecture
{
    protected override void Init()
    {
        LoggerProperties = new LoggerProperties
        {
            LoggerFactoryProvider = new GodotLoggerFactoryProvider(),
            MinLevel = LogLevel.Info
        };
        
        RegisterSystem(new GameController());
    }
}
```

## 池化管理

### AbstractObjectPool<T>

通用对象池基类。

#### 构造函数

```csharp
public AbstractObjectPool(
    Func<T> createFunc,
    Action<T> actionOnGet = null,
    Action<T> actionOnRelease = null,
    bool collectionCheck = false
);
```

#### 方法

```csharp
public T Get();
public void Release(T item);
public void Clear();
public int CountInactive { get; }
public int CountAll { get; }
```

#### 使用示例

```csharp
// 创建对象池
var explosionPool = new AbstractObjectPool<ExplosionEffect>(
    createFunc: () => new ExplosionEffect(),
    actionOnGet: effect => effect.Reset(),
    actionOnRelease: effect => Cleanup()
);

// 使用对象池
var effect = explosionPool.Get();
effect.Play(effect.Position);

// 回收对象
explosionPool.Release(effect);
```

---

## 音频系统

### AudioSystem

音频系统，管理音乐和音效播放。

#### 使用示例

```csharp
[ContextAware]
[Log]
public partial class AudioSystem : AbstractSystem
{
    private AudioStreamPlayer _musicPlayer;
    private readonly Dictionary<string, AudioStream> _soundCache = new();
    
    protected override void OnInit()
    {
        InitializeAudioPlayers();
        CacheSounds();
        
        // 监听音频事件
        this.RegisterEvent<PlaySoundEvent>(OnPlaySound);
        this.RegisterEvent<PlayMusicEvent>(OnPlayMusic);
        this.RegisterEvent<StopMusicEvent>(OnStopMusic);
        this.RegisterEvent<SetVolumeEvent>(OnSetVolume);
    }
    
    private void InitializeAudioPlayers()
    {
        _musicPlayer = new AudioStreamPlayer();
        AddChild(_musicPlayer);
        
        // 配置音乐播放器
        _musicPlayer.Bus = "Music";
    }
    
    private void CacheSounds()
    {
        var soundPaths = new[]
        {
            "res://audio/jump.wav",
            "res://audio/attack.wav",
            "res://audio/hurt.wav",
            "res://audio/victory.wav"
        };
        
        foreach (var path in soundPaths)
        {
            var sound = GD.Load<AudioStream>(path);
            var soundName = Path.GetFileNameWithoutExtension(path);
            _soundCache[soundName] = sound;
        }
    }
    
    private void OnPlaySound(PlaySoundEvent e)
    {
        if (_soundCache.TryGetValue(e.SoundName, out var sound))
        {
            PlaySound(sound);
        }
        else
        {
            Logger.Warning($"Sound not found: {e.SoundName}");
        }
    }
    
    private void PlayMusic(PlayMusicEvent e)
    {
        var music = GD.Load<AudioStream>(e.MusicPath);
        _musicPlayer.Stream = music;
        _musicPlayer.Play();
        
        Logger.Info($"Playing music: {e.MusicPath}");
    }
    
    private void OnStopMusic(StopMusicEvent e)
    {
        _musicPlayer.Stop();
        Logger.Info("Music stopped");
    }
    
    private void OnSetVolume(SetVolumeEvent e)
    {
        AudioServer.SetBusVolume(e.BusName, e.Volume);
        Logger.Info($"Set volume for bus {e.BusName}: {e.Volume}");
    }
    
    private void PlaySound(AudioStream sound)
    {
        var player = new AudioStreamPlayer();
        player.Stream = sound;
        player.Bus = "SFX";
        player.Play();
        
        // 自动清理播放器
        this.CreateSignalBuilder(player.SignalName.Finished)
            .WithFlags(ConnectFlags.OneShot)
            .Connect(() => player.QueueFree())
            .UnRegisterWhenNodeExitTree(this);
    }
}

// 音频事件
public struct PlaySoundEvent { public string SoundName; }
public struct PlayMusicEvent { public string MusicPath; }
public struct StopMusicEvent { }
public struct SetVolumeEvent { public string BusName; public float Volume; }
```

---

**文档版本**: 1.0.0  
**更新日期**: 2026-01-12
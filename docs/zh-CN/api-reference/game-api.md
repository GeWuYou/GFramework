# GFramework.Game API 参考

> GFramework.Game 模块的完整 API 参考文档，包含游戏特定功能的详细说明。

## 📋 目录

- [架构模块](#架构模块)
- [资产管理系统](#资产管理系统)
- [存储系统](#存储系统)
- [序列化系统](#序列化系统)
- [数据模型](#数据模型)
- [工具类](#工具类)

## 架构模块

### IArchitectureModule

架构模块接口，定义了模块的基本行为。

#### 方法签名

```csharp
void Install(IArchitecture architecture);
void OnAttach(Architecture architecture);
void OnDetach(Architecture architecture);
void OnPhase(ArchitecturePhase phase, IArchitecture architecture);
```

#### 使用示例

```csharp
public class AudioModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
    
    public void OnAttach(Architecture architecture)
    {
        Logger.Info("Audio module attached to architecture");
    }
    
    public void OnDetach(Architecture architecture)
    {
        Logger.Info("Audio module detached from architecture");
    }
    
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                // 架构准备就绪，可以开始播放背景音乐
                PlayBackgroundMusic();
                break;
            case ArchitecturePhase.Destroying:
                // 架构正在销毁，清理音频资源
                CleanupAudioResources();
                break;
        }
    }
}
```

### AbstractModule

抽象模块基类，实现了 IArchitectureModule 接口。

#### 构造函数

```csharp
public AbstractModule();
```

#### 可用方法

```csharp
// 发送事件
protected void SendEvent<T>(T e) where T : new();
protected void SendEvent<T>(T e);

// 获取模型
protected T GetModel<T>() where T : class, IModel;
protected T GetSystem<T>() where T : class, ISystem;
protected T GetUtility<T>() where T : class, IUtility;

// 发送命令
protected void SendCommand(ICommand command);
protected TResult SendCommand<TResult>(ICommand<TResult> command);

// 发送查询
protected TResult SendQuery<TResult>(IQuery<TResult> query);
```

#### 使用示例

```csharp
public class SaveModule : AbstractModule
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
        // 这里无法使用 AddChild，因为不是 Node
        // 在具体的模块实现中处理
    }
    
    public override void OnDetach(Architecture architecture)
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = null;
    }
    
    public override void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                Logger.Info("Save module ready");
                break;
            case ArchitecturePhase.Destroying:
                Logger.Info("Save module destroying");
                break;
        }
    }
    
    private void OnAutoSave()
    {
        var saveSystem = GetSystem<SaveSystem>();
        saveSystem.SaveAutoSave();
    }
}
```

## 资产管理系统

### IAssetCatalogUtility

资产目录工具接口，定义了资产管理的基本行为。

#### 方法签名

```csharp
// 场景资产注册
void RegisterSceneUnit(string key, string scenePath);
void RegisterScenePage(string key, string scenePath);
void RegisterAsset<T>(string key, string path) where T : Resource;
void RegisterAsset(string key, Type type, string path);

// 映射资产注册
void RegisterSceneUnit(string key, AssetMapping mapping);
void RegisterScenePage(string key, AssetMapping mapping);
void RegisterAsset(string key, AssetMapping mapping);

// 查询方法
bool HasSceneUnit(string key);
bool HasScenePage(string key);
bool HasAsset<T>(string key);
bool HasAsset(string key, Type type);

// 获取方法
T GetScene<T>(string key) where T : PackedScene;
T GetScenePage<T>(string key) where T : PackedScene;
T GetAsset<T>(string key) where T : Resource;
T GetAsset(string key, Type type);

// 元数据获取
AssetCatalogMapping GetAssetMetadata(string key);
```

#### AssetCatalogMapping

资产映射数据类。

#### 属性

```csharp
public string Key { get; set; }
public string Path { get; set; }
public Type Type { get; set; }
public Dictionary<string, object> Metadata { get; set; }
public DateTime RegisteredAt { get; set; }
```

#### 使用示例

```csharp
public class GameAssetCatalog : AbstractAssetCatalogUtility
{
    public override void Initialize()
    {
        // 注册场景资产
        RegisterSceneUnit("Player", "res://scenes/Player.tscn");
        RegisterSceneUnit("Enemy", "res://scenes/Enemy.tscn");
        RegisterScenePage("MainMenu", "res://ui/MainMenu.tscn");
        RegisterScenePage("GameUI", "res://ui/GameUI.tscn");
        
        // 注册通用资产
        RegisterAsset<Texture2D>("PlayerTexture", "res://textures/player.png");
        RegisterAsset<Texture2D>("EnemyTexture", "res://textures/enemy.png");
        RegisterAsset<AudioStream>("ShootSound", "res://audio/shoot.wav");
        RegisterAsset<AudioStream>("ExplosionSound", "res://audio/explosion.wav");
        
        // 使用元数据注册
        var playerMapping = new AssetMapping
        {
            Key = "Player",
            Path = "res://scenes/Player.tscn",
            Type = typeof(PackedScene),
            Metadata = new Dictionary<string, object>
            {
                ["category"] = "character",
                ["tags"] = new[] { "player", "hero", "controlled" },
                ["health"] = 100,
                ["speed"] = 5.0f
            }
        };
        RegisterSceneUnit("Player", playerMapping);
    }
    
    protected override bool ValidateAsset(string key, string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Asset file not found: {path}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(key))
        {
            GD.PrintErr("Asset key cannot be empty");
            return false;
        }
        
        return true;
    }
    
    protected override void OnAssetLoaded(string key, object asset)
    {
        GD.Print($"Asset loaded: {key} of type {asset.GetType().Name}");
        
        // 对特定资产进行额外处理
        if (key == "Player" && asset is PackedScene playerScene)
        {
            PreloadPlayerComponents(playerScene);
        }
    }
    
    private void PreloadPlayerComponents(PackedScene playerScene)
    {
        // 预加载玩家组件到内存
        playerScene.Instantiate(); // 实例化以加载子节点
    }
    
    // 获取资产
    public PackedScene GetPlayerScene() => GetScene<PackedScene>("Player");
    public PackedScene GetEnemyScene() => GetScene<PackedScene>("Enemy");
    public Texture2D GetPlayerTexture() => GetAsset<Texture2D>("PlayerTexture");
    public AudioStream GetShootSound() => GetAsset<AudioStream>("ShootSound");
}
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
        RegisterFactory<LevelData>("res://data/levels/{id}.json");
    }
    
    public PlayerData CreatePlayer(string playerId)
    {
        var playerPath = $"res://data/players/{playerId}.json";
        return CreateFactory<PlayerData>(playerPath);
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
    
    public LevelData CreateLevel(string levelId)
    {
        return CreateFactory<LevelData>($"res://data/levels/{levelId}.json");
    }
    
    protected override PlayerData CreateFactory<PlayerData>(string path, Dictionary<string, object> metadata)
    {
        if (!FileAccess.FileExists(path))
        {
            return new PlayerData(); // 返回默认数据
        }
        
        try
        {
            var json = FileAccess.Open(path, FileAccess.ModeFlags.Read).GetAsText();
            var data = JsonConvert.DeserializeObject<PlayerData>(json);
            
            // 如果有元数据，可以用来修改数据
            if (metadata != null)
            {
                data.LastLoadedAt = metadata.GetValueOrDefault("loadTime", DateTime.Now);
                data.LoadCount = metadata.GetValueOrDefault("loadCount", 0) + 1;
            }
            
            return data;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load player data from {path}: {ex.Message}");
            return new PlayerData();
        }
    }
}
```

## 存储系统

### IStorage

存储接口，定义了数据持久化的基本行为。

#### 方法签名

```csharp
// 读写操作
void Write<T>(string key, T data);
T Read<T>(string key, T defaultValue = default);
Task WriteAsync<T>(string key, T data);
Task<T> ReadAsync<T>(string key, T defaultValue = default);

// 存在检查
bool Has(string key);
void Delete(string key);
void Clear();
```

#### IScopedStorage

作用域存储接口，支持命名空间隔离。

#### 构造函数

```csharp
public ScopedStorage(IStorage storage, string scope, string delimiter = ".");
```

#### 使用示例

```csharp
// 创建根存储
var rootStorage = new FileStorage("user://data/");

// 创建分层存储
var playerStorage = new ScopedStorage(rootStorage, "player");
var saveStorage = new ScopedStorage(rootStorage, "saves");
var settingsStorage = new ScopedStorage(rootStorage, "settings");

// 使用分层存储
playerStorage.Write("profile", playerProfile);
playerStorage.Write("inventory", inventory);
playerStorage.Write("stats", playerStats);

saveStorage.Write("slot_1", saveData);
saveStorage.Write("slot_2", saveData);

settingsStorage.Write("graphics", graphicsSettings);
settingsStorage.Write("audio", audioSettings);

// 读取数据
var profile = playerStorage.Read<PlayerProfile>("profile", new PlayerProfile());
var inventory = playerStorage.Read<Inventory>("inventory", new Inventory());

// 使用完整路径
playerStorage.Write("savegames/auto", autoSaveData); // 写入 player/savegames/auto.json
```

### FileStorage

文件存储实现，基于 Godot 的 FileAccess。

#### 构造函数

```csharp
public FileStorage(string rootPath, bool createDirectoryIfNotExists = true);
```

#### 使用示例

```csharp
// 创建文件存储
var storage = new FileStorage("user://saves");

// 基础用法
storage.Write("player", playerData);
var loadedPlayer = storage.Read<Player>("player", new Player());

// 异步用法
await storage.WriteAsync("player", playerData);
var loadedPlayer = await storage.ReadAsync<Player>("player");

// 检查存在性
bool hasPlayerData = storage.Has("player");
storage.Delete("old_save");
storage.Clear();
```

### JsonStorage

JSON 存储实现，使用 Newtonsoft.Json 进行序列化。

#### 构造函数

```csharp
public JsonStorage(IFileStorage fileStorage, JsonSerializerSettings settings = null);
```

#### 使用示例

```csharp
var fileStorage = new FileStorage("user://saves");
var settings = new JsonSerializerSettings
{
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
    DefaultValueHandling = DefaultValueHandling.Populate
};

var storage = new JsonStorage(fileStorage, settings);

// 自定义序列化
storage.Write("player", playerData, customSerializer);

// 压缩缩存储
storage.Write("player", playerData, serializer: new JsonSerializer 
{ 
    Formatting = Formatting.None 
});
```

### CachedStorage

缓存存储实现，提高频繁访问的性能。

#### 构造函数

```csharp
public CachedStorage(IStorage innerStorage, TimeSpan cacheExpiry = default);
```

#### 使用示例

```csharp
var fileStorage = new FileStorage("user://saves");
var cachedStorage = new CachedStorage(fileStorage, TimeSpan.FromMinutes(5));

// 第一次读取会从存储加载
var player1 = cachedStorage.Read<Player>("player");

// 第二次读取会从缓存返回（如果未过期）
var player2 = cachedStorage.Read<Player>("player");

// 手动清除缓存
cachedStorage.ClearCache();

// 检查缓存状态
var playerInCache = cachedStorage.IsCached("player");
```

## 序列化系统

### JsonSerializer

JSON 序列化工具类，基于 Newtonsoft.Json。

#### 构造函数

```csharp
public JsonSerializer(JsonSerializerSettings settings = null);
```

#### 方法

```csharp
// 基础序列化
public string Serialize<T>(T data);
public void SerializeToFile<T>(string path, T data);

// 反序列化
public T Deserialize<T>(string json);
public T DeserializeFromFile<T>(string path, T defaultValue = default);

// 异步序列化
public Task<string> SerializeAsync<T>(T data);
public Task<T> DeserializeAsync<T>(string json, T defaultValue = default);

// 压缩序列化
public string SerializeCompressed<T>(T data);
public T DeserializeCompressed<T>(string compressedJson);
```

#### 使用示例

```csharp
var serializer = new JsonSerializer();

// 基础序列化
var json = serializer.Serialize(playerData);
var loadedPlayer = serializer.Deserialize<Player>(json);

// 文件操作
serializer.SerializeToFile("player.json", playerData);
var loadedFromFile = serializer.DeserializeFromFile<Player>("player.json");

// 异步操作
var json = await serializer.SerializeAsync(playerData);
var loadedAsync = await serializer.DeserializeAsync<Player>(json);

// 压缩操作
var compressed = serializer.SerializeCompressed(playerData);
var decompressed = serializer.DeserializeCompressed<Player>(compressed);
```

### ISerializable

可序列化接口。

#### 使用示例

```csharp
public class PlayerData : ISerializable
{
    public string PlayerId { get; set; }
    public int Level { get; set; }
    public int Score { get; set; }
    public Vector2 Position { get; set; }
    
    [JsonProperty("last_saved_at")]
    public DateTime LastSavedAt { get; set; }
    
    [JsonProperty("inventory_items")]
    public List<InventoryItem> Inventory { get; set; }
    
    // 自定义序列化方法
    [OnSerializing]
    public void OnSerializing()
    {
        // 序列化前的处理
        LastSavedAt = DateTime.Now;
    }
    
    [OnDeserialized]
    public void OnDeserialized()
    {
        // 反序列化后的处理
        if (LastSavedAt == default)
        {
            LastSavedAt = DateTime.Now;
        }
    }
}
```

## 数据模型

### SaveData

存档数据类。

#### 属性

```csharp
public string Version { get; set; } = "1.0.0";
public DateTime SavedAt { get; set; }
public Dictionary<string, object> PlayerData { get; set; } = new();
public Dictionary<string, object> GameData { get; set; } = new();
public Dictionary<string, object> SystemData { get; set; } = new();
public Dictionary<string, object> ModuleData { get; set; } = new();
public Dictionary<string, object> SettingsData { get; set; } = new();
```

### PlayerData

玩家数据类。

#### 属性

```csharp
[JsonProperty("player_id")]
public string PlayerId { get; set; }

[JsonProperty("player_name")]
public string PlayerName { get; set; }

[JsonProperty("level")]
public int Level { get; set; }

[JsonProperty("experience")]
public long Experience { get; set; }

[JsonProperty("health")]
public int Health { get; set; }
public int MaxHealth { get; set; }

[JsonProperty("position")]
public Vector3 Position { get; set; }

[JsonProperty("inventory")]
public InventoryData Inventory { get; set; }

[JsonProperty("skills")]
public List<SkillData> Skills { get; set; }
}
```

### GameData

游戏数据类。

#### 属性

```csharp
[JsonProperty("current_level")]
public int CurrentLevel { get; set; }

[JsonProperty("high_score")]
public int HighScore { get; set; }

[JsonProperty("total_play_time")]
public float TotalPlayTime { get; set; }

[JsonProperty("enemies_defeated")]
public int EnemiesDefeated { get; set; }

[JsonProperty("achievements_unlocked")]
public List<string> AchievementsUnlocked { get; set; }
```

### InventoryData

背包数据类。

#### 属性

```csharp
[JsonProperty("slots")]
public List<InventorySlot> Slots { get; set; } = new();

[JsonProperty("equipment")]
public Dictionary<string, string> Equipment { get; set; } = new();
```

### InventorySlot

背包槽位类。

#### 属性

```csharp
[JsonProperty("item_id")]
public string ItemId { get; set; }

[JsonProperty("quantity")]
public int Quantity { get; set; }

[JsonProperty("durability")]
public int Durability { get; set; }

[JsonProperty("metadata")]
public Dictionary<string, object> Metadata { get; set; } = new();
```

## 工具类

### StorageUtility

存储工具类，提供高级存储功能。

#### 构造函数

```csharp
public StorageUtility(IStorage storage, IVersionMigrationManager migrationManager = null);
```

#### 方法

```csharp
// 自动保存
public void EnableAutoSave(IArchitecture architecture, float intervalMinutes = 5.0f);
public void DisableAutoSave();

// 存档管理
public void CreateSave(int slotId, SaveData data);
public SaveData LoadSave(int slotId);
public List<SaveSlotInfo> GetSaveSlots();
public void DeleteSave(int slotId);

// 数据迁移
public void RegisterMigration<T>(int fromVersion, int toVersion, Func<T, T> migrator);
```

#### 使用示例

```csharp
[ContextAware]
[Log]
public partial class GameManager : Node, IController
{
    private StorageUtility _storageUtility;
    
    protected override void OnInit()
    {
        _storageUtility = Context.GetUtility<StorageUtility>();
        
        // 注册数据迁移
        _storageUtility.RegisterMigration<PlayerData>(1, 2, MigratePlayerDataV1ToV2);
        _storageUtility.RegisterMigration<PlayerData>(2, 3, MigratePlayerDataV2ToV3);
        
        // 启用自动保存
        _storageUtility.EnableAutoSave(Context, 5.0f);
        
        // 监听存档相关事件
        this.RegisterEvent<SaveRequestEvent>(OnSaveRequest);
        this.RegisterEvent<LoadRequestEvent>(OnLoadRequest);
    }
    
    private void OnSaveRequest(SaveRequestEvent e)
    {
        var saveData = CreateSaveData();
        _storageUtility.CreateSave(e.SlotId, saveData);
    }
    
    private void OnLoadRequest(LoadRequestEvent e)
    {
        var saveData = _storageUtility.LoadSave(e.SlotId);
        if (saveData != null)
        {
            RestoreGameData(saveData);
        }
    }
    
    private SaveData CreateSaveData()
    {
        var playerModel = Context.GetModel<PlayerModel>();
        var gameModel = Context.GetModel<GameModel>();
        
        return new SaveData
        {
            Version = "1.0.0",
            SavedAt = DateTime.Now,
            PlayerData = new Dictionary<string, object>
            {
                ["player"] = playerModel.GetData(),
                ["statistics"] = playerModel.GetStatistics()
            },
            GameData = new Dictionary<string, object>
            {
                ["game"] = gameModel.GetData()
            }
        };
    }
    
    private PlayerData MigratePlayerDataV1ToV2(PlayerData v1Data)
    {
        return new PlayerData
        {
            PlayerId = v1Data.PlayerId,
            PlayerName = v1Data.PlayerName,
            Level = v1Data.Level,
            Experience = v1Data.Experience,
            Health = v1Data.Health,
            MaxHealth = Math.Max(100, v1Data.MaxHealth), // V2 新增最大生命值
            Position = v1Data.Position,
            Inventory = v1Data.Inventory,
            Skills = v1Data.Skills // V1 的 Kills 字段在 V2 中重命名为 Skills
        };
    }
    
    private PlayerData MigratePlayerDataV2ToV3(PlayerData v2Data)
    {
        return new PlayerData
        {
            PlayerId = v2Data.PlayerId,
            PlayerName = Versioning.GetVersionString(v2Data.Version),
            Level = v2Data.Level,
            Experience = v2Data.Experience,
            Health = v2Data.Health,
            MaxHealth = v2Data.MaxHealth,
            Position = v2Data.Position,
            Inventory = v2Data.Inventory,
            Skills = v2Data.Skills,
            Achievements = v2Data.Achievements ?? new List<string>() // V3 新增成就系统
        };
    }
}
```

---

**文档版本**: 1.0.0  
**更新日期**: 2026-01-12
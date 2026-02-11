# 架构模式最佳实践

> 指导你如何设计清晰、可维护、可扩展的游戏架构，遵循 GFramework 的设计原则。

## 📋 目录

- [设计原则](#设计原则)
- [架构分层](#架构分层)
- [依赖管理](#依赖管理)
- [事件系统设计](#事件系统设计)
- [模块化架构](#模块化架构)
- [错误处理策略](#错误处理策略)
- [测试策略](#测试策略)
- [重构指南](#重构指南)

## 设计原则

### 1. 单一职责原则 (SRP)

确保每个类只负责一个功能领域：

```csharp
// ✅ 好的做法：职责单一
public class PlayerMovementController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerInputEvent>(OnPlayerInput);
    }
    
    private void OnPlayerInput(PlayerInputEvent e)
    {
        // 只负责移动逻辑
        ProcessMovement(e.Direction);
    }
    
    private void ProcessMovement(Vector2 direction)
    {
        // 移动相关的业务逻辑
    }
}

public class PlayerCombatController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackInputEvent>(OnAttackInput);
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        // 只负责战斗逻辑
        ProcessAttack(e.Target);
    }
    
    private void ProcessAttack(Entity target)
    {
        // 战斗相关的业务逻辑
    }
}

// ❌ 避免：职责混乱
public class PlayerController : AbstractSystem
{
    private void OnPlayerInput(PlayerInputEvent e)
    {
        // 移动逻辑
        ProcessMovement(e.Direction);
        
        // 战斗逻辑
        if (e.IsAttacking)
        {
            ProcessAttack(e.Target);
        }
        
        // UI逻辑
        UpdateHealthBar();
        
        // 音效逻辑
        PlaySoundEffect();
        
        // 存档逻辑
        SaveGame();
        
        // 职责太多，难以维护
    }
}
```

### 2. 开闭原则 (OCP)

设计应该对扩展开放，对修改封闭：

```csharp
// ✅ 好的做法：使用接口和策略模式
public interface IWeaponStrategy
{
    void Attack(Entity attacker, Entity target);
    int CalculateDamage(Entity attacker, Entity target);
}

public class SwordWeaponStrategy : IWeaponStrategy
{
    public void Attack(Entity attacker, Entity target)
    {
        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);
        PlaySwingAnimation();
    }
    
    public int CalculateDamage(Entity attacker, Entity target)
    {
        return attacker.Strength + GetSwordBonus() - target.Armor;
    }
}

public class MagicWeaponStrategy : IWeaponStrategy
{
    public void Attack(Entity attacker, Entity target)
    {
        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);
        CastMagicEffect();
    }
    
    public int CalculateDamage(Entity attacker, Entity target)
    {
        return attacker.Intelligence * 2 + GetMagicBonus() - target.MagicResistance;
    }
}

public class CombatSystem : AbstractSystem
{
    private readonly Dictionary<WeaponType, IWeaponStrategy> _weaponStrategies;
    
    public CombatSystem()
    {
        _weaponStrategies = new()
        {
            { WeaponType.Sword, new SwordWeaponStrategy() },
            { WeaponType.Magic, new MagicWeaponStrategy() }
        };
    }
    
    public void Attack(Entity attacker, Entity target)
    {
        var weaponType = attacker.EquippedWeapon.Type;
        
        if (_weaponStrategies.TryGetValue(weaponType, out var strategy))
        {
            strategy.Attack(attacker, target);
        }
    }
    
    // 添加新武器类型时，只需要添加新的策略，不需要修改现有代码
    public void RegisterWeaponStrategy(WeaponType type, IWeaponStrategy strategy)
    {
        _weaponStrategies[type] = strategy;
    }
}

// ❌ 避免：需要修改现有代码来扩展
public class CombatSystem : AbstractSystem
{
    public void Attack(Entity attacker, Entity target)
    {
        var weaponType = attacker.EquippedWeapon.Type;
        
        switch (weaponType)
        {
            case WeaponType.Sword:
                // 剑的攻击逻辑
                break;
            case WeaponType.Bow:
                // 弓的攻击逻辑
                break;
            default:
                throw new NotSupportedException($"Weapon type {weaponType} not supported");
        }
        
        // 添加新武器类型时需要修改这里的 switch 语句
    }
}
```

### 3. 依赖倒置原则 (DIP)

高层模块不应该依赖低层模块，两者都应该依赖抽象：

```csharp
// ✅ 好的做法：依赖抽象
public interface IDataStorage
{
    Task SaveAsync<T>(string key, T data);
    Task<T> LoadAsync<T>(string key, T defaultValue = default);
    Task<bool> ExistsAsync(string key);
}

public class FileStorage : IDataStorage
{
    public async Task SaveAsync<T>(string key, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        await File.WriteAllTextAsync(GetFilePath(key), json);
    }
    
    public async Task<T> LoadAsync<T>(string key, T defaultValue = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return defaultValue;
            
        var json = await File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        return File.Exists(GetFilePath(key));
    }
    
    private string GetFilePath(string key)
    {
        return $"saves/{key}.json";
    }
}

public class CloudStorage : IDataStorage
{
    public async Task SaveAsync<T>(string key, T data)
    {
        // 云存储实现
        await UploadToCloud(key, data);
    }
    
    public async Task<T> LoadAsync<T>(string key, T defaultValue = default)
    {
        // 云存储实现
        return await DownloadFromCloud<T>(key, defaultValue);
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        // 云存储实现
        return await CheckCloudExists(key);
    }
}

// 高层模块依赖抽象
public class SaveSystem : AbstractSystem
{
    private readonly IDataStorage _storage;
    
    public SaveSystem(IDataStorage storage)
    {
        _storage = storage;
    }
    
    public async Task SaveGameAsync(SaveData data)
    {
        await _storage.SaveAsync("current_save", data);
    }
    
    public async Task<SaveData> LoadGameAsync()
    {
        return await _storage.LoadAsync<SaveData>("current_save");
    }
}

// ❌ 避免：依赖具体实现
public class SaveSystem : AbstractSystem
{
    private readonly FileStorage _storage; // 直接依赖具体实现
    
    public SaveSystem()
    {
        _storage = new FileStorage(); // 硬编码依赖
    }
    
    // 无法轻松切换到其他存储方式
}
```

## 架构分层

### 1. 清晰的层次结构

```csharp
// ✅ 好的做法：清晰的分层架构
namespace Game.Models
{
    // 数据层：只负责存储状态
    public class PlayerModel : AbstractModel
    {
        public BindableProperty<int> Health { get; } = new(100);
        public BindableProperty<int> MaxHealth { get; } = new(100);
        public BindableProperty<Vector2> Position { get; } = new(Vector2.Zero);
        public BindableProperty<PlayerState> State { get; } = new(PlayerState.Idle);
        
        protected override void OnInit()
        {
            // 只处理数据相关的逻辑
            Health.Register(OnHealthChanged);
        }
        
        private void OnHealthChanged(int newHealth)
        {
            if (newHealth <= 0)
            {
                State.Value = PlayerState.Dead;
                SendEvent(new PlayerDeathEvent());
            }
        }
    }
    
    public enum PlayerState
    {
        Idle,
        Moving,
        Attacking,
        Dead
    }
}

namespace Game.Systems
{
    // 业务逻辑层：处理游戏逻辑
    public class PlayerMovementSystem : AbstractSystem
    {
        private PlayerModel _playerModel;
        private GameModel _gameModel;
        
        protected override void OnInit()
        {
            _playerModel = GetModel<PlayerModel>();
            _gameModel = GetModel<GameModel>();
            
            this.RegisterEvent<PlayerInputEvent>(OnPlayerInput);
        }
        
        private void OnPlayerInput(PlayerInputEvent e)
        {
            if (_gameModel.State.Value != GameState.Playing)
                return;
                
            if (_playerModel.State.Value == PlayerState.Dead)
                return;
                
            // 处理移动逻辑
            ProcessMovement(e.Direction);
        }
        
        private void ProcessMovement(Vector2 direction)
        {
            if (direction != Vector2.Zero)
            {
                _playerModel.Position.Value += direction.Normalized() * GetMovementSpeed();
                _playerModel.State.Value = PlayerState.Moving;
                
                SendEvent(new PlayerMovedEvent { 
                    NewPosition = _playerModel.Position.Value,
                    Direction = direction
                });
            }
            else
            {
                _playerModel.State.Value = PlayerState.Idle;
            }
        }
        
        private float GetMovementSpeed()
        {
            // 从玩家属性或其他地方获取速度
            return 5.0f;
        }
    }
}

namespace Game.Controllers
{
    // 控制层：连接用户输入和业务逻辑
    [ContextAware]
    public partial class PlayerController : Node, IController
    {
        private PlayerModel _playerModel;
        
        public override void _Ready()
        {
            _playerModel = Context.GetModel<PlayerModel>();
            
            // 监听用户输入
            SetProcessInput(true);
            
            // 监听数据变化，更新UI
            _playerModel.Health.Register(UpdateHealthUI);
            _playerModel.Position.Register(UpdatePosition);
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                var direction = GetInputDirection(keyEvent);
                Context.SendEvent(new PlayerInputEvent { Direction = direction });
            }
        }
        
        private void UpdateHealthUI(int health)
        {
            // 更新UI显示
            var healthBar = GetNode<ProgressBar>("UI/HealthBar");
            healthBar.Value = (float)health / _playerModel.MaxHealth.Value * 100;
        }
        
        private void UpdatePosition(Vector2 position)
        {
            // 更新玩家位置
            Position = position;
        }
        
        private Vector2 GetInputDirection(InputEventKey keyEvent)
        {
            return keyEvent.Keycode switch
            {
                Key.W => Vector2.Up,
                Key.S => Vector2.Down,
                Key.A => Vector2.Left,
                Key.D => Vector2.Right,
                _ => Vector2.Zero
            };
        }
    }
}
```

### 2. 避免层次混乱

```csharp
// ❌ 避免：层次混乱
public class PlayerController : Node, IController
{
    // 混合了数据层、业务逻辑层和控制层的职责
    public BindableProperty<int> Health { get; } = new(100); // 数据层职责
    
    public override void _Input(InputEvent @event) // 控制层职责
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.W)
            {
                Position += Vector2.Up * MovementSpeed; // 业务逻辑层职责
            }
            
            if (keyEvent.Keycode == Key.Space)
            {
                Health -= 10; // 业务逻辑层职责
                PlaySoundEffect(); // 业务逻辑层职责
            }
        }
    }
    
    // 这样会导致代码难以测试和维护
}
```

## 依赖管理

### 1. 构造函数注入

```csharp
// ✅ 好的做法：构造函数注入
public class PlayerCombatSystem : AbstractSystem
{
    private readonly PlayerModel _playerModel;
    private readonly IWeaponService _weaponService;
    private readonly ISoundService _soundService;
    private readonly IEffectService _effectService;
    
    // 通过构造函数注入依赖
    public PlayerCombatSystem(
        PlayerModel playerModel,
        IWeaponService weaponService,
        ISoundService soundService,
        IEffectService effectService)
    {
        _playerModel = playerModel;
        _weaponService = weaponService;
        _soundService = soundService;
        _effectService = effectService;
    }
    
    protected override void OnInit()
    {
        this.RegisterEvent<AttackEvent>(OnAttack);
    }
    
    private void OnAttack(AttackEvent e)
    {
        var weapon = _weaponService.GetEquippedWeapon(_playerModel);
        var damage = _weaponService.CalculateDamage(weapon, e.Target);
        
        e.Target.TakeDamage(damage);
        _soundService.PlayAttackSound(weapon.Type);
        _effectService.PlayAttackEffect(_playerModel.Position, weapon.Type);
    }
}

// ❌ 避免：依赖注入容器
public class PlayerCombatSystem : AbstractSystem
{
    private PlayerModel _playerModel;
    private IWeaponService _weaponService;
    private ISoundService _soundService;
    private IEffectService _effectService;
    
    protected override void OnInit()
    {
        // 在运行时获取依赖，难以测试
        _playerModel = GetModel<PlayerModel>();
        _weaponService = GetService<IWeaponService>();
        _soundService = GetService<ISoundService>();
        _effectService = GetService<IEffectService>();
    }
    
    // 测试时难以模拟依赖
}
```

### 2. 接口隔离

```csharp
// ✅ 好的做法：小而专注的接口
public interface IMovementController
{
    void Move(Vector2 direction);
    void Stop();
    bool CanMove();
}

public interface ICombatController
{
    void Attack(Entity target);
    void Defend();
    bool CanAttack();
}

public interface IUIController
{
    void ShowHealthBar();
    void HideHealthBar();
    void UpdateHealthDisplay(int currentHealth, int maxHealth);
}

public class PlayerController : Node, IMovementController, ICombatController, IUIController
{
    // 实现各个接口，职责清晰
}

// ❌ 避免：大而全的接口
public interface IPlayerController
{
    void Move(Vector2 direction);
    void Stop();
    void Attack(Entity target);
    void Defend();
    void ShowHealthBar();
    void HideHealthBar();
    void UpdateHealthDisplay(int currentHealth, int maxHealth);
    void SaveGame();
    void LoadGame();
    void Respawn();
    void PlayAnimation(string animationName);
    void StopAnimation();
    // ... 更多方法，接口过于庞大
}
```

## 事件系统设计

### 1. 事件命名和结构

```csharp
// ✅ 好的做法：清晰的事件命名和结构
public struct PlayerHealthChangedEvent
{
    public int PreviousHealth { get; }
    public int NewHealth { get; }
    public int MaxHealth { get; }
    public Vector3 DamagePosition { get; }
    public DamageType DamageType { get; }
}

public struct PlayerDiedEvent
{
    public Vector3 DeathPosition { get; }
    public string CauseOfDeath { get; }
    public TimeSpan SurvivalTime { get; }
}

public struct WeaponEquippedEvent
{
    public string PlayerId { get; }
    public WeaponType WeaponType { get; }
    public string WeaponId { get; }
}

// ❌ 避免：模糊的事件命名和结构
public struct PlayerEvent
{
    public EventType Type { get; }
    public object Data { get; } // 类型不安全
    public Dictionary<string, object> Properties { get; } // 难以理解
}
```

### 2. 事件处理职责

```csharp
// ✅ 好的做法：单一职责的事件处理
public class UIHealthBarController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        UpdateHealthBar(e.NewHealth, e.MaxHealth);
        
        if (e.NewHealth < e.PreviousHealth)
        {
            ShowDamageEffect(e.DamagePosition, e.PreviousHealth - e.NewHealth);
        }
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        HideHealthBar();
        ShowDeathScreen(e.CauseOfDeath);
    }
}

public class AudioController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (e.NewHealth < e.PreviousHealth)
        {
            PlayHurtSound(e.DamageType);
        }
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        PlayDeathSound();
    }
}

// ❌ 避免：一个处理器处理多种不相关的事件
public class PlayerEventHandler : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
        this.RegisterEvent<WeaponEquippedEvent>(OnWeaponEquipped);
        this.RegisterEvent<LevelUpEvent>(OnLevelUp);
        // 注册太多事件，职责混乱
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        UpdateUI();          // UI职责
        PlayAudio();         // 音频职责
        SaveStatistics();     // 存档职责
        UpdateAchievements(); // 成就系统职责
        // 一个事件处理器承担太多职责
    }
}
```

## 模块化架构

### 1. 模块边界清晰

```csharp
// ✅ 好的做法：清晰的模块边界
public class AudioModule : AbstractModule
{
    // 模块只负责音频相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterSystem(new MusicSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
}

public class InputModule : AbstractModule
{
    // 模块只负责输入相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new InputSystem());
        architecture.RegisterSystem(new InputMappingSystem());
        architecture.RegisterUtility(new InputUtility());
    }
}

public class UIModule : AbstractModule
{
    // 模块只负责UI相关的功能
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new UISystem());
        architecture.RegisterSystem(new HUDSystem());
        architecture.RegisterSystem(new MenuSystem());
        architecture.RegisterUtility(new UIUtility());
    }
}

// ❌ 避免：模块职责混乱
public class GameModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        // 一个模块包含所有功能
        architecture.RegisterSystem(new AudioSystem());        // 音频
        architecture.RegisterSystem(new InputSystem());         // 输入
        architecture.RegisterSystem(new UISystem());            // UI
        architecture.RegisterSystem(new CombatSystem());        // 战斗
        architecture.RegisterSystem(new InventorySystem());     // 背包
        architecture.RegisterSystem(new QuestSystem());          // 任务
        // 模块过于庞大，难以维护
    }
}
```

### 2. 模块间通信

```csharp
// ✅ 好的做法：通过事件进行模块间通信
public class AudioModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
    }
}

public class AudioSystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听其他模块发送的事件
        this.RegisterEvent<PlayerAttackEvent>(OnPlayerAttack);
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
        this.RegisterEvent<WeaponEquippedEvent>(OnWeaponEquipped);
    }
    
    private void OnPlayerAttack(PlayerAttackEvent e)
    {
        PlayAttackSound(e.WeaponType);
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        PlayDeathSound();
    }
    
    private void OnWeaponEquipped(WeaponEquippedEvent e)
    {
        PlayEquipSound(e.WeaponType);
    }
}

public class CombatModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new CombatSystem());
    }
}

public class CombatSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<AttackInputEvent>(OnAttackInput);
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        ProcessAttack(e);
        
        // 发送事件通知其他模块
        SendEvent(new PlayerAttackEvent { 
            PlayerId = e.PlayerId, 
            WeaponType = GetPlayerWeaponType(e.PlayerId) 
        });
    }
}

// ❌ 避免：模块间直接依赖
public class CombatSystem : AbstractSystem
{
    private AudioSystem _audioSystem; // 直接依赖其他模块
    
    protected override void OnInit()
    {
        // 直接获取其他模块的系统
        _audioSystem = GetSystem<AudioSystem>();
    }
    
    private void OnAttackInput(AttackInputEvent e)
    {
        ProcessAttack(e);
        
        // 直接调用其他模块的方法
        _audioSystem.PlayAttackSound(weaponType);
    }
}
```

## 错误处理策略

### 1. 异常处理层次

```csharp
// ✅ 好的做法：分层异常处理
public class GameApplicationException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    public GameApplicationException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary<string, object>();
    }
}

public class PlayerException : GameApplicationException
{
    public PlayerException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, errorCode, context, innerException)
    {
    }
}

public class InventoryException : GameApplicationException
{
    public InventoryException(string message, string errorCode, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, errorCode, context, innerException)
    {
    }
}

// 在系统中的使用
public class PlayerInventorySystem : AbstractSystem
{
    public void AddItem(string playerId, Item item)
    {
        try
        {
            ValidateItem(item);
            CheckInventorySpace(playerId, item);
            
            AddItemToInventory(playerId, item);
            
            SendEvent(new ItemAddedEvent { PlayerId = playerId, Item = item });
        }
        catch (ItemValidationException ex)
        {
            throw new InventoryException(
                $"Failed to add item {item.Id} to player {playerId}",
                "ITEM_VALIDATION_FAILED",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["validationError"] = ex.Message
                },
                ex
            );
        }
        catch (InventoryFullException ex)
        {
            throw new InventoryException(
                $"Player {playerId} inventory is full",
                "INVENTORY_FULL",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["maxSlots"] = ex.MaxSlots,
                    ["currentSlots"] = ex.CurrentSlots
                },
                ex
            );
        }
        catch (Exception ex)
        {
            // 捕获未知异常并包装
            throw new InventoryException(
                $"Unexpected error adding item {item.Id} to player {playerId}",
                "UNKNOWN_ERROR",
                new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["itemId"] = item.Id,
                    ["originalError"] = ex.Message
                },
                ex
            );
        }
    }
    
    private void ValidateItem(Item item)
    {
        if (item == null)
            throw new ItemValidationException("Item cannot be null");
            
        if (string.IsNullOrEmpty(item.Id))
            throw new ItemValidationException("Item ID cannot be empty");
            
        if (item.StackSize <= 0)
            throw new ItemValidationException("Item stack size must be positive");
    }
    
    private void CheckInventorySpace(string playerId, Item item)
    {
        var inventory = GetPlayerInventory(playerId);
        var requiredSpace = CalculateRequiredSpace(item);
        
        if (inventory.FreeSpace < requiredSpace)
        {
            throw new InventoryFullException(
                inventory.FreeSpace,
                inventory.MaxSlots
            );
        }
    }
}
```

### 2. 错误恢复策略

```csharp
// ✅ 好的做法：优雅的错误恢复
public class SaveSystem : AbstractSystem
{
    private readonly IStorage _primaryStorage;
    private readonly IStorage _backupStorage;
    
    public SaveSystem(IStorage primaryStorage, IStorage backupStorage = null)
    {
        _primaryStorage = primaryStorage;
        _backupStorage = backupStorage ?? new LocalStorage("backup");
    }
    
    public async Task<SaveData> LoadSaveDataAsync(string saveId)
    {
        try
        {
            // 尝试从主存储加载
            return await _primaryStorage.ReadAsync<SaveData>(saveId);
        }
        catch (StorageException ex)
        {
            Logger.Warning($"Failed to load from primary storage: {ex.Message}");
            
            try
            {
                // 尝试从备份存储加载
                var backupData = await _backupStorage.ReadAsync<SaveData>(saveId);
                Logger.Info($"Successfully loaded from backup storage: {saveId}");
                
                // 恢复到主存储
                await _primaryStorage.WriteAsync(saveId, backupData);
                
                return backupData;
            }
            catch (Exception backupEx)
            {
                Logger.Error($"Failed to load from backup storage: {backupEx.Message}");
                
                // 返回默认存档数据
                return GetDefaultSaveData();
            }
        }
    }
    
    private SaveData GetDefaultSaveData()
    {
        Logger.Warning("Returning default save data due to loading failures");
        return new SaveData
        {
            PlayerId = "default",
            Level = 1,
            Health = 100,
            Position = Vector3.Zero,
            CreatedAt = DateTime.Now
        };
    }
}

// ❌ 避免：粗暴的错误处理
public class SaveSystem : AbstractSystem
{
    public async Task<SaveData> LoadSaveDataAsync(string saveId)
    {
        try
        {
            return await _storage.ReadAsync<SaveData>(saveId);
        }
        catch (Exception ex)
        {
            // 直接抛出异常，不提供恢复机制
            throw new Exception($"Failed to load save: {ex.Message}", ex);
        }
    }
}
```

## 测试策略

### 1. 可测试的架构设计

```csharp
// ✅ 好的做法：可测试的架构
public interface IPlayerMovementService
{
    void MovePlayer(string playerId, Vector2 direction);
    bool CanPlayerMove(string playerId);
}

public class PlayerMovementService : IPlayerMovementService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ICollisionService _collisionService;
    private readonly IMapService _mapService;
    
    public PlayerMovementService(
        IPlayerRepository playerRepository,
        ICollisionService collisionService,
        IMapService mapService)
    {
        _playerRepository = playerRepository;
        _collisionService = collisionService;
        _mapService = mapService;
    }
    
    public void MovePlayer(string playerId, Vector2 direction)
    {
        if (!CanPlayerMove(playerId))
            return;
            
        var player = _playerRepository.GetById(playerId);
        var newPosition = player.Position + direction * player.Speed;
        
        if (_collisionService.CanMoveTo(newPosition))
        {
            player.Position = newPosition;
            _playerRepository.Update(player);
        }
    }
    
    public bool CanPlayerMove(string playerId)
    {
        var player = _playerRepository.GetById(playerId);
        return player != null && player.IsAlive && !player.IsStunned;
    }
}

// 测试代码
[TestFixture]
public class PlayerMovementServiceTests
{
    private Mock<IPlayerRepository> _mockPlayerRepository;
    private Mock<ICollisionService> _mockCollisionService;
    private Mock<IMapService> _mockMapService;
    private PlayerMovementService _movementService;
    
    [SetUp]
    public void Setup()
    {
        _mockPlayerRepository = new Mock<IPlayerRepository>();
        _mockCollisionService = new Mock<ICollisionService>();
        _mockMapService = new Mock<IMapService>();
        
        _movementService = new PlayerMovementService(
            _mockPlayerRepository.Object,
            _mockCollisionService.Object,
            _mockMapService.Object
        );
    }
    
    [Test]
    public void MovePlayer_ValidMovement_ShouldUpdatePlayerPosition()
    {
        // Arrange
        var playerId = "player1";
        var player = new Player { Id = playerId, Position = Vector2.Zero, Speed = 5.0f };
        var direction = Vector2.Right;
        
        _mockPlayerRepository.Setup(r => r.GetById(playerId)).Returns(player);
        _mockCollisionService.Setup(c => c.CanMoveTo(It.IsAny<Vector2>())).Returns(true);
        
        // Act
        _movementService.MovePlayer(playerId, direction);
        
        // Assert
        _mockPlayerRepository.Verify(r => r.Update(It.Is<Player>(p => p.Position == Vector2.Right * 5.0f)), Times.Once);
    }
    
    [Test]
    public void MovePlayer_CollisionBlocked_ShouldNotUpdatePlayerPosition()
    {
        // Arrange
        var playerId = "player1";
        var player = new Player { Id = playerId, Position = Vector2.Zero, Speed = 5.0f };
        var direction = Vector2.Right;
        
        _mockPlayerRepository.Setup(r => r.GetById(playerId)).Returns(player);
        _mockCollisionService.Setup(c => c.CanMoveTo(It.IsAny<Vector2>())).Returns(false);
        
        // Act
        _movementService.MovePlayer(playerId, direction);
        
        // Assert
        _mockPlayerRepository.Verify(r => r.Update(It.IsAny<Player>()), Times.Never);
    }
}

// ❌ 避免：难以测试的设计
public class PlayerMovementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<MovementInputEvent>(OnMovementInput);
    }
    
    private void OnMovementInput(MovementInputEvent e)
    {
        var player = GetModel<PlayerModel>(); // 依赖架构，难以测试
        var newPosition = player.Position + e.Direction * player.Speed;
        
        if (CanMoveTo(newPosition)) // 私有方法，难以直接测试
        {
            player.Position = newPosition;
        }
    }
    
    private bool CanMoveTo(Vector2 position)
    {
        // 复杂的碰撞检测逻辑，难以测试
        return true;
    }
}
```

## 重构指南

### 1. 识别代码异味

```csharp
// ❌ 代码异味：长方法、重复代码、上帝类
public class GameManager : Node
{
    public void ProcessPlayerInput(InputEvent @event)
    {
        // 长方法 - 做太多事情
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.W:
                    MovePlayer(Vector2.Up);
                    PlayFootstepSound();
                    UpdatePlayerAnimation("walk_up");
                    CheckPlayerCollisions();
                    UpdateCameraPosition();
                    SavePlayerPosition();
                    break;
                case Key.S:
                    MovePlayer(Vector2.Down);
                    PlayFootstepSound();
                    UpdatePlayerAnimation("walk_down");
                    CheckPlayerCollisions();
                    UpdateCameraPosition();
                    SavePlayerPosition();
                    break;
                // 重复代码
            }
        }
    }
    
    private void MovePlayer(Vector2 direction)
    {
        Player.Position += direction * Player.Speed;
    }
    
    private void PlayFootstepSound()
    {
        AudioPlayer.Play("footstep.wav");
    }
    
    // ... 更多方法，类过于庞大
}

// ✅ 重构后：职责分离
public class PlayerInputController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<InputEvent>(OnInput);
    }
    
    private void OnInput(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                SendEvent(new PlayerMoveEvent { Direction = direction });
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}

public class PlayerMovementSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMoveEvent>(OnPlayerMove);
    }
    
    private void OnPlayerMove(PlayerMoveEvent e)
    {
        var playerModel = GetModel<PlayerModel>();
        var newPosition = playerModel.Position + e.Direction * playerModel.Speed;
        
        if (CanMoveTo(newPosition))
        {
            playerModel.Position = newPosition;
            SendEvent(new PlayerMovedEvent { NewPosition = newPosition });
        }
    }
    
    private bool CanMoveTo(Vector2 position)
    {
        var collisionService = GetUtility<ICollisionService>();
        return collisionService.CanMoveTo(position);
    }
}

public class AudioSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMovedEvent>(OnPlayerMoved);
    }
    
    private void OnPlayerMoved(PlayerMovedEvent e)
    {
        PlayFootstepSound();
    }
    
    private void PlayFootstepSound()
    {
        var audioUtility = GetUtility<AudioUtility>();
        audioUtility.PlaySound("footstep.wav");
    }
}

public class AnimationSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerMovedEvent>(OnPlayerMoved);
    }
    
    private void OnPlayerMoved(PlayerMovedEvent e)
    {
        var animationName = GetAnimationNameFromDirection(e.Direction);
        SendEvent(new PlayAnimationEvent { AnimationName = animationName });
    }
    
    private string GetAnimationNameFromDirection(Vector2 direction)
    {
        if (direction == Vector2.Up) return "walk_up";
        if (direction == Vector2.Down) return "walk_down";
        if (direction == Vector2.Left) return "walk_left";
        if (direction == Vector2.Right) return "walk_right";
        return "idle";
    }
}
```

### 2. 渐进式重构

```csharp
// 第一步：提取重复代码
public class PlayerController : Node
{
    public void ProcessInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            Vector2 direction;
            switch (keyEvent.Keycode)
            {
                case Key.W:
                    direction = Vector2.Up;
                    break;
                case Key.S:
                    direction = Vector2.Down;
                    break;
                case Key.A:
                    direction = Vector2.Left;
                    break;
                case Key.D:
                    direction = Vector2.Right;
                    break;
                default:
                    return;
            }
            
            MovePlayer(direction);
        }
    }
}

// 第二步：提取方法
public class PlayerController : Node
{
    public void ProcessInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                MovePlayer(direction);
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}

// 第三步：引入系统和事件
public class PlayerController : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<InputEvent>(OnInput);
    }
    
    private void OnInput(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var direction = GetDirectionFromKey(keyEvent.Keycode);
            if (direction != Vector2.Zero)
            {
                SendEvent(new PlayerMoveEvent { Direction = direction });
            }
        }
    }
    
    private Vector2 GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.W => Vector2.Up,
            Key.S => Vector2.Down,
            Key.A => Vector2.Left,
            Key.D => Vector2.Right,
            _ => Vector2.Zero
        };
    }
}
```

---

## 总结

遵循这些架构模式最佳实践，你将能够构建：

- ✅ **清晰的代码结构** - 易于理解和维护
- ✅ **松耦合的组件** - 便于测试和扩展
- ✅ **可重用的模块** - 提高开发效率
- ✅ **健壮的错误处理** - 提高系统稳定性
- ✅ **完善的测试覆盖** - 保证代码质量

记住，好的架构不是一蹴而就的，需要持续的重构和改进。

---

**文档版本**: 1.0.0  
**更新日期**: 2026-01-12
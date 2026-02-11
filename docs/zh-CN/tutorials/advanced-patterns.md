# 高级模式教程

> 深入学习 GFramework 的高级特性和设计模式，构建更复杂和可维护的游戏系统。

## 📋 目录

- [架构模式](#架构模式)
- [事件驱动架构](#事件驱动架构)
- [插件系统](#插件系统)
- [网络集成](#网络集成)
- [模块化设计](#模块化设计)
- [性能分析](#性能分析)
- [错误处理](#错误处理)

## 架构模式

### 1. CQRS (命令查询职责分离)

实现完整的 CQRS 模式，分离读写操作：

```csharp
using GFramework.Core.command;
using GFramework.Core.query;
using GFramework.Core.events;

// 命令 - 负责写操作
public class CreatePlayerCommand : AbstractCommand
{
    public string PlayerName { get; set; }
    public PlayerClass Class { get; set; }
    public Vector3 InitialPosition { get; set; }
    
    protected override void OnExecute()
    {
        var playerModel = GetModel<PlayerModel>();
        
        // 验证命令
        if (string.IsNullOrWhiteSpace(PlayerName))
            throw new ArgumentException("Player name cannot be empty");
            
        if (playerModel.PlayerExists(PlayerName))
            throw new InvalidOperationException($"Player {PlayerName} already exists");
        
        // 创建玩家
        var playerData = new PlayerData
        {
            Id = Guid.NewGuid(),
            Name = PlayerName,
            Class = Class,
            Position = InitialPosition,
            Level = 1,
            Experience = 0,
            Health = 100,
            MaxHealth = 100,
            Mana = 50,
            MaxMana = 50,
            CreatedAt = DateTime.Now
        };
        
        // 保存玩家数据
        playerModel.AddPlayer(playerData);
        
        // 发送事件
        SendEvent(new PlayerCreatedEvent { Player = playerData });
    }
}

// 查询 - 负责读操作
public class GetPlayerQuery : AbstractQuery<PlayerData>
{
    public string PlayerName { get; set; }
    
    protected override PlayerData OnDo()
    {
        var playerModel = GetModel<PlayerModel>();
        return playerModel.GetPlayer(PlayerName);
    }
}

public class GetAllPlayersQuery : AbstractQuery<List<PlayerData>>
{
    public PlayerClass? FilterByClass { get; set; }
    public int? MinLevel { get; set; }
    
    protected override List<PlayerData> OnDo()
    {
        var playerModel = GetModel<PlayerModel>();
        var players = playerModel.GetAllPlayers();
        
        // 应用过滤器
        if (FilterByClass.HasValue)
        {
            players = players.Where(p => p.Class == FilterByClass.Value).ToList();
        }
        
        if (MinLevel.HasValue)
        {
            players = players.Where(p => p.Level >= MinLevel.Value).ToList();
        }
        
        return players;
    }
}

// 复杂查询示例
public class GetPlayerStatisticsQuery : AbstractQuery<PlayerStatistics>
{
    public string PlayerName { get; set; }
    
    protected override PlayerStatistics OnDo()
    {
        var playerModel = GetModel<PlayerModel>();
        var player = playerModel.GetPlayer(PlayerName);
        
        if (player == null)
            return null;
        
        // 计算统计数据
        var statistics = new PlayerStatistics
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Level = player.Level,
            Experience = player.Experience,
            ExperienceToNextLevel = CalculateExperienceToNextLevel(player.Level),
            HealthPercentage = (float)player.Health / player.MaxHealth * 100,
            ManaPercentage = (float)player.Mana / player.MaxMana * 100,
            PlayTime = player.PlayTime,
            LastLogin = player.LastLogin,
            AchievementsUnlocked = player.Achievements.Count,
            TotalItemsOwned = player.Inventory.Count
        };
        
        return statistics;
    }
    
    private int CalculateExperienceToNextLevel(int currentLevel)
    {
        // 经验值计算公式
        return currentLevel * 1000 + (currentLevel - 1) * 500;
    }
}

// 事件 - 领域事件
public struct PlayerCreatedEvent
{
    public PlayerData Player { get; set; }
}

public struct PlayerLevelUpEvent
{
    public string PlayerName { get; set; }
    public int NewLevel { get; set; }
    public int NewMaxHealth { get; set; }
    public int NewMaxMana { get; set; }
}

// 使用示例
[ContextAware]
[Log]
public partial class PlayerService : IController
{
    public void CreateNewPlayer(string name, PlayerClass playerClass)
    {
        var command = new CreatePlayerCommand
        {
            PlayerName = name,
            Class = playerClass,
            InitialPosition = Vector3.Zero
        };
        
        try
        {
            Context.SendCommand(command);
            Logger.Info($"Player {name} created successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create player {name}: {ex.Message}");
            throw;
        }
    }
    
    public PlayerData GetPlayer(string name)
    {
        var query = new GetPlayerQuery { PlayerName = name };
        return Context.SendQuery(query);
    }
    
    public List<PlayerData> GetAllPlayers(PlayerClass? classFilter = null, int? minLevel = null)
    {
        var query = new GetAllPlayersQuery 
        { 
            FilterByClass = classFilter, 
            MinLevel = minLevel 
        };
        return Context.SendQuery(query);
    }
    
    public PlayerStatistics GetPlayerStatistics(string playerName)
    {
        var query = new GetPlayerStatisticsQuery { PlayerName = playerName };
        return Context.SendQuery(query);
    }
}
```

### 2. 领域驱动设计 (DDD)

实现领域驱动设计的核心概念：

```csharp
// 领域实体
public class Player : AbstractEntity
{
    public PlayerId Id { get; private set; }
    public PlayerName Name { get; private set; }
    public PlayerClass Class { get; private set; }
    public Level Level { get; private set; }
    public Experience Experience { get; private set; }
    public Health Health { get; private set; }
    public Mana Mana { get; private set; }
    public Inventory Inventory { get; private set; }
    public List<Achievement> Achievements { get; private set; }
    
    private List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public Player(PlayerId id, PlayerName name, PlayerClass playerClass)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Class = playerClass;
        
        Level = new Level(1);
        Experience = new Experience(0);
        Health = new Health(100, 100);
        Mana = new Mana(50, 50);
        Inventory = new Inventory();
        Achievements = new List<Achievement>();
        
        // 添加领域事件
        AddDomainEvent(new PlayerCreatedDomainEvent(Id, Name, Class));
    }
    
    public void GainExperience(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Experience amount must be positive");
        
        var oldLevel = Level.Value;
        Experience.Add(amount);
        
        // 检查是否升级
        while (Experience.Value >= CalculateRequiredExperience(Level.Value + 1))
        {
            LevelUp();
        }
        
        if (Level.Value > oldLevel)
        {
            AddDomainEvent(new PlayerLevelUpDomainEvent(Id, Name, Level.Value, oldLevel));
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative");
        
        Health.Reduce(damage);
        
        if (Health.Value <= 0)
        {
            Health.Value = 0;
            AddDomainEvent(new PlayerDiedDomainEvent(Id, Name));
        }
        else
        {
            AddDomainEvent(new PlayerDamagedDomainEvent(Id, Name, damage, Health.Value));
        }
    }
    
    public void Heal(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Heal amount must be positive");
        
        Health.Increase(amount);
        AddDomainEvent(new PlayerHealedDomainEvent(Id, Name, amount, Health.Value));
    }
    
    public void AddItem(Item item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        Inventory.AddItem(item);
        AddDomainEvent(new ItemAddedDomainEvent(Id, Name, item.Id, item.Name));
    }
    
    public void RemoveItem(ItemId itemId)
    {
        var item = Inventory.GetItem(itemId);
        if (item == null)
            throw new InvalidOperationException($"Item {itemId} not found in inventory");
        
        Inventory.RemoveItem(itemId);
        AddDomainEvent(new ItemRemovedDomainEvent(Id, Name, itemId, item.Name));
    }
    
    private void LevelUp()
    {
        var oldLevel = Level.Value;
        Level.Increase();
        
        // 增加属性
        var healthIncrease = CalculateHealthIncrease(Class);
        var manaIncrease = CalculateManaIncrease(Class);
        
        Health.IncreaseMax(healthIncrease);
        Health.RestoreToFull();
        
        Mana.IncreaseMax(manaIncrease);
        Mana.RestoreToFull();
        
        Logger.Info($"Player {Name.Value} leveled up to {Level.Value}");
    }
    
    private int CalculateRequiredExperience(int level)
    {
        return level * 1000 + (level - 1) * 500;
    }
    
    private int CalculateHealthIncrease(PlayerClass playerClass)
    {
        return playerClass switch
        {
            PlayerClass.Warrior => 20,
            PlayerClass.Mage => 10,
            PlayerClass.Rogue => 15,
            PlayerClass.Priest => 12,
            _ => 15
        };
    }
    
    private int CalculateManaIncrease(PlayerClass playerClass)
    {
        return playerClass switch
        {
            PlayerClass.Warrior => 5,
            PlayerClass.Mage => 20,
            PlayerClass.Rogue => 8,
            PlayerClass.Priest => 15,
            _ => 10
        };
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// 值对象
public record PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
}

public record PlayerName(string Value)
{
    public PlayerName(string value) : base(value?.Trim() ?? string.Empty)
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Player name cannot be empty");
            
        if (Value.Length > 50)
            throw new ArgumentException("Player name cannot exceed 50 characters");
    }
}

public record Level(int Value)
{
    public Level(int value) : base(Math.Max(1, value))
    {
        if (value < 1)
            throw new ArgumentException("Level cannot be less than 1");
            
        if (value > 100)
            throw new ArgumentException("Level cannot exceed 100");
    }
    
    public void Increase() => Value + 1;
}

public record Experience(int Value)
{
    public Experience(int value) : base(Math.Max(0, value))
    {
        if (value < 0)
            throw new ArgumentException("Experience cannot be negative");
    }
    
    public void Add(int amount) => Value + amount;
}

public record Health(int Value, int MaxValue)
{
    public Health(int value, int maxValue) : base(Math.Max(0, value), Math.Max(1, maxValue))
    {
        if (value < 0)
            throw new ArgumentException("Health cannot be negative");
            
        if (maxValue <= 0)
            throw new ArgumentException("Max health must be positive");
            
        if (value > maxValue)
            Value = maxValue;
    }
    
    public void Reduce(int amount) => new(Math.Max(0, Value - amount), MaxValue);
    
    public void Increase(int amount) => new(Math.Min(MaxValue, Value + amount), MaxValue);
    
    public void IncreaseMax(int amount) => new(Value, MaxValue + amount);
    
    public void RestoreToFull() => new(MaxValue, MaxValue);
}

// 领域事件
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public record PlayerCreatedDomainEvent(PlayerId PlayerId, PlayerName PlayerName, PlayerClass Class) 
    : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.Now;
}

public record PlayerLevelUpDomainEvent(PlayerId PlayerId, PlayerName PlayerName, int NewLevel, int OldLevel) 
    : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.Now;
}

// 领域服务
public interface IPlayerDomainService
{
    bool CanPlayerAttack(Player attacker, Player target);
    int CalculateDamage(Player attacker, Player target);
    bool IsPlayerAlive(Player player);
}

public class PlayerDomainService : IPlayerDomainService
{
    public bool CanPlayerAttack(Player attacker, Player target)
    {
        return IsPlayerAlive(attacker) && IsPlayerAlive(target) && 
               attacker.Id != target.Id &&
               Vector3.Distance(attacker.Position, target.Position) <= GetAttackRange(attacker.Class);
    }
    
    public int CalculateDamage(Player attacker, Player target)
    {
        var baseDamage = GetBaseDamage(attacker.Class, attacker.Level.Value);
        var weaponDamage = attacker.Inventory.EquippedWeapon?.Damage ?? 0;
        var defense = target.Inventory.EquippedArmor?.Defense ?? 0;
        
        var totalDamage = baseDamage + weaponDamage - defense;
        return Math.Max(1, totalDamage); // 最少造成1点伤害
    }
    
    public bool IsPlayerAlive(Player player)
    {
        return player.Health.Value > 0;
    }
    
    private float GetAttackRange(PlayerClass playerClass)
    {
        return playerClass switch
        {
            PlayerClass.Warrior => 1.5f,
            PlayerClass.Mage => 10.0f,
            PlayerClass.Rogue => 1.2f,
            PlayerClass.Priest => 5.0f,
            _ => 2.0f
        };
    }
    
    private int GetBaseDamage(PlayerClass playerClass, int level)
    {
        var baseDamage = playerClass switch
        {
            PlayerClass.Warrior => 15,
            PlayerClass.Mage => 8,
            PlayerClass.Rogue => 12,
            PlayerClass.Priest => 6,
            _ => 10
        };
        
        return baseDamage + (level - 1) * 2; // 等级加成
    }
}
```

## 事件驱动架构

### 1. 事件溯源模式

实现事件的持久化和重放：

```csharp
using GFramework.Core.events;
using System.Collections.Concurrent;

public class EventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _eventStreams = new();
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _snapshots = new();
    private readonly object _lock = new();
    
    public async Task SaveEventsAsync(string streamId, IEnumerable<IDomainEvent> events, int expectedVersion = -1)
    {
        if (!_eventStreams.TryGetValue(streamId, out var eventStream))
        {
            eventStream = new List<IDomainEvent>();
            _eventStreams[streamId] = eventStream;
        }
        
        lock (_lock)
        {
            if (expectedVersion >= 0 && eventStream.Count != expectedVersion)
            {
                throw new ConcurrencyException($"Expected version {expectedVersion}, but stream has {eventStream.Count} events");
            }
            
            foreach (var evt in events)
            {
                eventStream.Add(evt);
            }
            
            // 定期创建快照
            if (eventStream.Count % 100 == 0)
            {
                await CreateSnapshotAsync(streamId);
            }
        }
    }
    
    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(string streamId, int fromVersion = 0)
    {
        // 检查是否可以从快照开始
        var snapshotVersion = GetSnapshotVersion(streamId, fromVersion);
        if (snapshotVersion >= 0)
        {
            var events = new List<IDomainEvent>();
            var snapshot = _snapshots[streamId][snapshotVersion];
            events.Add(snapshot);
            
            // 添加快照之后的事件
            if (_eventStreams.TryGetValue(streamId, out var eventStream))
            {
                events.AddRange(eventStream.Skip(snapshotVersion + 1));
            }
            
            return events;
        }
        
        // 返回所有事件
        return _eventStreams.TryGetValue(streamId, out var eventStream) 
            ? eventStream.Skip(fromVersion) 
            : Enumerable.Empty<IDomainEvent>();
    }
    
    private async Task CreateSnapshotAsync(string streamId)
    {
        // 这里应该根据事件重建聚合状态
        // 简化实现，实际应该更复杂
        if (_eventStreams.TryGetValue(streamId, out var eventStream))
        {
            var snapshot = new AggregateSnapshot(streamId, eventStream.Count - 1);
            
            if (!_snapshots.TryGetValue(streamId, out var snapshotList))
            {
                snapshotList = new List<IDomainEvent>();
                _snapshots[streamId] = snapshotList;
            }
            
            snapshotList.Add(snapshot);
        }
    }
    
    private int GetSnapshotVersion(string streamId, int fromVersion)
    {
        if (!_snapshots.TryGetValue(streamId, out var snapshotList))
            return -1;
            
        // 找到最近的快照版本
        for (int i = snapshotList.Count - 1; i >= 0; i--)
        {
            if (((AggregateSnapshot)snapshotList[i]).Version >= fromVersion)
                return i;
        }
        
        return -1;
    }
}

public interface IEventStore
{
    Task SaveEventsAsync(string streamId, IEnumerable<IDomainEvent> events, int expectedVersion = -1);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(string streamId, int fromVersion = 0);
}

public record AggregateSnapshot(string StreamId, int Version) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.Now;
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

// 聚合根重建器
public class AggregateRootBuilder
{
    private readonly IEventStore _eventStore;
    
    public AggregateRootBuilder(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    public async Task<T> RebuildAsync<T>(string aggregateId) where T : AggregateRoot, new()
    {
        var events = await _eventStore.GetEventsAsync(aggregateId);
        var aggregate = new T();
        
        foreach (var evt in events)
        {
            aggregate.ApplyEvent(evt);
        }
        
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }
}

public abstract class AggregateRoot
{
    public string Id { get; protected set; }
    public int Version { get; protected set; }
    
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
    
    protected void ApplyEvent(IDomainEvent evt)
    {
        // 调用具体的事件应用方法
        When(evt);
        
        // 添加到未提交事件列表
        _uncommittedEvents.Add(evt);
        Version++;
    }
    
    protected abstract void When(IDomainEvent evt);
    
    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var evt in events)
        {
            When(evt);
            Version++;
        }
    }
}
```

### 2. 事件总线模式

实现灵活的事件路由和处理：

```csharp
using GFramework.Core.events;

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<IEventHandler>> _handlers = new();
    private readonly Dictionary<Type, List<IAsyncEventHandler>> _asyncHandlers = new();
    private readonly IEventStore _eventStore;
    private readonly object _lock = new();
    
    public EventBus(IEventStore eventStore = null)
    {
        _eventStore = eventStore;
    }
    
    public void Subscribe<T>(IEventHandler<T> handler) where T : IEvent
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<IEventHandler>();
            }
            
            _handlers[eventType].Add(handler);
        }
    }
    
    public void SubscribeAsync<T>(IAsyncEventHandler<T> handler) where T : IEvent
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_asyncHandlers.ContainsKey(eventType))
            {
                _asyncHandlers[eventType] = new List<IAsyncEventHandler>();
            }
            
            _asyncHandlers[eventType].Add(handler);
        }
    }
    
    public async Task PublishAsync<T>(T evt) where T : IEvent
    {
        // 持久化事件（如果有事件存储）
        if (_eventStore != null && evt is IDomainEvent domainEvent)
        {
            var streamId = GetStreamId(domainEvent);
            await _eventStore.SaveEventsAsync(streamId, new[] { domainEvent });
        }
        
        var eventType = typeof(T);
        var tasks = new List<Task>();
        
        // 处理同步处理器
        if (_handlers.TryGetValue(eventType, out var syncHandlers))
        {
            foreach (var handler in syncHandlers)
            {
                try
                {
                    if (handler is IEventHandler<T> typedHandler)
                    {
                        typedHandler.Handle(evt);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断其他处理器
                    GD.PrintErr($"Error in event handler: {ex.Message}");
                }
            }
        }
        
        // 处理异步处理器
        if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlers))
        {
            foreach (var handler in asyncHandlers)
            {
                try
                {
                    if (handler is IAsyncEventHandler<T> typedHandler)
                    {
                        tasks.Add(typedHandler.HandleAsync(evt));
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error in async event handler: {ex.Message}");
                }
            }
        }
        
        // 等待所有异步处理器完成
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
    
    private string GetStreamId(IDomainEvent domainEvent)
    {
        // 根据领域事件类型生成流ID
        return domainEvent switch
        {
            PlayerCreatedDomainEvent evt => $"player-{evt.PlayerId}",
            PlayerLevelUpDomainEvent evt => $"player-{evt.PlayerId}",
            PlayerDiedDomainEvent evt => $"player-{evt.PlayerId}",
            _ => $"unknown-{Guid.NewGuid()}"
        };
    }
}

public interface IEventBus
{
    void Subscribe<T>(IEventHandler<T> handler) where T : IEvent;
    void SubscribeAsync<T>(IAsyncEventHandler<T> handler) where T : IEvent;
    Task PublishAsync<T>(T evt) where T : IEvent;
}

public interface IEventHandler<in T> where T : IEvent
{
    void Handle(T evt);
}

public interface IAsyncEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T evt);
}

// 事件处理器示例
public class PlayerEventHandler : IEventHandler<PlayerLevelUpEvent>,
                                 IAsyncEventHandler<PlayerDiedEvent>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly INotificationService _notificationService;
    
    public PlayerEventHandler(IPlayerRepository playerRepository, 
                            INotificationService notificationService)
    {
        _playerRepository = playerRepository;
        _notificationService = notificationService;
    }
    
    public void Handle(PlayerLevelUpEvent evt)
    {
        // 同步处理升级事件
        var player = _playerRepository.GetById(evt.PlayerId);
        if (player != null)
        {
            player.Level = evt.NewLevel;
            _playerRepository.Update(player);
            
            GD.Print($"Player {evt.PlayerName} leveled up to {evt.NewLevel}");
        }
    }
    
    public async Task HandleAsync(PlayerDiedEvent evt)
    {
        // 异步处理死亡事件
        var player = _playerRepository.GetById(evt.PlayerId);
        if (player != null)
        {
            player.IsAlive = false;
            player.DeathTime = DateTime.Now;
            _playerRepository.Update(player);
            
            // 发送通知
            await _notificationService.SendNotificationAsync(
                $"Player {evt.PlayerName} has died", 
                NotificationType.Warning
            );
            
            // 记录到外部系统
            await LogPlayerDeathToAnalyticsAsync(evt);
        }
    }
    
    private async Task LogPlayerDeathToAnalyticsAsync(PlayerDiedEvent evt)
    {
        // 发送分析数据
        await Task.Delay(100); // 模拟网络请求
        GD.Print($"Death analytics sent for player {evt.PlayerId}");
    }
}
```

## 插件系统

### 1. 插件架构

实现可扩展的插件系统：

```csharp
using System.Reflection;
using System.Collections.Concurrent;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }
    string[] Dependencies { get; }
    
    Task InitializeAsync(IPluginContext context);
    Task ShutdownAsync();
    bool IsCompatible(string frameworkVersion);
}

public interface IPluginContext
{
    IArchitecture Architecture { get; }
    IServiceContainer Services { get; }
    IEventManager Events { get; }
    ILogger Logger { get; }
    
    void RegisterService<T>(T service) where T : class;
    T GetService<T>() where T : class;
    void RegisterEventHandler<T>(IEventHandler<T> handler) where T : IEvent;
}

public class PluginManager : IPluginContext
{
    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
    private readonly ConcurrentDictionary<Type, object> _services = new();
    private readonly List<IEventHandler> _eventHandlers = new();
    private readonly IArchitecture _architecture;
    private readonly ILogger _logger;
    
    public IArchitecture Architecture => _architecture;
    public IServiceContainer Services => this;
    public IEventManager Events => _architecture.Context;
    public ILogger Logger => _logger;
    
    public PluginManager(IArchitecture architecture, ILogger logger)
    {
        _architecture = architecture;
        _logger = logger;
    }
    
    public async Task LoadPluginAsync(string pluginPath)
    {
        try
        {
            _logger.Info($"Loading plugin from: {pluginPath}");
            
            // 加载程序集
            var assembly = Assembly.LoadFrom(pluginPath);
            
            // 查找插件类型
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
            foreach (var pluginType in pluginTypes)
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                await LoadPluginInstanceAsync(plugin);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load plugin from {pluginPath}: {ex.Message}");
            throw;
        }
    }
    
    private async Task LoadPluginInstanceAsync(IPlugin plugin)
    {
        // 检查兼容性
        if (!plugin.IsCompatible(GetCurrentFrameworkVersion()))
        {
            throw new IncompatiblePluginException(
                $"Plugin {plugin.Name} v{plugin.Version} is not compatible with framework version {GetCurrentFrameworkVersion()}"
            );
        }
        
        // 检查依赖
        foreach (var dependency in plugin.Dependencies)
        {
            if (!_loadedPlugins.ContainsKey(dependency))
            {
                throw new MissingDependencyException(
                    $"Plugin {plugin.Name} requires plugin {dependency} to be loaded first"
                );
            }
        }
        
        // 初始化插件
        await plugin.InitializeAsync(this);
        _loadedPlugins[plugin.Name] = plugin;
        
        _logger.Info($"Plugin {plugin.Name} v{plugin.Version} loaded successfully");
    }
    
    public async Task UnloadPluginAsync(string pluginName)
    {
        if (!_loadedPlugins.TryGetValue(pluginName, out var plugin))
        {
            throw new PluginNotFoundException($"Plugin {pluginName} is not loaded");
        }
        
        try
        {
            // 检查是否有其他插件依赖此插件
            var dependentPlugins = _loadedPlugins.Values
                .Where(p => p.Dependencies.Contains(pluginName))
                .ToList();
                
            if (dependentPlugins.Any())
            {
                var dependentNames = string.Join(", ", dependentPlugins.Select(p => p.Name));
                throw new DependencyException(
                    $"Cannot unload plugin {pluginName} because the following plugins depend on it: {dependentNames}"
                );
            }
            
            // 关闭插件
            await plugin.ShutdownAsync();
            _loadedPlugins.Remove(pluginName);
            
            _logger.Info($"Plugin {pluginName} unloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to unload plugin {pluginName}: {ex.Message}");
            throw;
        }
    }
    
    public IEnumerable<IPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.ToList();
    }
    
    public IPlugin GetPlugin(string name)
    {
        return _loadedPlugins.TryGetValue(name, out var plugin) ? plugin : null;
    }
    
    // IPluginContext 实现
    public void RegisterService<T>(T service) where T : class
    {
        _services.TryAdd(typeof(T), service);
        _logger.Debug($"Service {typeof(T).Name} registered by plugin system");
    }
    
    public T GetService<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        
        // 尝试从架构中获取
        return _architecture.Context.GetUtility<T>();
    }
    
    public void RegisterEventHandler<T>(IEventHandler<T> handler) where T : IEvent
    {
        _eventHandlers.Add(handler);
        _architecture.Context.RegisterEvent<T>(handler.Handle);
        _logger.Debug($"Event handler for {typeof(T).Name} registered by plugin system");
    }
    
    private string GetCurrentFrameworkVersion()
    {
        return "1.0.0"; // 应该从配置或程序集中读取
    }
}

// 插件示例
public class ChatPlugin : IPlugin
{
    public string Name => "Chat";
    public string Version => "1.2.0";
    public string Description => "In-game chat system with moderation features";
    public string Author => "GameStudio";
    public string[] Dependencies => new[] { "Authentication" };
    
    private IChatService _chatService;
    private ILogger _logger;
    
    public async Task InitializeAsync(IPluginContext context)
    {
        _logger = context.Logger;
        
        // 创建聊天服务
        _chatService = new ChatService(context.Architecture);
        
        // 注册服务
        context.RegisterService<IChatService>(_chatService);
        
        // 注册事件处理器
        context.RegisterEventHandler<PlayerJoinEvent>(OnPlayerJoin);
        context.RegisterEventHandler<PlayerLeaveEvent>(OnPlayerLeave);
        context.RegisterEventHandler<ChatMessageEvent>(OnChatMessage);
        
        _logger.Info("Chat plugin initialized");
        
        await Task.CompletedTask;
    }
    
    public async Task ShutdownAsync()
    {
        _chatService?.Dispose();
        _logger.Info("Chat plugin shutdown");
        await Task.CompletedTask;
    }
    
    public bool IsCompatible(string frameworkVersion)
    {
        // 检查版本兼容性
        return Version.TryParse(frameworkVersion, out var version) && 
               version >= new Version(1, 0, 0);
    }
    
    private void OnPlayerJoin(PlayerJoinEvent evt)
    {
        _chatService.SendSystemMessage($"{evt.PlayerName} joined the game");
    }
    
    private void OnPlayerLeave(PlayerLeaveEvent evt)
    {
        _chatService.SendSystemMessage($"{evt.PlayerName} left the game");
    }
    
    private void OnChatMessage(ChatMessageEvent evt)
    {
        _chatService.SendMessage(evt.PlayerId, evt.Message, evt.Channel);
    }
}

public interface IChatService
{
    void SendMessage(string playerId, string message, string channel = "global");
    void SendSystemMessage(string message);
    void Dispose();
}

public class ChatService : IChatService
{
    private readonly IArchitecture _architecture;
    private readonly Dictionary<string, ChatChannel> _channels = new();
    
    public ChatService(IArchitecture architecture)
    {
        _architecture = architecture;
        InitializeChannels();
    }
    
    private void InitializeChannels()
    {
        _channels["global"] = new ChatChannel("global", "Global Chat");
        _channels["trade"] = new ChatChannel("trade", "Trade Chat");
        _channels["guild"] = new ChatChannel("guild", "Guild Chat");
    }
    
    public void SendMessage(string playerId, string message, string channel = "global")
    {
        if (!_channels.TryGetValue(channel, out var chatChannel))
        {
            throw new ArgumentException($"Unknown channel: {channel}");
        }
        
        var chatMessage = new ChatMessage
        {
            PlayerId = playerId,
            Message = FilterMessage(message),
            Timestamp = DateTime.Now,
            Channel = channel
        };
        
        chatChannel.AddMessage(chatMessage);
        
        // 发送事件
        _architecture.Context.SendEvent(new ChatMessageReceivedEvent(chatMessage));
    }
    
    public void SendSystemMessage(string message)
    {
        var systemMessage = new ChatMessage
        {
            PlayerId = "system",
            Message = message,
            Timestamp = DateTime.Now,
            Channel = "system"
        };
        
        _channels["global"].AddMessage(systemMessage);
        _architecture.Context.SendEvent(new ChatMessageReceivedEvent(systemMessage));
    }
    
    private string FilterMessage(string message)
    {
        // 实现聊天过滤逻辑
        return message;
    }
    
    public void Dispose()
    {
        _channels.Clear();
    }
}

public class ChatChannel
{
    public string Name { get; }
    public string Description { get; }
    public Queue<ChatMessage> Messages { get; } = new();
    public int MaxMessages { get; set; } = 100;
    
    public ChatChannel(string name, string description)
    {
        Name = name;
        Description = description;
    }
    
    public void AddMessage(ChatMessage message)
    {
        Messages.Enqueue(message);
        
        // 限制消息数量
        while (Messages.Count > MaxMessages)
        {
            Messages.Dequeue();
        }
    }
}

public record ChatMessage
{
    public string PlayerId { get; init; }
    public string Message { get; init; }
    public DateTime Timestamp { get; init; }
    public string Channel { get; init; }
}

public struct ChatMessageReceivedEvent
{
    public ChatMessage Message { get; init; }
}
```

## 网络集成

### 1. 网络架构

实现基于事件的网络系统：

```csharp
using Godot;
using System.Net.WebSockets;

public class NetworkManager : Node, INetworkManager
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentQueue<NetworkMessage> _messageQueue = new();
    private readonly Dictionary<string, Type> _messageTypes = new();
    private bool _isConnected = false;
    
    [Signal]
    public delegate void ConnectedEventHandler();
    
    [Signal]
    public delegate void DisconnectedEventHandler(string reason);
    
    [Signal]
    public delegate void MessageReceivedEventHandler(NetworkMessage message);
    
    [Signal]
    public delegate void ConnectionFailedEventHandler(string error);
    
    public override void _Ready()
    {
        RegisterMessageTypes();
        SetProcess(true);
    }
    
    private void RegisterMessageTypes()
    {
        _messageTypes["player_position"] = typeof(PlayerPositionMessage);
        _messageTypes["chat_message"] = typeof(ChatMessageMessage);
        _messageTypes["player_action"] = typeof(PlayerActionMessage);
        _messageTypes["game_state"] = typeof(GameStateMessage);
    }
    
    public async Task ConnectAsync(string url)
    {
        try
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            
            GD.Print($"Connecting to {url}");
            await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
            
            _isConnected = true;
            EmitSignal(SignalName.Connected);
            
            // 启动消息接收循环
            _ = Task.Run(ReceiveMessagesLoop);
            
            GD.Print("Connected to server");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Connection failed: {ex.Message}");
            EmitSignal(SignalName.ConnectionFailed, ex.Message);
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            _isConnected = false;
            _cancellationTokenSource?.Cancel();
            
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error during disconnect: {ex.Message}");
            }
            
            EmitSignal(SignalName.Disconnected, "Manual disconnect");
        }
    }
    
    public async Task SendMessageAsync(NetworkMessage message)
    {
        if (!_isConnected || _webSocket?.State != WebSocketState.Open)
        {
            GD.PrintErr("Not connected to server");
            return;
        }
        
        try
        {
            var json = Json.Stringify(message.Serialize());
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to send message: {ex.Message}");
        }
    }
    
    private async Task ReceiveMessagesLoop()
    {
        var buffer = new byte[4096];
        
        while (_isConnected && _cancellationTokenSource?.Token.IsCancellationRequested == false)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), 
                    _cancellationTokenSource.Token);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var messageData = Json.ParseString(json);
                    var message = ParseMessage(messageData);
                    
                    if (message != null)
                    {
                        _messageQueue.Enqueue(message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _isConnected = false;
                    EmitSignal(SignalName.Disconnected, "Server closed connection");
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // 正常的取消操作
                break;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error receiving message: {ex.Message}");
                _isConnected = false;
                EmitSignal(SignalName.Disconnected, ex.Message);
                break;
            }
        }
    }
    
    private NetworkMessage ParseMessage(Godot.Collections.Dictionary messageData)
    {
        if (!messageData.ContainsKey("type"))
        {
            GD.PrintErr("Message missing type field");
            return null;
        }
        
        var messageType = messageData["type"].ToString();
        if (!_messageTypes.TryGetValue(messageType, out var type))
        {
            GD.PrintErr($"Unknown message type: {messageType}");
            return null;
        }
        
        var message = (NetworkMessage)Activator.CreateInstance(type);
        message.Deserialize(messageData);
        
        return message;
    }
    
    public override void _Process(double delta)
    {
        // 处理接收到的消息
        while (_messageQueue.TryDequeue(out var message))
        {
            EmitSignal(SignalName.MessageReceived, message);
            HandleNetworkMessage(message);
        }
    }
    
    private void HandleNetworkMessage(NetworkMessage message)
    {
        // 根据消息类型处理网络消息
        switch (message)
        {
            case PlayerPositionMessage posMsg:
                HandlePlayerPosition(posMsg);
                break;
            case ChatMessageMessage chatMsg:
                HandleChatMessage(chatMsg);
                break;
            case PlayerActionMessage actionMsg:
                HandlePlayerAction(actionMsg);
                break;
            case GameStateMessage stateMsg:
                HandleGameState(stateMsg);
                break;
        }
    }
    
    private void HandlePlayerPosition(PlayerPositionMessage message)
    {
        // 更新其他玩家位置
        Context.SendEvent(new NetworkPlayerPositionEvent
        {
            PlayerId = message.PlayerId,
            Position = new Vector2(message.X, message.Y)
        });
    }
    
    private void HandleChatMessage(ChatMessageMessage message)
    {
        // 显示聊天消息
        Context.SendEvent(new NetworkChatEvent
        {
            PlayerName = message.PlayerName,
            Message = message.Content,
            Channel = message.Channel
        });
    }
    
    private void HandlePlayerAction(PlayerActionMessage message)
    {
        // 处理玩家动作
        Context.SendEvent(new NetworkPlayerActionEvent
        {
            PlayerId = message.PlayerId,
            Action = message.Action,
            Data = message.Data
        });
    }
    
    private void HandleGameState(GameStateMessage message)
    {
        // 更新游戏状态
        Context.SendEvent(new NetworkGameStateEvent
        {
            State = message.State,
            Data = message.Data
        });
    }
    
    public override void _ExitTree()
    {
        _cancellationTokenSource?.Cancel();
        _webSocket?.Dispose();
        base._ExitTree();
    }
}

// 网络消息基类
public abstract class NetworkMessage
{
    public abstract string Type { get; }
    
    public abstract Godot.Collections.Dictionary Serialize();
    public abstract void Deserialize(Godot.Collections.Dictionary data);
}

// 具体网络消息
public class PlayerPositionMessage : NetworkMessage
{
    public string PlayerId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Rotation { get; set; }
    
    public override string Type => "player_position";
    
    public override Godot.Collections.Dictionary Serialize()
    {
        return new Godot.Collections.Dictionary
        {
            ["type"] = Type,
            ["player_id"] = PlayerId,
            ["x"] = X,
            ["y"] = Y,
            ["rotation"] = Rotation
        };
    }
    
    public override void Deserialize(Godot.Collections.Dictionary data)
    {
        PlayerId = data["player_id"].ToString();
        X = (float)data["x"];
        Y = (float)data["y"];
        Rotation = (float)data["rotation"];
    }
}

// 网络事件
public struct NetworkPlayerPositionEvent
{
    public string PlayerId { get; set; }
    public Vector2 Position { get; set; }
}

public struct NetworkChatEvent
{
    public string PlayerName { get; set; }
    public string Message { get; set; }
    public string Channel { get; set; }
}

// 网络控制器
[ContextAware]
[Log]
public partial class NetworkController : Node, IController
{
    private NetworkManager _networkManager;
    
    public override void _Ready()
    {
        _networkManager = new NetworkManager();
        AddChild(_networkManager);
        
        // 连接网络事件
        _networkManager.Connected += OnNetworkConnected;
        _networkManager.Disconnected += OnNetworkDisconnected;
        _networkManager.MessageReceived += OnNetworkMessageReceived;
        _networkManager.ConnectionFailed += OnConnectionFailed;
    }
    
    public async Task ConnectToServer(string url)
    {
        Logger.Info($"Connecting to server: {url}");
        await _networkManager.ConnectAsync(url);
    }
    
    public void SendPlayerPosition(Vector2 position)
    {
        var message = new PlayerPositionMessage
        {
            PlayerId = Context.GetModel<PlayerModel>().PlayerId,
            X = position.X,
            Y = position.Y,
            Rotation = 0f // 根据实际需要设置
        };
        
        _networkManager.SendMessageAsync(message);
    }
    
    public void SendChatMessage(string message, string channel = "global")
    {
        var chatMessage = new ChatMessageMessage
        {
            PlayerName = Context.GetModel<PlayerModel>().Name,
            Content = message,
            Channel = channel
        };
        
        _networkManager.SendMessageAsync(chatMessage);
    }
    
    private void OnNetworkConnected()
    {
        Logger.Info("Connected to network server");
        Context.SendEvent(new NetworkConnectedEvent());
    }
    
    private void OnNetworkDisconnected(string reason)
    {
        Logger.Info($"Disconnected from network server: {reason}");
        Context.SendEvent(new NetworkDisconnectedEvent { Reason = reason });
    }
    
    private void OnNetworkMessageReceived(NetworkMessage message)
    {
        Logger.Debug($"Received network message: {message.Type}");
    }
    
    private void OnConnectionFailed(string error)
    {
        Logger.Error($"Network connection failed: {error}");
        Context.SendEvent(new NetworkConnectionFailedEvent { Error = error });
    }
}
```

---

## 总结

通过本高级模式教程，你已经学会了：

- ✅ **CQRS 模式** - 分离命令和查询职责
- ✅ **领域驱动设计** - 构建丰富的领域模型
- ✅ **事件溯源** - 持久化和重放领域事件
- ✅ **插件系统** - 可扩展的插件架构
- ✅ **网络集成** - 实时多人游戏网络支持

这些高级模式将帮助你构建企业级的大型游戏系统。

---

**教程版本**: 1.0.0  
**更新日期**: 2026-01-12
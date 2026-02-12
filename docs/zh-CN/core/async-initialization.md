# 异步初始化指南

## 概述

异步初始化是 GFramework 中的一个重要特性，允许 Architecture、Model、System 等组件在初始化时执行异步操作。这对于需要加载大量资源、进行网络请求或其他耗时操作的场景特别有用。

## 核心接口

### IAsyncInitializable

异步初始化接口，定义了异步初始化的契约。

**核心方法：**

```csharp
Task InitializeAsync();  // 异步初始化方法
```

**实现者：**

- `Architecture` - 架构支持异步初始化
- `AbstractModel` - Model 可以实现异步初始化
- `AbstractSystem` - System 可以实现异步初始化
- `AbstractUtility` - Utility 可以实现异步初始化

## 异步 Architecture

### 基本使用

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 同步初始化逻辑
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
    }
}

// 异步初始化架构
var architecture = new GameArchitecture();
await architecture.InitializeAsync();

// 等待架构就绪
await architecture.WaitUntilReadyAsync();
```

### 初始化流程

异步初始化遵循以下流程：

1. 创建架构实例
2. 调用 `InitializeAsync()` 方法
3. 执行同步初始化（`Init()` 方法）
4. 按顺序异步初始化各个阶段：
    - 工具异步初始化（BeforeUtilityInit → AfterUtilityInit）
    - 模型异步初始化（BeforeModelInit → AfterModelInit）
    - 系统异步初始化（BeforeSystemInit → AfterSystemInit）
5. 进入 Ready 阶段
6. 返回 Task 完成

##

### 定义异步 Model

```csharp
public class ConfigModel : AbstractModel, IAsyncInitializable
{
    public BindableProperty<GameConfig> Config { get; } = new(null);

    protected override void OnInit()
    {
        // 同步初始化逻辑
        Console.WriteLine("ConfigModel sync init");
    }

    public async Task InitializeAsync()
    {
        // 异步加载配置
        var storage = this.GetUtility<IStorageUtility>();
        var config = await storage.LoadConfigAsync();
        Config.Value = config;

        Console.WriteLine("ConfigModel async init completed");
        this.SendEvent(new ConfigLoadedEvent { Config = config });
    }
}

public class PlayerDataModel : AbstractModel, IAsyncInitializable
{
    public BindableProperty<PlayerData> PlayerData { get; } = new(null);

    protected override void OnInit()
    {
        // 同步初始化逻辑
    }

    public async Task InitializeAsync()
    {
        // 异步加载玩家数据
        var storage = this.GetUtility<IStorageUtility>();
        var playerId = this.GetContext().Environment.Get<string>("PlayerId");
        var playerData = await storage.LoadPlayerDataAsync(playerId);
        PlayerData.Value = playerData;
    }
}
```

### 在架构中使用异步 Model

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册异步 Model
        RegisterModel(new ConfigModel());
        RegisterModel(new PlayerDataModel());
        RegisterModel(new PlayerModel());

        // 注册 System
        RegisterSystem(new CombatSystem());
    }
}

// 使用
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
```

## 异步 System

### 定义异步 System

```csharp
public class DataLoadSystem : AbstractSystem, IAsyncInitializable
{
    private GameData _gameData;

    protected override void OnInit()
    {
        // 同步初始化逻辑
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    public async Task InitializeAsync()
    {
        // 异步加载游戏数据
        var storage = this.GetUtility<IStorageUtility>();
        _gameData = await storage.LoadGameDataAsync();

        Console.WriteLine("Game data loaded");
        this.SendEvent(new GameDataLoadedEvent { Data = _gameData });
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        Console.WriteLine($"Game started with data version: {_gameData.Version}");
    }
}

public class ResourceLoadSystem : AbstractSystem, IAsyncInitializable
{
    private Dictionary<string, Resource> _resources = new();

    protected override void OnInit()
    {
        // 同步初始化逻辑
    }

    public async Task InitializeAsync()
    {
        // 异步加载资源
        var resourceManager = this.GetUtility<IResourceManager>();
        _resources = await resourceManager.LoadAllResourcesAsync();

        Console.WriteLine($"Loaded {_resources.Count} resources");
    }
}
```

## 异步 Utility

### 定义异步 Utility

```csharp
public class DatabaseUtility : IUtility, IAsyncInitializable
{
    private Database _database;

    public void Init()
    {
        // 同步初始化逻辑
    }

    public async Task InitializeAsync()
    {
        // 异步连接数据库
        _database = new Database();
        await _database.ConnectAsync("connection_string");

        Console.WriteLine("Database connected");
    }

    public async Task<T> QueryAsync<T>(string sql)
    {
        return await _database.QueryAsync<T>(sql);
    }
}
```

## 完整示例

### 场景：游戏启动流程

```csharp
// 1. 定义各个异步组件
public class ConfigModel : AbstractModel, IAsyncInitializable
{
    public BindableProperty<GameConfig> Config { get; } = new(null);

    protected override void OnInit() { }

    public async Task InitializeAsync()
    {
        var storage = this.GetUtility<IStorageUtility>();
        Config.Value = await storage.LoadConfigAsync();
    }
}

public class PlayerModel : AbstractModel, IAsyncInitializable
{
    public BindableProperty<PlayerData> PlayerData { get; } = new(null);

    protected override void OnInit() { }

    public async Task InitializeAsync()
    {
        var storage = this.GetUtility<IStorageUtility>();
        var playerId = this.GetContext().Environment.Get<string>("PlayerId");
        PlayerData.Value = await storage.LoadPlayerDataAsync(playerId);
    }
}

public class ResourceLoadSystem : AbstractSystem, IAsyncInitializable
{
    private Dictionary<string, Resource> _resources = new();

    protected override void OnInit()
    {
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    public async Task InitializeAsync()
    {
        var resourceManager = this.GetUtility<IResourceManager>();
        _resources = await resourceManager.LoadAllResourcesAsync();
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        Console.WriteLine("Game started with all resources loaded");
    }
}

// 2. 定义架构
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new ConfigModel());
        RegisterModel(new PlayerModel());
        RegisterSystem(new ResourceLoadSystem());
        RegisterSystem(new CombatSystem());
    }
}

// 3. 使用架构
public class GameBootstrapper
{
    public async Task StartGameAsync()
    {
        var architecture = new GameArchitecture();

        // 异步初始化架构
        await architecture.InitializeAsync();

        // 等待架构就绪
        await architecture.WaitUntilReadyAsync();

        Console.WriteLine("Game is ready!");

        // 发送游戏启动事件
        architecture.SendEvent(new GameStartedEvent());
    }
}
```

## 同步 vs 异步初始化

### 同步初始化

```csharp
var architecture = new GameArchitecture();
architecture.Initialize();  // 阻塞等待初始化完成

// 立即可以使用架构
var playerModel = architecture.GetModel<PlayerModel>();
```

**特点：**

- 阻塞式，等待初始化完成
- 适用于简单场景或控制台应用
- 初始化失败时抛出异常

### 异步初始化

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync();  // 非阻塞等待

// 初始化完成后可以使用架构
var playerModel = architecture.GetModel<PlayerModel>();
```

**特点：**

- 非阻塞式，不会阻塞主线程
- 支持异步组件初始化
- 适用于需要加载大量资源的场景
- 初始化失败时抛出异常

## 最佳实践

### 1. 合理划分同步和异步初始化

```csharp
public class MyModel : AbstractModel, IAsyncInitializable
{
    protected override void OnInit()
    {
        // 同步初始化：快速的初始化逻辑
        // - 注册事件监听
        // - 初始化本地数据结构
        // - 设置默认值
    }

    public async Task InitializeAsync()
    {
        // 异步初始化：耗时的初始化逻辑
        // - 加载文件
        // - 网络请求
        // - 数据库查询
    }
}
```

### 2. 处理异步初始化异常

```csharp
public async Task StartGameAsync()
{
    var architecture = new GameArchitecture();

    try
    {
        await architecture.InitializeAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Initialization failed: {ex.Message}");
        // 处理初始化失败
    }
}
```

### 3. 显示加载进度

```csharp
public class ProgressTrackingArchitecture : Architecture
{
    public event Action<float> OnProgressChanged;

    protected override void Init()
    {
        RegisterModel(new ConfigModel());
        RegisterModel(new PlayerModel());
        RegisterSystem(new ResourceLoadSystem());
    }

    public async Task InitializeAsyncWithProgress()
    {
        OnProgressChanged?.Invoke(0.0f);

        // 初始化各个阶段
        await InitializeAsync();

        OnProgressChanged?.Invoke(1.0f);
    }
}

// 使用
var architecture = new ProgressTrackingArchitecture();
architecture.OnProgressChanged += progress =>
{
    Console.WriteLine($"Loading: {progress * 100}%");
};

await architecture.InitializeAsyncWithProgress();
```

### 4. 超时控制

```csharp
public async Task StartGameWithTimeoutAsync()
{
    var architecture = new GameArchitecture();
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        var initTask = architecture.InitializeAsync();
        await initTask.ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Initialization timeout");
    }
}
```

## 相关包

- [`architecture`](./architecture.md) - 架构核心，支持异步初始化
- [`model`](./model.md) - Model 可以实现异步初始化
- [`system`](./system.md) - System 可以实现异步初始化
- [`utility`](./utility.md) - Utility 可以实现异步初始化

---

**许可证**：Apache 2.0

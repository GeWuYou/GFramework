# 故障排除与调试

本指南帮助你诊断和解决 GFramework 使用中的常见问题。

## 如何使用本指南

1. **快速查找**：使用浏览器的搜索功能（Ctrl+F / Cmd+F）查找错误信息或关键词
2. **分类浏览**：根据问题类型（安装、架构、事件等）查看对应章节
3. **排查清单**：每个问题都提供了详细的排查步骤和解决方案
4. **代码示例**：所有解决方案都包含可运行的代码示例

## 安装问题

### NuGet 包安装失败

**问题**：无法安装 GFramework NuGet 包。

**错误信息**：

```
NU1101: Unable to find package GFramework.Core
```

**解决方案**：

1. **检查包源配置**：

```bash
# 查看当前包源
dotnet nuget list source

# 添加 NuGet.org 源
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

2. **清理 NuGet 缓存**：

```bash
dotnet nuget locals all --clear
```

3. **手动指定版本**：

```bash
dotnet add package GFramework.Core --version 1.0.0
```

### 依赖冲突

**问题**：安装 GFramework 后出现依赖版本冲突。

**错误信息**：

```
NU1107: Version conflict detected for Microsoft.Extensions.DependencyInjection
```

**解决方案**：

1. **检查项目文件**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 确保使用兼容的版本 -->
    <PackageReference Include="GFramework.Core" Version="1.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>
</Project>
```

2. **统一依赖版本**：

```xml
<ItemGroup>
  <!-- 在 Directory.Build.props 中统一管理版本 -->
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
</ItemGroup>
```

### .NET 版本不兼容

**问题**：项目无法编译，提示 .NET 版本不兼容。

**错误信息**：

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0
```

**解决方案**：

1. **检查 .NET SDK 版本**：

```bash
dotnet --version
```

2. **安装正确的 .NET SDK**：

- GFramework 需要 .NET 8.0 或更高版本
- 下载地址：https://dotnet.microsoft.com/download

3. **更新项目目标框架**：

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

## 架构问题

### 架构初始化失败

**问题**：架构初始化时抛出异常。

**错误信息**：

```
InvalidOperationException: Architecture is already initialized
```

**原因**：重复调用 `Initialize()` 方法。

**解决方案**：

```csharp
// ❌ 错误：重复初始化
var arch = new GameArchitecture();
arch.Initialize();
arch.Initialize(); // 抛出异常

// ✅ 正确：只初始化一次
var arch = new GameArchitecture();
if (!arch.IsInitialized)
{
    arch.Initialize();
}

// ✅ 更好：使用单例模式
public class GameArchitecture : Architecture&lt;GameArchitecture&gt;
{
    protected override void Init()
    {
        // 注册组件
    }
}

// 使用
var arch = GameArchitecture.Interface;
```

### 服务未注册

**问题**：尝试获取未注册的服务。

**错误信息**：

```
InvalidOperationException: No service for type 'IPlayerService' has been registered
```

**解决方案**：

```csharp
// ❌ 错误：未注册服务
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 忘记注册 IPlayerService
    }
}

var service = arch.GetService&lt;IPlayerService&gt;(); // 抛出异常

// ✅ 正确：先注册服务
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册服务
        RegisterService&lt;IPlayerService, PlayerService&gt;();
    }
}

// ✅ 使用 IoC 容器注册
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton&lt;IPlayerService, PlayerService&gt;();
    services.AddTransient&lt;IGameService, GameService&gt;();
}
```

### 组件注册顺序错误

**问题**：组件初始化时依赖的其他组件尚未注册。

**错误信息**：

```
NullReferenceException: Object reference not set to an instance of an object
```

**解决方案**：

```csharp
// ❌ 错误：SystemB 依赖 ModelA，但 ModelA 后注册
protected override void Init()
{
    RegisterSystem(new SystemB()); // SystemB.OnInit() 中访问 ModelA 失败
    RegisterModel(new ModelA());
}

// ✅ 正确：先注册依赖项
protected override void Init()
{
    // 1. 先注册 Model
    RegisterModel(new ModelA());

    // 2. 再注册 System
    RegisterSystem(new SystemB());

    // 3. 最后注册 Utility
    RegisterUtility&lt;IConfigUtility&gt;(new ConfigUtility());
}

// ✅ 更好：使用延迟初始化
public class SystemB : AbstractSystem
{
    private ModelA _modelA;

    protected override void OnInit()
    {
        // 在 OnInit 中获取依赖，此时所有组件已注册
        _modelA = this.GetModel&lt;ModelA&gt;();
    }
}
```

### 异步初始化问题

**问题**：异步架构初始化未正确等待。

**错误信息**：

```
InvalidOperationException: Architecture not fully initialized
```

**解决方案**：

```csharp
// ❌ 错误：未等待异步初始化完成
var arch = new AsyncGameArchitecture();
arch.InitializeAsync(); // 未等待
var model = arch.GetModel&lt;PlayerModel&gt;(); // 可能失败

// ✅ 正确：等待异步初始化
var arch = new AsyncGameArchitecture();
await arch.InitializeAsync();
var model = arch.GetModel&lt;PlayerModel&gt;(); // 安全

// ✅ 在 Godot 中使用
public partial class GameRoot : Node
{
    private AsyncGameArchitecture _architecture;

    public override async void _Ready()
    {
        _architecture = new AsyncGameArchitecture();
        await _architecture.InitializeAsync();

        // 现在可以安全使用架构
        var model = _architecture.GetModel&lt;PlayerModel&gt;();
    }
}
```

## 常见错误

### 1. 组件未注册错误

**错误信息**：`KeyNotFoundException: 未找到类型为 'XXX' 的组件`

**原因**：尝试获取未注册的组件。

**解决方案**：

```csharp
// ❌ 错误：未注册 PlayerModel
var arch = new GameArchitecture();
arch.Initialize();
var model = arch.GetModel<PlayerModel>(); // 抛出异常

// ✅ 正确：先注册再获取
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel()); // 注册模型
    }
}
```

### 2. 事件监听器未触发

**问题**：注册了事件监听器但没有被调用。

**原因**：

- 事件类型不匹配
- 监听器在事件发送前注销
- 事件发送时使用了错误的类型
- 事件传播被停止

**解决方案**：

```csharp
// ❌ 错误：事件类型不匹配
this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);
this.SendEvent(new PlayerAttackedEvent()); // 不会触发

// ✅ 正确：事件类型匹配
this.RegisterEvent&lt;PlayerAttackedEvent&gt;(OnPlayerAttacked);
this.SendEvent(new PlayerAttackedEvent()); // 正确触发

// ❌ 错误：过早注销
var unregister = this.RegisterEvent&lt;GameEvent&gt;(OnGameEvent);
unregister.UnRegister(); // 立即注销
this.SendEvent(new GameEvent()); // 不会触发

// ✅ 正确：在适当时机注销
private IUnRegister _eventUnregister;

public void Initialize()
{
    _eventUnregister = this.RegisterEvent&lt;GameEvent&gt;(OnGameEvent);
}

public void Cleanup()
{
    _eventUnregister?.UnRegister();
}

// ❌ 错误：事件传播被停止
this.RegisterEvent&lt;GameEvent&gt;(e =>
{
    e.StopPropagation(); // 停止传播
});

this.RegisterEvent&lt;GameEvent&gt;(OnGameEvent); // 不会被调用

// ✅ 正确：使用优先级控制执行顺序
this.RegisterEvent&lt;GameEvent&gt;(OnGameEventFirst, priority: 100);
this.RegisterEvent&lt;GameEvent&gt;(OnGameEventSecond, priority: 50);
```

### 3. 内存泄漏

**问题**：应用内存持续增长。

**原因**：

- 未注销事件监听器
- 未注销属性监听器
- 未销毁 Architecture
- 循环引用

**解决方案**：

```csharp
// ✅ 正确：使用 UnRegisterList 管理注销
private IUnRegisterList _unregisterList = new UnRegisterList();

public void Initialize()
{
    this.RegisterEvent&lt;Event1&gt;(OnEvent1)
        .AddToUnregisterList(_unregisterList);

    model.Property.Register(OnPropertyChanged)
        .AddToUnregisterList(_unregisterList);
}

public void Cleanup()
{
    _unregisterList.UnRegisterAll();
}

// ✅ 销毁架构
architecture.Destroy();

// ✅ 使用 EventListenerScope 自动管理生命周期
public partial class Player : Node
{
    private EventListenerScope _eventScope;

    public override void _Ready()
    {
        _eventScope = new EventListenerScope(this.GetArchitecture());

        _eventScope.Register&lt;PlayerDiedEvent&gt;(OnPlayerDied);
        _eventScope.Register&lt;PlayerAttackedEvent&gt;(OnPlayerAttacked);
    }

    public override void _ExitTree()
    {
        _eventScope.Dispose(); // 自动注销所有监听器
    }
}

// ✅ 在 Godot 中使用 UnRegisterOnFree
public partial class GameController : Node
{
    public override void _Ready()
    {
        this.RegisterEvent&lt;GameEvent&gt;(OnGameEvent)
            .UnRegisterOnFree(this); // 节点释放时自动注销
    }
}
```

### 4. 循环依赖

**问题**：两个系统相互依赖导致死循环。

**原因**：系统间直接调用而不是通过事件通信。

**解决方案**：

```csharp
// ❌ 错误：直接调用导致循环依赖
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        var systemB = this.GetSystem&lt;SystemB&gt;();
        systemB.DoSomething(); // 可能导致循环
    }
}

// ✅ 正确：使用事件解耦
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        this.SendEvent(new EventB()); // 发送事件
    }
}

public class SystemB : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent&lt;EventB&gt;(OnEventB);
    }
}

// ✅ 使用 Command 模式
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        this.SendCommand(new ProcessDataCommand());
    }
}

// ✅ 使用 Query 模式获取数据
public class SystemA : AbstractSystem
{
    private void ProcessData()
    {
        var data = this.SendQuery(new GetPlayerDataQuery());
        // 处理数据
    }
}
```

## 事件系统问题

### 事件未触发

**问题**：发送事件后没有任何监听器响应。

**排查步骤**：

1. **检查事件类型是否正确**：

```csharp
// 确保发送和监听的是同一个事件类型
public struct PlayerDiedEvent : IEvent { }

// 注册
this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);

// 发送
this.SendEvent(new PlayerDiedEvent()); // 类型必须完全匹配
```

2. **检查是否在架构初始化后注册**：

```csharp
// ❌ 错误：在架构初始化前注册
var arch = new GameArchitecture();
arch.RegisterEvent&lt;GameEvent&gt;(OnGameEvent); // 可能失败

// ✅ 正确：在架构初始化后注册
var arch = new GameArchitecture();
arch.Initialize();
arch.RegisterEvent&lt;GameEvent&gt;(OnGameEvent);
```

3. **检查事件总线是否正确注册**：

```csharp
protected override void Init()
{
    // 确保注册了事件总线
    RegisterSystem(new EventBusModule());
}
```

### 事件执行顺序错误

**问题**：多个监听器的执行顺序不符合预期。

**解决方案**：

```csharp
// ✅ 使用优先级控制执行顺序（数值越大优先级越高）
this.RegisterEvent&lt;GameEvent&gt;(OnGameEventFirst, priority: 100);
this.RegisterEvent&lt;GameEvent&gt;(OnGameEventSecond, priority: 50);
this.RegisterEvent&lt;GameEvent&gt;(OnGameEventLast, priority: 0);

// ✅ 使用事件链
public struct FirstEvent : IEvent { }
public struct SecondEvent : IEvent { }
public struct ThirdEvent : IEvent { }

this.RegisterEvent&lt;FirstEvent&gt;(e =>
{
    // 处理第一个事件
    this.SendEvent(new SecondEvent()); // 触发下一个事件
});

this.RegisterEvent&lt;SecondEvent&gt;(e =>
{
    // 处理第二个事件
    this.SendEvent(new ThirdEvent()); // 触发下一个事件
});
```

### 事件传播控制

**问题**：需要在某些条件下停止事件传播。

**解决方案**：

```csharp
// ✅ 使用 StopPropagation
this.RegisterEvent&lt;DamageEvent&gt;(e =>
{
    if (IsInvincible)
    {
        e.StopPropagation(); // 停止传播，后续监听器不会执行
        return;
    }

    ApplyDamage(e.Amount);
}, priority: 100); // 高优先级先执行

// ✅ 使用条件过滤
this.RegisterEvent&lt;DamageEvent&gt;(e =>
{
    if (e.Target != this)
        return; // 不是针对自己的伤害，忽略

    ApplyDamage(e.Amount);
});
```

## 协程问题

### 协程不执行

**问题**：启动协程后没有任何效果。

**原因**：

- 未正确启动协程调度器
- 协程方法没有返回 `IEnumerator`
- 忘记调用 `yield return`

**解决方案**：

```csharp
// ❌ 错误：未启动协程调度器
public partial class GameRoot : Node
{
    public override void _Ready()
    {
        StartCoroutine(MyCoroutine()); // 不会执行
    }

    private IEnumerator MyCoroutine()
    {
        yield return new WaitForSeconds(1);
    }
}

// ✅ 正确：在 Godot 中使用协程
public partial class GameRoot : Node
{
    private CoroutineScheduler _scheduler;

    public override void _Ready()
    {
        _scheduler = new CoroutineScheduler(new GodotTimeSource(this));
        _scheduler.StartCoroutine(MyCoroutine());
    }

    public override void _Process(double delta)
    {
        _scheduler?.Update((float)delta);
    }

    private IEnumerator MyCoroutine()
    {
        yield return new WaitForSeconds(1);
        GD.Print("Coroutine executed!");
    }
}

// ❌ 错误：方法签名错误
private void MyCoroutine() // 应该返回 IEnumerator
{
    // 不会作为协程执行
}

// ✅ 正确：返回 IEnumerator
private IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1);
}

// ❌ 错误：忘记 yield return
private IEnumerator MyCoroutine()
{
    new WaitForSeconds(1); // 缺少 yield return
    GD.Print("This executes immediately!");
}

// ✅ 正确：使用 yield return
private IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1);
    GD.Print("This executes after 1 second!");
}
```

### 协程死锁

**问题**：协程永远不会完成。

**原因**：

- 等待条件永远不满足
- 循环等待
- 等待已停止的协程

**解决方案**：

```csharp
// ❌ 错误：等待条件永远不满足
private IEnumerator WaitForever()
{
    yield return new WaitUntil(() => false); // 永远等待
}

// ✅ 正确：添加超时机制
private IEnumerator WaitWithTimeout()
{
    float timeout = 5f;
    float elapsed = 0f;

    while (!condition && elapsed &lt; timeout)
    {
        elapsed += Time.DeltaTime;
        yield return null;
    }

    if (elapsed &gt;= timeout)
    {
        GD.PrintErr("Timeout!");
    }
}

// ✅ 使用 WaitForEventWithTimeout
private IEnumerator WaitForEventSafely()
{
    yield return new WaitForEventWithTimeout&lt;GameEvent&gt;(
        this.GetArchitecture(),
        timeout: 5f
    );
}

// ❌ 错误：循环等待
private IEnumerator CoroutineA()
{
    yield return StartCoroutine(CoroutineB());
}

private IEnumerator CoroutineB()
{
    yield return StartCoroutine(CoroutineA()); // 循环等待
}

// ✅ 正确：避免循环依赖
private IEnumerator CoroutineA()
{
    yield return new WaitForSeconds(1);
    this.SendEvent(new EventA());
}

private IEnumerator CoroutineB()
{
    yield return new WaitForEvent&lt;EventA&gt;(this.GetArchitecture());
    // 继续执行
}
```

### 协程提前停止

**问题**：协程在完成前被意外停止。

**解决方案**：

```csharp
// ✅ 保存协程句柄
private CoroutineHandle _coroutineHandle;

public void StartMyCoroutine()
{
    _coroutineHandle = _scheduler.StartCoroutine(MyCoroutine());
}

public void StopMyCoroutine()
{
    if (_coroutineHandle != null && _coroutineHandle.IsRunning)
    {
        _coroutineHandle.Stop();
    }
}

// ✅ 检查协程状态
private IEnumerator MonitorCoroutine()
{
    var handle = _scheduler.StartCoroutine(LongRunningCoroutine());

    while (handle.IsRunning)
    {
        yield return null;
    }

    GD.Print($"Coroutine completed with state: {handle.State}");
}

// ✅ 使用 try-finally 确保清理
private IEnumerator CoroutineWithCleanup()
{
    try
    {
        yield return new WaitForSeconds(1);
        // 执行操作
    }
    finally
    {
        // 清理资源
        Cleanup();
    }
}
```

## 资源管理问题

### 资源加载失败

**问题**：无法加载资源文件。

**错误信息**：

```
FileNotFoundException: Could not find file 'res://assets/player.png'
```

**解决方案**：

```csharp
// ❌ 错误：路径错误
var texture = ResourceLoader.Load&lt;Texture2D&gt;("assets/player.png"); // 缺少 res://

// ✅ 正确：使用完整路径
var texture = ResourceLoader.Load&lt;Texture2D&gt;("res://assets/player.png");

// ✅ 检查资源是否存在
if (ResourceLoader.Exists("res://assets/player.png"))
{
    var texture = ResourceLoader.Load&lt;Texture2D&gt;("res://assets/player.png");
}
else
{
    GD.PrintErr("Resource not found!");
}

// ✅ 使用资源管理器
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterSystem(new ResourceManager());
    }
}

// 加载资源
var handle = this.GetSystem&lt;ResourceManager&gt;()
    .LoadAsync&lt;Texture2D&gt;("res://assets/player.png");

await handle.Task;

if (handle.IsValid)
{
    var texture = handle.Resource;
}
else
{
    GD.PrintErr($"Failed to load resource: {handle.Error}");
}
```

### 资源内存泄漏

**问题**：加载的资源未释放，导致内存持续增长。

**解决方案**：

```csharp
// ❌ 错误：未释放资源
public void LoadTexture()
{
    var texture = ResourceLoader.Load&lt;Texture2D&gt;("res://assets/player.png");
    // 使用 texture
    // 忘记释放
}

// ✅ 正确：使用资源句柄自动管理
private IResourceHandle&lt;Texture2D&gt; _textureHandle;

public void LoadTexture()
{
    _textureHandle = resourceManager.Load&lt;Texture2D&gt;("res://assets/player.png");
}

public void Cleanup()
{
    _textureHandle?.Release(); // 释放资源
}

// ✅ 使用自动释放策略
var handle = resourceManager.Load&lt;Texture2D&gt;(
    "res://assets/player.png",
    new AutoReleaseStrategy(timeToLive: 60f) // 60秒后自动释放
);

// ✅ 使用 using 语句
public async Task LoadAndUseResource()
{
    using var handle = resourceManager.Load&lt;Texture2D&gt;("res://assets/player.png");
    await handle.Task;

    // 使用资源
    var texture = handle.Resource;

    // 离开作用域时自动释放
}
```

### 资源加载超时

**问题**：大型资源加载时间过长。

**解决方案**：

```csharp
// ✅ 使用异步加载
private async Task LoadLargeResourceAsync()
{
    var handle = resourceManager.LoadAsync&lt;PackedScene&gt;("res://scenes/large_scene.tscn");

    // 显示加载进度
    while (!handle.IsDone)
    {
        GD.Print($"Loading: {handle.Progress * 100}%");
        await Task.Delay(100);
    }

    if (handle.IsValid)
    {
        var scene = handle.Resource;
    }
}

// ✅ 使用协程加载
private IEnumerator LoadLargeResourceCoroutine()
{
    var handle = resourceManager.LoadAsync&lt;PackedScene&gt;("res://scenes/large_scene.tscn");

    yield return new WaitForProgress(handle);

    if (handle.IsValid)
    {
        var scene = handle.Resource;
    }
}

// ✅ 预加载资源
public override void _Ready()
{
    // 在游戏开始时预加载常用资源
    PreloadResources();
}

private async void PreloadResources()
{
    var resources = new[]
    {
        "res://assets/player.png",
        "res://assets/enemy.png",
        "res://sounds/bgm.ogg"
    };

    foreach (var path in resources)
    {
        await resourceManager.LoadAsync&lt;Resource&gt;(path).Task;
    }

    GD.Print("All resources preloaded!");
}
```

## Godot 集成问题

### 场景加载失败

**问题**：无法加载或实例化场景。

**错误信息**：

```
NullReferenceException: Object reference not set to an instance of an object
```

**解决方案**：

```csharp
// ❌ 错误：路径错误或场景未导出
var scene = GD.Load&lt;PackedScene&gt;("scenes/player.tscn"); // 缺少 res://

// ✅ 正确：使用完整路径
var scene = GD.Load&lt;PackedScene&gt;("res://scenes/player.tscn");
var instance = scene.Instantiate();

// ✅ 检查场景是否存在
if (ResourceLoader.Exists("res://scenes/player.tscn"))
{
    var scene = GD.Load&lt;PackedScene&gt;("res://scenes/player.tscn");
    var instance = scene.Instantiate&lt;Player&gt;();
    AddChild(instance);
}
else
{
    GD.PrintErr("Scene not found!");
}

// ✅ 使用场景路由器
public class GameSceneRouter : SceneRouterBase
{
    protected override void RegisterScenes()
    {
        Register&lt;MainMenuScene&gt;("MainMenu", "res://scenes/main_menu.tscn");
        Register&lt;GameScene&gt;("Game", "res://scenes/game.tscn");
    }
}

// 导航到场景
await sceneRouter.NavigateToAsync("Game");
```

### 节点查找失败

**问题**：使用 `GetNode()` 查找节点返回 null。

**错误信息**：

```
NullReferenceException: Object reference not set to an instance of an object
```

**解决方案**：

```csharp
// ❌ 错误：路径错误
var player = GetNode&lt;Player&gt;("Player"); // 节点可能在子节点中

// ✅ 正确：使用完整路径
var player = GetNode&lt;Player&gt;("Level/Player");

// ✅ 使用 % 符号访问唯一节点（Godot 4.0+）
var player = GetNode&lt;Player&gt;("%Player");

// ✅ 检查节点是否存在
if (HasNode("Player"))
{
    var player = GetNode&lt;Player&gt;("Player");
}
else
{
    GD.PrintErr("Player node not found!");
}

// ✅ 使用 NodePath 缓存
[Export] private NodePath _playerPath;
private Player _player;

public override void _Ready()
{
    if (_playerPath != null)
    {
        _player = GetNode&lt;Player&gt;(_playerPath);
    }
}
```

### 信号连接失败

**问题**：信号未正确连接或触发。

**解决方案**：

```csharp
// ❌ 错误：信号名称错误
button.Connect("press", Callable.From(OnButtonPressed)); // 应该是 "pressed"

// ✅ 正确：使用正确的信号名称
button.Connect("pressed", Callable.From(OnButtonPressed));

// ✅ 使用类型安全的信号连接
button.Pressed += OnButtonPressed;

// ✅ 使用 GFramework 的流式 API
button.OnPressed()
    .Subscribe(OnButtonPressed)
    .AddToUnregisterList(_unregisterList);

// ✅ 检查信号是否存在
if (button.HasSignal("pressed"))
{
    button.Connect("pressed", Callable.From(OnButtonPressed));
}

// ✅ 自动注销信号
public partial class GameController : Node
{
    private IUnRegisterList _unregisterList = new UnRegisterList();

    public override void _Ready()
    {
        var button = GetNode&lt;Button&gt;("Button");
        button.OnPressed()
            .Subscribe(OnButtonPressed)
            .AddToUnregisterList(_unregisterList);
    }

    public override void _ExitTree()
    {
        _unregisterList.UnRegisterAll();
    }

    private void OnButtonPressed()
    {
        GD.Print("Button pressed!");
    }
}
```

### 架构上下文丢失

**问题**：在 Godot 节点中无法访问架构。

**错误信息**：

```
InvalidOperationException: Architecture context not found
```

**解决方案**：

```csharp
// ❌ 错误：未设置架构锚点
public partial class Player : Node
{
    public override void _Ready()
    {
        var model = this.GetModel&lt;PlayerModel&gt;(); // 失败：无架构上下文
    }
}

// ✅ 正确：在根节点设置架构锚点
public partial class GameRoot : Node
{
    private GameArchitecture _architecture;

    public override void _Ready()
    {
        _architecture = new GameArchitecture();
        _architecture.Initialize();

        // 设置架构锚点
        var anchor = new ArchitectureAnchor();
        anchor.SetArchitecture(_architecture);
        AddChild(anchor);
    }
}

// 现在子节点可以访问架构
public partial class Player : Node
{
    public override void _Ready()
    {
        var model = this.GetModel&lt;PlayerModel&gt;(); // 成功
    }
}

// ✅ 使用 Godot 模块
public class GameArchitecture : AbstractArchitecture
{
    protected override void Init()
    {
        // 注册 Godot 模块
        this.RegisterGodotModule&lt;PlayerModule&gt;();
        this.RegisterGodotModule&lt;EnemyModule&gt;();
    }
}
```

### UI 页面导航问题

**问题**：UI 页面无法正确显示或切换。

**解决方案**：

```csharp
// ❌ 错误：未注册 UI 页面
var uiRouter = this.GetSystem&lt;IUiRouter&gt;();
await uiRouter.PushAsync("MainMenu"); // 失败：页面未注册

// ✅ 正确：先注册 UI 页面
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        var uiRegistry = new GodotUiRegistry();
        uiRegistry.Register("MainMenu", "res://ui/main_menu.tscn", UiLayer.Page);
        uiRegistry.Register("Settings", "res://ui/settings.tscn", UiLayer.Modal);

        RegisterUtility&lt;IGodotUiRegistry&gt;(uiRegistry);
        RegisterSystem(new UiRouter(uiRegistry));
    }
}

// 导航到页面
var uiRouter = this.GetSystem&lt;IUiRouter&gt;();
await uiRouter.PushAsync("MainMenu");

// ✅ 使用转场效果
await uiRouter.PushAsync("Settings", new UiTransitionOptions
{
    TransitionType = UiTransitionType.Fade,
    Duration = 0.3f
});

// ✅ 处理导航失败
try
{
    await uiRouter.PushAsync("NonExistentPage");
}
catch (InvalidOperationException ex)
{
    GD.PrintErr($"Navigation failed: {ex.Message}");
}
```

## 性能问题

### 事件处理缓慢

**问题**：事件处理耗时过长，导致游戏卡顿。

**诊断**：

```csharp
// 测量事件处理时间
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
arch.SendEvent(new HeavyEvent());
stopwatch.Stop();
GD.Print($"Event processing time: {stopwatch.ElapsedMilliseconds}ms");

// 使用性能分析器
public class PerformanceProfiler : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent&lt;IEvent&gt;(e =>
        {
            var sw = Stopwatch.StartNew();
            // 事件处理
            sw.Stop();
            if (sw.ElapsedMilliseconds &gt; 16) // 超过一帧
            {
                GD.PrintErr($"Slow event: {e.GetType().Name} took {sw.ElapsedMilliseconds}ms");
            }
        });
    }
}
```

**优化**：

```csharp
// ❌ 低效：在事件处理中进行复杂计算
private void OnEvent(HeavyEvent e)
{
    for (int i = 0; i &lt; 1000000; i++)
    {
        // 复杂计算
    }
}

// ✅ 高效：异步处理
private async void OnEvent(HeavyEvent e)
{
    await Task.Run(() =>
    {
        for (int i = 0; i &lt; 1000000; i++)
        {
            // 复杂计算
        }
    });
}

// ✅ 使用协程分帧处理
private void OnEvent(HeavyEvent e)
{
    this.StartCoroutine(ProcessHeavyEventCoroutine(e));
}

private IEnumerator ProcessHeavyEventCoroutine(HeavyEvent e)
{
    const int batchSize = 1000;
    for (int i = 0; i &lt; 1000000; i += batchSize)
    {
        // 处理一批数据
        for (int j = 0; j &lt; batchSize && i + j &lt; 1000000; j++)
        {
            // 计算
        }
        yield return null; // 下一帧继续
    }
}
```

### 频繁的组件访问

**问题**：每帧都调用 `GetModel`、`GetSystem` 等方法导致性能下降。

**优化**：

```csharp
// ❌ 低效：每帧访问
public override void _Process(double delta)
{
    var model = this.GetModel&lt;PlayerModel&gt;(); // 每帧调用
    model.Health.Value -= 1;
}

// ✅ 高效：缓存引用
private PlayerModel _playerModel;

public override void _Ready()
{
    _playerModel = this.GetModel&lt;PlayerModel&gt;(); // 只调用一次
}

public override void _Process(double delta)
{
    _playerModel.Health.Value -= 1;
}

// ✅ 使用延迟初始化
private PlayerModel _playerModel;
private PlayerModel PlayerModel =&gt; _playerModel ??= this.GetModel&lt;PlayerModel&gt;();

public override void _Process(double delta)
{
    PlayerModel.Health.Value -= 1;
}
```

### 内存占用过高

**问题**：游戏运行时内存持续增长。

**诊断**：

```csharp
// 监控内存使用
public class MemoryMonitor : Node
{
    public override void _Process(double delta)
    {
        var memoryUsed = GC.GetTotalMemory(false) / 1024 / 1024;
        GD.Print($"Memory used: {memoryUsed} MB");

        if (memoryUsed &gt; 500) // 超过 500MB
        {
            GD.PrintErr("High memory usage detected!");
        }
    }
}
```

**优化**：

```csharp
// ✅ 使用对象池
public class BulletPool : AbstractNodePoolSystem&lt;Bullet&gt;
{
    protected override Bullet CreateInstance()
    {
        var scene = GD.Load&lt;PackedScene&gt;("res://entities/bullet.tscn");
        return scene.Instantiate&lt;Bullet&gt;();
    }

    protected override void OnGet(Bullet bullet)
    {
        bullet.Show();
        bullet.ProcessMode = ProcessModeEnum.Inherit;
    }

    protected override void OnRelease(Bullet bullet)
    {
        bullet.Hide();
        bullet.ProcessMode = ProcessModeEnum.Disabled;
    }
}

// 使用对象池
var bullet = bulletPool.Get();
// 使用完毕后归还
bulletPool.Release(bullet);

// ✅ 及时释放资源
public override void _ExitTree()
{
    // 注销事件监听器
    _unregisterList.UnRegisterAll();

    // 释放资源句柄
    _textureHandle?.Release();

    // 停止协程
    _coroutineHandle?.Stop();

    // 销毁架构（如果是根节点）
    _architecture?.Destroy();
}

// ✅ 定期执行垃圾回收
private float _gcTimer = 0f;
private const float GC_INTERVAL = 60f; // 每60秒

public override void _Process(double delta)
{
    _gcTimer += (float)delta;
    if (_gcTimer &gt;= GC_INTERVAL)
    {
        _gcTimer = 0f;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
```

### 协程性能问题

**问题**：大量协程导致性能下降。

**优化**：

```csharp
// ❌ 低效：创建大量短期协程
for (int i = 0; i &lt; 1000; i++)
{
    StartCoroutine(ShortCoroutine());
}

// ✅ 高效：合并协程
StartCoroutine(BatchCoroutine(1000));

private IEnumerator BatchCoroutine(int count)
{
    for (int i = 0; i &lt; count; i++)
    {
        // 处理
        if (i % 10 == 0)
            yield return null; // 每10次操作暂停一次
    }
}

// ✅ 使用协程池
private readonly Queue&lt;IEnumerator&gt; _coroutineQueue = new();

private IEnumerator CoroutinePoolRunner()
{
    while (true)
    {
        if (_coroutineQueue.Count &gt; 0)
        {
            var coroutine = _coroutineQueue.Dequeue();
            yield return coroutine;
        }
        else
        {
            yield return null;
        }
    }
}

// 添加到队列而不是立即启动
_coroutineQueue.Enqueue(MyCoroutine());
```

## 调试技巧

### 1. 启用日志系统

```csharp
// 使用 GFramework 日志系统
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册日志工厂
        RegisterUtility&lt;ILoggerFactory&gt;(new GodotLoggerFactory());
    }
}

// 在组件中使用日志
public class PlayerSystem : AbstractSystem
{
    private ILogger _logger;

    protected override void OnInit()
    {
        _logger = this.GetUtility&lt;ILoggerFactory&gt;().CreateLogger&lt;PlayerSystem&gt;();
        _logger.LogInfo("PlayerSystem initialized");
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        _logger.LogWarning($"Player died at position {e.Position}");
    }
}

// 配置日志级别
var loggingConfig = new LoggingConfiguration
{
    MinimumLevel = LogLevel.Debug,
    Appenders = new[]
    {
        new ConsoleAppender(),
        new FileAppender("logs/game.log")
    }
};
```

### 2. 使用断点调试

```csharp
// 在关键位置添加断点
public override void _Ready()
{
    var model = this.GetModel&lt;PlayerModel&gt;();
    // 在这里设置断点，检查 model 的值
    GD.Print($"Player health: {model.Health.Value}");
}

// 使用条件断点
private void OnDamage(DamageEvent e)
{
    // 只在伤害大于50时中断
    if (e.Amount &gt; 50)
    {
        GD.Print("High damage detected!"); // 在这里设置断点
    }
}

// 使用 GD.PushWarning 和 GD.PushError
if (player == null)
{
    GD.PushError("Player is null!"); // 在输出面板显示错误
    return;
}
```

### 3. 追踪事件流

```csharp
// 创建事件追踪系统
public class EventTracer : AbstractSystem
{
    private ILogger _logger;

    protected override void OnInit()
    {
        _logger = this.GetUtility&lt;ILoggerFactory&gt;().CreateLogger&lt;EventTracer&gt;();

        // 使用反射监听所有事件（仅用于调试）
        this.RegisterEvent&lt;PlayerDiedEvent&gt;(e =&gt;
            _logger.LogDebug($"Event: PlayerDiedEvent"));
        this.RegisterEvent&lt;PlayerAttackedEvent&gt;(e =&gt;
            _logger.LogDebug($"Event: PlayerAttackedEvent - Damage: {e.Damage}"));
    }
}

// 在架构中启用事件追踪
#if DEBUG
protected override void Init()
{
    RegisterSystem(new EventTracer());
}
#endif
```

### 4. 性能分析

```csharp
// 使用 Godot 性能监视器
public override void _Process(double delta)
{
    // 在编辑器中查看性能统计
    Performance.GetMonitor(Performance.Monitor.TimeFps);
    Performance.GetMonitor(Performance.Monitor.MemoryStatic);
}

// 自定义性能计数器
public class PerformanceCounter
{
    private readonly Dictionary&lt;string, Stopwatch&gt; _timers = new();

    public void StartTimer(string name)
    {
        if (!_timers.ContainsKey(name))
            _timers[name] = new Stopwatch();

        _timers[name].Restart();
    }

    public void StopTimer(string name)
    {
        if (_timers.TryGetValue(name, out var timer))
        {
            timer.Stop();
            GD.Print($"{name}: {timer.ElapsedMilliseconds}ms");
        }
    }
}

// 使用
var counter = new PerformanceCounter();
counter.StartTimer("EventProcessing");
arch.SendEvent(new GameEvent());
counter.StopTimer("EventProcessing");
```

### 5. 单元测试调试

```csharp
[Test]
public void DebugPlayerDamage()
{
    var arch = new TestArchitecture();
    arch.Initialize();

    var player = arch.GetModel&lt;PlayerModel&gt;();

    // 打印初始状态
    GD.Print($"Initial Health: {player.Health.Value}");

    // 发送伤害事件
    arch.SendEvent(new DamageEvent { Amount = 10 });

    // 打印最终状态
    GD.Print($"Final Health: {player.Health.Value}");

    // 验证
    Assert.AreEqual(90, player.Health.Value);
}
```

## 常见错误信息

### NU1101: Unable to find package

**错误类型**：NuGet 包安装错误

**完整错误信息**：

```
NU1101: Unable to find package GFramework.Core. No packages exist with this id in source(s): nuget.org
```

**原因**：

- 包源配置错误
- 包名拼写错误
- 网络连接问题

**解决方案**：

```bash
# 1. 检查包源
dotnet nuget list source

# 2. 添加 NuGet.org 源
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# 3. 清理缓存
dotnet nuget locals all --clear

# 4. 重新安装
dotnet restore
```

### KeyNotFoundException: 未找到类型

**错误类型**：架构组件未注册

**完整错误信息**：

```
System.Collections.Generic.KeyNotFoundException: 未找到类型为 'PlayerModel' 的组件
```

**原因**：尝试获取未注册的组件

**解决方案**：

```csharp
// 在架构中注册组件
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new PlayerSystem());
        RegisterUtility&lt;IConfigUtility&gt;(new ConfigUtility());
    }
}
```

### InvalidOperationException: Architecture is already initialized

**错误类型**：重复初始化

**完整错误信息**：

```
System.InvalidOperationException: Architecture is already initialized
```

**原因**：多次调用 `Initialize()` 方法

**解决方案**：

```csharp
// 检查初始化状态
if (!architecture.IsInitialized)
{
    architecture.Initialize();
}

// 或使用单例模式
public class GameArchitecture : Architecture&lt;GameArchitecture&gt;
{
    // 自动处理单例
}
```

### NullReferenceException: Object reference not set

**错误类型**：空引用异常

**常见场景**：

1. **节点未找到**：

```csharp
// ❌ 错误
var player = GetNode&lt;Player&gt;("Player"); // 返回 null
player.Health = 100; // 抛出异常

// ✅ 正确
if (HasNode("Player"))
{
    var player = GetNode&lt;Player&gt;("Player");
    player.Health = 100;
}
```

2. **组件未注册**：

```csharp
// ❌ 错误
var model = this.GetModel&lt;PlayerModel&gt;(); // 返回 null
model.Health.Value = 100; // 抛出异常

// ✅ 正确：先注册
protected override void Init()
{
    RegisterModel(new PlayerModel());
}
```

3. **架构上下文丢失**：

```csharp
// ❌ 错误：未设置架构锚点
var model = this.GetModel&lt;PlayerModel&gt;(); // 抛出异常

// ✅ 正确：设置架构锚点
var anchor = new ArchitectureAnchor();
anchor.SetArchitecture(architecture);
AddChild(anchor);
```

### InvalidCastException: Unable to cast object

**错误类型**：类型转换错误

**完整错误信息**：

```
System.InvalidCastException: Unable to cast object of type 'Node' to type 'Player'
```

**原因**：节点类型不匹配

**解决方案**：

```csharp
// ❌ 错误：强制转换
var player = (Player)GetNode("Player"); // 如果不是 Player 类型会抛出异常

// ✅ 正确：使用泛型方法
var player = GetNode&lt;Player&gt;("Player");

// ✅ 使用 as 操作符
var player = GetNode("Player") as Player;
if (player != null)
{
    player.Health = 100;
}

// ✅ 使用 is 模式匹配
if (GetNode("Player") is Player player)
{
    player.Health = 100;
}
```

### ArgumentException: An item with the same key has already been added

**错误类型**：重复注册

**完整错误信息**：

```
System.ArgumentException: An item with the same key has already been added. Key: PlayerModel
```

**原因**：重复注册同一类型的组件

**解决方案**：

```csharp
// ❌ 错误：重复注册
protected override void Init()
{
    RegisterModel(new PlayerModel());
    RegisterModel(new PlayerModel()); // 重复注册
}

// ✅ 正确：只注册一次
protected override void Init()
{
    RegisterModel(new PlayerModel());
}

// ✅ 如果需要多个实例，使用不同的键
protected override void Init()
{
    RegisterModel(new PlayerModel(), "Player1");
    RegisterModel(new PlayerModel(), "Player2");
}
```

### TimeoutException: The operation has timed out

**错误类型**：操作超时

**常见场景**：

1. **资源加载超时**：

```csharp
// ✅ 增加超时时间
var handle = resourceManager.LoadAsync&lt;Texture2D&gt;(
    "res://assets/large_texture.png",
    timeout: 30f // 30秒超时
);
```

2. **事件等待超时**：

```csharp
// ✅ 使用带超时的等待
yield return new WaitForEventWithTimeout&lt;GameEvent&gt;(
    architecture,
    timeout: 5f
);
```

### StackOverflowException: Operation caused a stack overflow

**错误类型**：栈溢出

**原因**：

- 无限递归
- 循环依赖

**解决方案**：

```csharp
// ❌ 错误：无限递归
private void ProcessData()
{
    ProcessData(); // 无限递归
}

// ✅ 正确：添加终止条件
private void ProcessData(int depth = 0)
{
    if (depth &gt; 100)
        return;

    ProcessData(depth + 1);
}

// ❌ 错误：循环依赖
public class SystemA : AbstractSystem
{
    protected override void OnInit()
    {
        this.GetSystem&lt;SystemB&gt;().Initialize();
    }
}

public class SystemB : AbstractSystem
{
    protected override void OnInit()
    {
        this.GetSystem&lt;SystemA&gt;().Initialize(); // 循环依赖
    }
}

// ✅ 正确：使用事件解耦
public class SystemA : AbstractSystem
{
    protected override void OnInit()
    {
        this.SendEvent(new InitializeSystemBEvent());
    }
}
```

### ObjectDisposedException: Cannot access a disposed object

**错误类型**：访问已释放的对象

**完整错误信息**：

```
System.ObjectDisposedException: Cannot access a disposed object. Object name: 'Architecture'
```

**原因**：在架构销毁后继续使用

**解决方案**：

```csharp
// ✅ 检查对象状态
if (architecture != null && !architecture.IsDisposed)
{
    var model = architecture.GetModel&lt;PlayerModel&gt;();
}

// ✅ 在销毁前清理引用
public override void _ExitTree()
{
    _unregisterList.UnRegisterAll();
    _architecture = null; // 清理引用
}

// ✅ 使用弱引用
private WeakReference&lt;GameArchitecture&gt; _architectureRef;

public void UseArchitecture()
{
    if (_architectureRef.TryGetTarget(out var arch))
    {
        var model = arch.GetModel&lt;PlayerModel&gt;();
    }
}
```

### FileNotFoundException: Could not find file

**错误类型**：文件未找到

**完整错误信息**：

```
System.IO.FileNotFoundException: Could not find file 'res://assets/player.png'
```

**原因**：

- 文件路径错误
- 文件不存在
- 文件未导入到项目

**解决方案**：

```csharp
// ✅ 检查文件是否存在
if (ResourceLoader.Exists("res://assets/player.png"))
{
    var texture = ResourceLoader.Load&lt;Texture2D&gt;("res://assets/player.png");
}
else
{
    GD.PrintErr("File not found!");
}

// ✅ 使用正确的路径格式
// Godot 使用 res:// 协议
var texture = ResourceLoader.Load&lt;Texture2D&gt;("res://assets/player.png");

// ✅ 检查文件是否在 .import 文件中
// 确保文件已被 Godot 导入
```

### NotImplementedException: The method is not implemented

**错误类型**：方法未实现

**完整错误信息**：

```
System.NotImplementedException: The method or operation is not implemented
```

**原因**：抽象方法或接口方法未实现

**解决方案**：

```csharp
// ❌ 错误：未实现抽象方法
public class MySystem : AbstractSystem
{
    // 缺少 OnInit 实现
}

// ✅ 正确：实现所有抽象方法
public class MySystem : AbstractSystem
{
    protected override void OnInit()
    {
        // 实现初始化逻辑
    }
}

// ✅ 实现接口方法
public class MyLoader : IResourceLoader
{
    public async Task&lt;T&gt; LoadAsync&lt;T&gt;(string path) where T : class
    {
        // 实现加载逻辑
        await Task.Delay(100);
        return default;
    }
}
```

## 常见问题排查清单

- [ ] 所有组件都已注册？
- [ ] 事件类型是否匹配？
- [ ] 是否正确注销了监听器？
- [ ] Architecture 是否已初始化？
- [ ] 是否有循环依赖？
- [ ] 内存使用是否持续增长？
- [ ] 事件处理是否过于复杂？
- [ ] 是否缓存了频繁访问的组件？
- [ ] 资源是否正确释放？
- [ ] 协程是否正确启动和停止？
- [ ] Godot 节点路径是否正确？
- [ ] 信号连接是否成功？

## 获取帮助

如果问题仍未解决：

1. 查看 [Core 文档](/zh-CN/core/) 了解更多细节
2. 查看 [架构组件](/zh-CN/core/architecture) 了解架构设计
3. 查看 [Godot 集成](/zh-CN/godot/) 了解 Godot 特定问题
4. 在 [GitHub Issues](https://github.com/GeWuYou/GFramework/issues) 提交问题
5. 查看 [教程](/zh-CN/tutorials/) 中的示例代码
6. 查看 [常见问题](/zh-CN/faq) 获取快速答案

---

**提示**：在提交 Issue 时，请提供：

- 错误信息和完整的堆栈跟踪
- 最小化的可复现代码示例
- 你的环境信息（.NET 版本、Godot 版本、操作系统等）
- 已尝试的解决方案
- 相关的配置文件（如有）

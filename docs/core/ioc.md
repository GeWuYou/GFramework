# IoC 包使用说明

## 概述

IoC（Inversion of Control，控制反转）包提供了一个轻量级的依赖注入容器，用于管理框架中各种组件的注册和获取。通过 IoC
容器，可以实现组件间的解耦，便于测试和维护。

## 核心类

### [`IocContainer`](IocContainer.cs)

IoC 容器类，负责管理对象的注册和获取。

**主要功能：**

- 注册实例到容器
- 从容器中获取实例
- 类型安全的依赖管理
- 线程安全操作
- 容器冻结保护

## 核心方法

### 1. Register<T> 和 Register(Type, object)

注册一个实例到容器中。

```csharp
public void Register<T>(T instance)
public void Register(Type type, object instance)
```

**参数：**

- `instance`: 要注册的实例对象
- `type`: 要注册的类型（重载方法）

**使用示例：**

```csharp
var container = new IocContainer();

// 注册各种类型的实例
container.Register<IPlayerModel>(new PlayerModel());
container.Register<IGameSystem>(new GameSystem());
container.Register<IStorageUtility>(new StorageUtility());
```

### 2. RegisterSingleton<T>

注册单例实例到容器中。一个类型只允许一个实例。

```csharp
public void RegisterSingleton<T>(T instance)
```

**参数：**

- `instance`: 要注册的单例实例

**使用示例：**

```csharp
var container = new IocContainer();

// 注册单例
container.RegisterSingleton<IPlayerModel>(new PlayerModel());
```

### 3. RegisterPlurality

注册多个实例，将实例注册到其实现的所有接口和具体类型上。

```csharp
public void RegisterPlurality(object instance)
```

**参数：**

- `instance`: 要注册的实例

### 4. RegisterSystem

注册系统实例，将其绑定到其所有实现的接口上。

```csharp
public void RegisterSystem(ISystem system)
```

**参数：**

- `system`: 系统实例对象

### 5. Get<T> 和 GetAll<T>

从容器中获取指定类型的实例。

```csharp
public T? Get<T>() where T : class
public IReadOnlyList<T> GetAll<T>() where T : class
```

**返回值：**

- `Get<T>`: 返回指定类型的实例，如果未找到则返回 `null`
- `GetAll<T>`: 返回指定类型的所有实例列表，如果未找到则返回空数组

**使用示例：**

```csharp
// 获取已注册的实例
var playerModel = container.Get<IPlayerModel>();
var gameSystems = container.GetAll<IGameSystem>();

// 如果类型未注册，Get 返回 null，GetAll 返回空数组
var unknownService = container.Get<IUnknownService>();  // null
```

### 6. GetRequired<T>

获取指定类型的必需实例，如果没有注册或注册了多个实例会抛出异常。

```csharp
public T GetRequired<T>() where T : class
```

**返回值：**

- 返回找到的唯一实例

### 7. GetAllSorted<T>

获取并排序（系统调度专用）。

```csharp
public IReadOnlyList<T> GetAllSorted<T>(Comparison<T> comparison) where T : class
```

**参数：**

- `comparison`: 比较器委托，定义排序规则

**返回值：**

- 按指定方式排序后的实例列表

## 在框架中的使用

### Architecture 中的应用

IoC 容器是 [`Architecture`](../architecture/Architecture.cs) 类的核心组件，用于管理所有的 System、Model 和 Utility。

```csharp
public abstract class Architecture : IArchitecture
{
    // 内置 IoC 容器
    private readonly IocContainer _mContainer = new();

    // 注册系统
    public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        system.SetArchitecture(this);
        _mContainer.Register(system);  // 注册到容器
        // ...
    }

    // 获取系统
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        => _mContainer.Get<TSystem>();  // 从容器获取

    // Model 和 Utility 同理
}
```

### 注册组件到容器

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 这些方法内部都使用 IoC 容器

        // 注册 Model（存储游戏数据）
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IInventoryModel>(new InventoryModel());

        // 注册 System（业务逻辑）
        RegisterSystem<IGameplaySystem>(new GameplaySystem());
        RegisterSystem<ISaveSystem>(new SaveSystem());

        // 注册 Utility（工具类）
        RegisterUtility<ITimeUtility>(new TimeUtility());
        RegisterUtility<IStorageUtility>(new StorageUtility());
    }
}
```

### 从容器获取组件

```csharp
// 通过扩展方法间接使用 IoC 容器
public class PlayerController : IController
{
    public void Start()
    {
        // GetModel 内部调用 Architecture.GetModel
        // Architecture.GetModel 内部调用 IocContainer.Get
        var playerModel = this.GetModel<IPlayerModel>();
        
        var gameplaySystem = this.GetSystem<IGameplaySystem>();
        var timeUtility = this.GetUtility<ITimeUtility>();
    }
}
```

## 工作原理

### 内部实现

```csharp
public class IocContainer
{
    // 使用字典存储类型到实例集合的映射
    private readonly Dictionary<Type, HashSet<object>> _typeIndex = new();
    private readonly HashSet<object> _objects = [];
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private volatile bool _frozen = false;
    
    public void Register<T>(T instance)
    {
        // 获取写锁以确保线程安全
        _lock.EnterWriteLock();
        try
        {
            RegisterInternal(typeof(T), instance!);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public T? Get<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_typeIndex.TryGetValue(typeof(T), out var set) && set.Count > 0)
            {
                var result = set.First() as T;
                return result;
            }

            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void RegisterInternal(Type type, object instance)
    {
        if (_frozen) throw new InvalidOperationException("IocContainer is frozen");

        _objects.Add(instance);

        if (!_typeIndex.TryGetValue(type, out var set))
        {
            set = [];
            _typeIndex[type] = set;
        }

        set.Add(instance);
    }
}
```

### 线程安全机制

容器使用 [ReaderWriterLockSlim](xref:System.Threading.ReaderWriterLockSlim) 来确保线程安全操作，允许多个线程同时读取，但在写入时阻止其他线程访问。

### 注册流程

```
用户代码
   ↓
RegisterSystem<T>(system)
   ↓
IocContainer.Register<T>(system)
   ↓
加写锁 -> Dictionary[typeof(T)] 添加实例到HashSet
```

### 获取流程

```
用户代码
   ↓
this.GetSystem<T>()
   ↓
Architecture.GetSystem<T>()
   ↓
IocContainer.Get<T>()
   ↓
加读锁 -> Dictionary.TryGetValue(typeof(T)) 获取HashSet
   ↓
返回HashSet中第一个实例或 null
```

## 使用示例

### 基础使用

```csharp
// 1. 创建容器
var container = new IocContainer();

// 2. 注册服务
var playerService = new PlayerService();
container.Register<IPlayerService>(playerService);

// 3. 获取服务
var service = container.Get<IPlayerService>();
service.DoSomething();
```

### 接口和实现分离

```csharp
// 定义接口
public interface IDataService
{
    void SaveData(string data);
    string LoadData();
}

// 实现类
public class LocalDataService : IDataService
{
    public void SaveData(string data) { /* 本地存储 */ }
    public string LoadData() { /* 本地加载 */ return ""; }
}

public class CloudDataService : IDataService
{
    public void SaveData(string data) { /* 云端存储 */ }
    public string LoadData() { /* 云端加载 */ return ""; }
}

// 注册（可以根据配置选择不同实现）
var container = new IocContainer();

#if CLOUD_SAVE
container.Register<IDataService>(new CloudDataService());
#else
container.Register<IDataService>(new LocalDataService());
#endif

// 使用（不需要关心具体实现）
var dataService = container.Get<IDataService>();
dataService.SaveData("game data");
```

### 注册多个实现

```csharp
var container = new IocContainer();

// 注册多个相同接口的不同实现
container.Register<IDataService>(new LocalDataService());
container.Register<IDataService>(new CloudDataService());

// 获取单个实例（返回第一个）
var singleService = container.Get<IDataService>();  // 返回第一个注册的实例

// 获取所有实例
var allServices = container.GetAll<IDataService>();  // 返回两个实例的列表
```

## 其他实用方法

### `Contains<T>()`

检查容器中是否包含指定类型的实例。

```csharp
public bool Contains<T>() where T : class
```

**参数：**

- 无泛型参数

**返回值：**

- 如果容器中包含指定类型的实例则返回 `true`，否则返回 `false`

**使用示例：**

```csharp
var container = new IocContainer();

// 检查服务是否已注册
if (container.Contains<IPlayerService>())
{
    Console.WriteLine("Player service is available");
}

// 根据检查结果决定是否注册
if (!container.Contains<ISettingsService>())
{
    container.Register<ISettingsService>(new SettingsService());
}
```

**应用场景：**

- 条件注册服务
- 检查依赖是否可用
- 动态功能开关

### `ContainsInstance(object instance)`

判断容器中是否包含某个具体的实例对象。

```csharp
public bool ContainsInstance(object instance)
```

**参数：**

- `instance`：待查询的实例对象

**返回值：**

- 若容器中包含该实例则返回 `true`，否则返回 `false`

**使用示例：**

```csharp
var container = new IocContainer();

var service = new MyService();
container.Register<IMyService>(service);

// 检查特定实例是否在容器中
if (container.ContainsInstance(service))
{
    Console.WriteLine("This instance is registered in the container");
}

// 检查另一个实例
var anotherService = new MyService();
if (!container.ContainsInstance(anotherService))
{
    Console.WriteLine("This instance is not in the container");
}
```

**应用场景：**

- 避免重复注册同一实例
- 检查对象是否已被管理
- 调试和日志记录

### `Clear()`

清空容器中的所有实例。

```csharp
public void Clear()
```

**使用示例：**

```csharp
var container = new IocContainer();

// 注册多个服务
container.Register<IService1>(new Service1());
container.Register<IService2>(new Service2());
container.Register<IService3>(new Service3());

// 清空容器
container.Clear();

// 检查是否清空成功
Console.WriteLine($"Contains IService1: {container.Contains<IService1>()}");  // False
Console.WriteLine($"Contains IService2: {container.Contains<IService2>()}");  // False
```

**应用场景：**

- 重置容器状态
- 内存清理
- 测试环境准备

**注意事项：**

- 容器冻结后也可以调用 `Clear()` 方法
- 清空后，所有已注册的实例都将丢失
- 不会自动清理已注册对象的其他引用

## 设计特点

### 1. 简单轻量

- 支持多种注册方式：普通注册、单例注册、多实例注册
- 基于字典和哈希集实现，性能高效
- 无复杂的依赖解析逻辑

### 2. 手动注册

- 需要显式注册每个组件
- 不支持自动依赖注入
- 完全可控的组件生命周期

### 3. 多实例支持

- 每个类型可以注册多个实例
- 提供 [GetAll](file:///d:/Project/Rider/GFramework/GFramework.Core/ioc/IocContainer.cs#L244-L261) 方法获取所有实例
- 提供 [Get](file:///d:/Project/Rider/GFramework/GFramework.Core/ioc/IocContainer.cs#L208-L225) 方法获取单个实例

### 4. 类型安全

- 基于泛型，编译时类型检查
- 避免字符串键导致的错误
- IDE 友好，支持自动补全

### 5. 线程安全

- 使用读写锁确保多线程环境下的安全操作
- 读操作可以并发执行
- 写操作独占锁，防止并发修改冲突

### 6. 容器冻结

- 提供 [Freeze](file:///d:/Project/Rider/GFramework/GFramework.Core/ioc/IocContainer.cs#L359-L372) 方法，防止进一步修改容器内容
- 防止在初始化后意外修改注册内容

## 与其他 IoC 容器的区别

### 本框架的 IocContainer

```csharp
// 简单直接
var container = new IocContainer();
container.Register(new MyService());
var service = container.Get<MyService>();
```

**特点：**

- ✅ 简单易用
- ✅ 性能高
- ✅ 线程安全
- ✅ 支持多实例
- ❌ 不支持构造函数注入
- ❌ 不支持自动解析依赖
- ❌ 不支持生命周期管理（Transient/Scoped/Singleton）

### 完整的 IoC 框架（如 Autofac、Zenject）

```csharp
// 复杂但功能强大
var builder = new ContainerBuilder();
builder.RegisterType<MyService>().As<IMyService>().SingleInstance();
builder.RegisterType<MyController>().WithParameter("config", config);
var container = builder.Build();

// 自动解析依赖
var controller = container.Resolve<MyController>();
```

**特点：**

- ✅ 自动依赖注入
- ✅ 生命周期管理
- ✅ 复杂场景支持
- ❌ 学习成本高
- ❌ 性能开销大

## 最佳实践

### 1. 在架构初始化时注册

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        // 按顺序注册组件
        // 1. 工具类（无依赖）
        RegisterUtility(new TimeUtility());
        RegisterUtility(new StorageUtility());

        // 2. 模型（可能依赖工具）
        RegisterModel(new PlayerModel());
        RegisterModel(new GameModel());

        // 3. 系统（可能依赖模型和工具）
        RegisterSystem(new GameplaySystem());
        RegisterSystem(new SaveSystem());
    }
}
```

### 2. 使用接口类型注册

```csharp
// ❌ 不推荐：直接使用实现类
RegisterSystem(new ConcreteSystem());
var system = GetSystem<ConcreteSystem>();

// ✅ 推荐：使用接口
RegisterSystem<IGameSystem>(new ConcreteSystem());
var system = GetSystem<IGameSystem>();
```

### 3. 避免运行时频繁注册

```csharp
// ❌ 不好：游戏运行时频繁注册
void Update()
{
    RegisterService(new TempService());  // 每帧创建
}

// ✅ 好：在初始化时一次性注册
protected override void Init()
{
    RegisterService(new PersistentService());
}
```

### 4. 检查 null 返回值

```csharp
// 获取可能不存在的服务
var service = container.Get<IOptionalService>();
if (service != null)
{
    service.DoSomething();
}
else
{
    GD.Print("Service not registered!");
}
```

### 5. 合理使用容器冻结

```csharp
// 在架构初始化完成后冻结容器，防止意外修改
protected override void OnInit()
{
    // 注册所有组件
    RegisterModel(new PlayerModel());
    RegisterSystem(new GameSystem());
    // ...
    
    // 冻结容器
    Container.Freeze();  // 此后无法再注册新组件
}
```

## 注意事项

1. **线程安全操作**：容器内部使用读写锁确保线程安全，无需额外同步

2. **容器冻结**：一旦调用 [Freeze](file:///d:/Project/Rider/GFramework/GFramework.Core/ioc/IocContainer.cs#L359-L372)
   方法，将不能再注册新实例

3. **单例注册限制
   **：[RegisterSingleton](file:///d:/Project/Rider/GFramework/GFramework.Core/ioc/IocContainer.cs#L84-L106)
   方法确保一个类型只能有一个实例，重复注册会抛出异常

4. **内存管理**：容器持有的实例不会自动释放，需要注意内存泄漏问题

5. **注册顺序**：组件的依赖关系需要手动保证，先注册被依赖的组件

## 相关包

- [`architecture`](./architecture.md) - 使用 IoC 容器管理所有组件
- [`model`](./model.md) - Model 通过 IoC 容器注册和获取
- [`system`](./system.md) - System 通过 IoC 容器注册和获取
- [`utility`](./utility.md) - Utility 通过 IoC 容器注册和获取
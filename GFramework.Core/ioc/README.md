# IoC 包使用说明

## 概述

IoC（Inversion of Control，控制反转）包提供了一个轻量级的依赖注入容器，用于管理框架中各种组件的注册和获取。通过 IoC 容器，可以实现组件间的解耦，便于测试和维护。

## 核心类

### [`IocContainer`](IocContainer.cs)

IoC 容器类，负责管理对象的注册和获取。

**主要功能：**
- 注册实例到容器
- 从容器中获取实例
- 类型安全的依赖管理

## 核心方法

### 1. Register<T>

注册一个实例到容器中。

```csharp
public void Register<T>(T instance)
```

**参数：**
- `instance`: 要注册的实例对象

**使用示例：**

```csharp
var container = new IocContainer();

// 注册各种类型的实例
container.Register<IPlayerModel>(new PlayerModel());
container.Register<IGameSystem>(new GameSystem());
container.Register<IStorageUtility>(new StorageUtility());
```

### 2. Get<T>

从容器中获取指定类型的实例。

```csharp
public T Get<T>() where T : class
```

**返回值：**
- 返回指定类型的实例，如果未找到则返回 `null`

**使用示例：**

```csharp
// 获取已注册的实例
var playerModel = container.Get<IPlayerModel>();
var gameSystem = container.Get<IGameSystem>();

// 如果类型未注册，返回 null
var unknownService = container.Get<IUnknownService>();  // null
```

## 在框架中的使用

### Architecture 中的应用

IoC 容器是 [`Architecture`](../architecture/Architecture.cs) 类的核心组件，用于管理所有的 System、Model 和 Utility。

```csharp
public abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
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
public class GameArchitecture : Architecture<GameArchitecture>
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
    // 使用字典存储类型到实例的映射
    private readonly Dictionary<Type, object> _mInstances = new();
    
    public void Register<T>(T instance)
    {
        var key = typeof(T);
        _mInstances[key] = instance;  // 注册或覆盖
    }
    
    public T Get<T>() where T : class
    {
        var key = typeof(T);
        if (_mInstances.TryGetValue(key, out var retInstance))
        {
            return retInstance as T;  // 类型转换
        }
        return null;
    }
}
```

### 注册流程

```
用户代码
   ↓
RegisterSystem<T>(system)
   ↓
IocContainer.Register<T>(system)
   ↓
Dictionary[typeof(T)] = system
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
Dictionary.TryGetValue(typeof(T))
   ↓
返回实例或 null
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

### 覆盖注册

```csharp
var container = new IocContainer();

// 首次注册
container.Register<IConfig>(new DefaultConfig());

// 后续注册会覆盖
container.Register<IConfig>(new CustomConfig());

// 获取到的是最后注册的实例
var config = container.Get<IConfig>();  // CustomConfig
```

## 设计特点

### 1. 简单轻量

- 只有两个核心方法：`Register` 和 `Get`
- 基于字典实现，性能高效
- 无复杂的依赖解析逻辑

### 2. 手动注册

- 需要显式注册每个组件
- 不支持自动依赖注入
- 完全可控的组件生命周期

### 3. 单例模式

- 每个类型只能注册一个实例
- 适合管理全局单例服务
- 后续注册会覆盖前面的实例

### 4. 类型安全

- 基于泛型，编译时类型检查
- 避免字符串键导致的错误
- IDE 友好，支持自动补全

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
public class GameArchitecture : Architecture<GameArchitecture>
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

## 注意事项

1. **类型唯一性**
   - 每个类型只能注册一个实例
   - 重复注册会覆盖之前的实例

2. **手动管理依赖顺序**
   - 组件的依赖关系需要手动保证
   - 先注册被依赖的组件

3. **无生命周期管理**
   - 实例一旦注册就一直存在
   - 需要手动管理实例的生命周期

4. **线程安全**
   - 当前实现非线程安全
   - 避免多线程同时访问

## 扩展可能性

如果需要更复杂的功能，可以扩展 `IocContainer`：

```csharp
// 支持工厂模式
public class AdvancedIocContainer : IocContainer
{
    private Dictionary<Type, Func<object>> _factories = new();
    
    public void RegisterFactory<T>(Func<T> factory) where T : class
    {
        _factories[typeof(T)] = () => factory();
    }
    
    public new T Get<T>() where T : class
    {
        // 先尝试获取实例
        var instance = base.Get<T>();
        if (instance != null) return instance;
        
        // 如果没有实例，尝试使用工厂创建
        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            return factory() as T;
        }
        
        return null;
    }
}
```

## 相关包

- [`architecture`](../architecture/README.md) - 使用 IoC 容器管理所有组件
- [`model`](../model/README.md) - Model 通过 IoC 容器注册和获取
- [`system`](../system/README.md) - System 通过 IoC 容器注册和获取
- [`utility`](../utility/README.md) - Utility 通过 IoC 容器注册和获取
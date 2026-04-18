# GFramework.Game

`GFramework.Game` 是 `GFramework` 面向游戏项目的运行时层。

它建立在 `GFramework.Core` 与 `GFramework.Game.Abstractions` 之上，提供可直接落地的实现与基类，覆盖静态内容配置、数据与存档、设置、场景路由、UI 路由、序列化、文件存储、状态机扩展等常见游戏运行时需求。

如果你的项目只需要“契约”而不想带入实现，请依赖 `GFramework.Game.Abstractions`。如果你的项目需要真正可运行的默认实现或基类，请依赖本包。

## 包定位

- 面向使用者的默认运行时实现层，不是纯接口包。
- 适合作为游戏项目启动层、基础设施层、引擎适配层之上的通用运行时底座。
- 当前最成熟、最明确的能力集中在：
  - 静态内容配置：`Config/`
  - 数据与存档：`Data/`
  - 设置系统：`Setting/`
  - 场景与 UI 路由基类：`Scene/`、`UI/`
  - 序列化与文件存储：`Serializer/`、`Storage/`

## 与相邻包的关系

- `GFramework.Core`
  - 本包直接依赖它。
  - 提供架构、上下文注入、事件、日志、系统/模型/utility 生命周期等底层能力。
- `GFramework.Game.Abstractions`
  - 本包的上游契约层。
  - 本包中的 `FileStorage`、`SettingsModel<TRepository>`、`SaveRepository<TSaveData>`、`YamlConfigLoader`、`SceneRouterBase`、`UiRouterBase` 等都在实现这里定义的接口。
- `GFramework.Game.SourceGenerators`
  - 主要与配置系统配合使用。
  - 当你需要 schema 驱动的 YAML 配表时，运行时包是 `GFramework.Game`，生成时代码由 source generators 补齐。
- 引擎适配包或项目内适配层
  - 本包提供的是“引擎无关”的核心逻辑和基类。
  - 真正和 Godot、Unity、MonoGame 等引擎对象打交道的工厂、根节点、资源注册表，通常在相邻引擎包或游戏项目内实现。
  - CoreGrid 的真实接法就是这样：配置文件 IO 由 `GFramework.Godot.Config` 适配，UI/Scene factory 与 root 由项目自己提供。

## 子系统地图

### `Config/`

面向静态游戏内容的只读配置系统。

- `YamlConfigLoader`
  - 从文件系统根目录加载 YAML 配置表，并注册到 `IConfigRegistry`
- `ConfigRegistry`
  - 运行时配置表注册表
- `GameConfigBootstrap`
  - 非 `Architecture` 场景下的官方配置启动入口
- `GameConfigModule`
  - `Architecture` 场景下的官方配置接入模块
- `InMemoryConfigTable<TKey, TValue>`
  - 配置表默认只读承载
- `YamlConfigHotReloadOptions`
  - 开发期热重载控制

这个子系统通常与 `GFramework.Game.SourceGenerators` 一起使用，而不是手写大量注册代码。

对应文档：

- [docs/zh-CN/game/config-system.md](../docs/zh-CN/game/config-system.md)
- [docs/zh-CN/game/index.md](../docs/zh-CN/game/index.md)

### `Data/`

面向可写业务数据、设置持久化与存档槽位的仓库实现。

- `DataRepository`
  - 一条 `IDataLocation` 对应一份持久化对象
- `UnifiedSettingsDataRepository`
  - 把多个设置 section 聚合到单一文件中
- `SaveRepository<TSaveData>`
  - 面向槽位存档，支持版本迁移链
- `SaveConfiguration`
  - 槽位目录、文件名、前缀等约定

CoreGrid 的真实用法：

- 设置持久化使用 `UnifiedSettingsDataRepository`
- 存档使用 `SaveRepository<GameSaveData>`
- 两者共用同一个底层存储 utility

对应文档：

- [docs/zh-CN/game/data.md](../docs/zh-CN/game/data.md)
- [docs/zh-CN/game/setting.md](../docs/zh-CN/game/setting.md)

### `Setting/`

设置生命周期编排层。

- `SettingsModel<TRepository>`
  - 管理 `ISettingsData` 实例、迁移、加载、保存、重置
  - 编排 applicator 的 `Apply`
- `SettingsSystem`
  - 面向业务代码暴露更直接的系统级入口
- `Setting/Events/*`
  - 设置初始化、应用、保存、重置相关事件

CoreGrid 的真实用法：

- 在模型模块中创建 `SettingsModel<ISettingsDataRepository>`
- 注册多个 applicator
- 启动时先 `InitializeAsync()`，再 `ApplyAll()`
- 退出时统一 `SaveAll()`

对应文档：

- [docs/zh-CN/game/setting.md](../docs/zh-CN/game/setting.md)

### `Storage/`

面向本地文件系统的基础存储。

- `FileStorage`
  - 基于目录与文件的 `IStorage` 实现
  - 负责路径清洗、细粒度锁、原子写入、层级 key 到目录结构的映射
- `ScopedStorage`
  - 为底层存储增加前缀作用域

这部分能力经常被 `DataRepository`、`SaveRepository<TSaveData>`、`UnifiedSettingsDataRepository` 复用。

对应文档：

- [docs/zh-CN/game/storage.md](../docs/zh-CN/game/storage.md)
- [GFramework.Game/Storage/ReadMe.md](./Storage/ReadMe.md)

### `Serializer/`

- `JsonSerializer`
  - 当前默认序列化实现
  - 同时可作为 `ISerializer` 与 `IRuntimeTypeSerializer`

它通常先于存储和数据仓库被注册。

对应文档：

- [docs/zh-CN/game/serialization.md](../docs/zh-CN/game/serialization.md)

### `Scene/` 与 `UI/`

面向游戏导航的可复用基类，不直接绑定具体引擎。

- `SceneRouterBase`
  - 依赖 `ISceneFactory`、`ISceneRoot`
  - 提供栈式场景路由与转换处理管道
- `UiRouterBase`
  - 依赖 `IUiFactory`、`IUiRoot`
  - 提供页面栈、Overlay/Modal/Toast 等层级 UI、输入动作分发、暂停联动
- `Scene/Handler/*`、`UI/Handler/*`
  - 默认转换处理器基类与日志处理器

CoreGrid 的真实用法：

- 项目自定义 `SceneRouter : SceneRouterBase`
- 项目自定义 `UiRouter : UiRouterBase`
- 工厂、注册表、root 都由项目或引擎适配层提供

对应文档：

- [docs/zh-CN/game/scene.md](../docs/zh-CN/game/scene.md)
- [docs/zh-CN/game/ui.md](../docs/zh-CN/game/ui.md)

### `Routing/` 与 `State/`

- `Routing/RouterBase<TRoute, TContext>`
  - Scene/UI 路由共享基类
- `State/GameStateMachineSystem`
  - 对核心状态机系统的游戏向封装

这两部分一般被上层子系统消费，不是多数项目的第一接入点。

## 最小接入路径

下面按最常见的四种接入目标给出最短路径。

### 1. 只想先拿到文件存储

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

ISerializer serializer = new JsonSerializer();
IStorage storage = new FileStorage("GameData", serializer);

await storage.WriteAsync("player/profile", new { Name = "Alice", Level = 3 });
```

如果你需要逻辑隔离，再包一层 `ScopedStorage`：

```csharp
var settingsStorage = new ScopedStorage(storage, "settings");
```

### 2. 接入设置和存档

运行时最小拼装顺序通常是：

1. 注册 `JsonSerializer`
2. 注册一个 `IStorage` 实现
3. 注册 `ISettingsDataRepository`
4. 创建并注册 `SettingsModel<ISettingsDataRepository>`
5. 注册 applicator
6. 注册 `SettingsSystem`
7. 注册 `ISaveRepository<TSaveData>`

示意代码：

```csharp
var serializer = new JsonSerializer();
var storage = new FileStorage("GameData", serializer);

architecture.RegisterUtility(serializer);
architecture.RegisterUtility<IStorage>(storage);

architecture.RegisterUtility<ISettingsDataRepository>(
    new UnifiedSettingsDataRepository(
        storage,
        serializer,
        new DataRepositoryOptions { BasePath = "settings", AutoBackup = true }));

architecture.RegisterModel(
    new SettingsModel<ISettingsDataRepository>(
        new MySettingsLocationProvider(),
        architecture.Context.GetUtility<ISettingsDataRepository>())
        .RegisterApplicator(new MyAudioSettingsApplicator()));

architecture.RegisterSystem<ISettingsSystem>(new SettingsSystem());

architecture.RegisterUtility<ISaveRepository<MySaveData>>(
    new SaveRepository<MySaveData>(
        storage,
        new SaveConfiguration
        {
            SaveRoot = "saves",
            SaveSlotPrefix = "slot_",
            SaveFileName = "save.json"
        }));
```

启动时：

```csharp
await settingsModel.InitializeAsync();
await settingsSystem.ApplyAll();
```

退出前：

```csharp
await settingsSystem.SaveAll();
```

CoreGrid 目前就是按这个思路接入，只是底层存储换成了 Godot 适配实现。

### 3. 接入静态 YAML 配置

如果你不走 `Architecture` 生命周期，直接使用 `GameConfigBootstrap`：

```csharp
var bootstrap = new GameConfigBootstrap(
    new GameConfigBootstrapOptions
    {
        RootPath = contentRootPath,
        ConfigureLoader = static loader =>
        {
            loader.RegisterAllGeneratedConfigTables();
        }
    });

await bootstrap.InitializeAsync();

var registry = bootstrap.Registry;
```

如果你走 `Architecture`，优先使用 `GameConfigModule`，并在较早阶段安装。

这一能力几乎总是与 source generators 绑定使用。目录、schema、生成器与热重载约定请直接看：

- [docs/zh-CN/game/config-system.md](../docs/zh-CN/game/config-system.md)

### 4. 接入 Scene / UI 路由

这里的最小前提不是“直接 new 一个 router”，而是先补齐运行时依赖：

- `ISceneFactory` / `IUiFactory`
- `ISceneRoot` / `IUiRoot`
- 具体页面或场景行为实现

然后让项目自己的 router 继承基类：

```csharp
public sealed class MySceneRouter : SceneRouterBase
{
    protected override void RegisterHandlers()
    {
        RegisterHandler(new LoggingTransitionHandler());
    }
}

public sealed class MyUiRouter : UiRouterBase
{
    protected override void RegisterHandlers()
    {
        RegisterHandler(new GFramework.Game.UI.Handler.LoggingTransitionHandler());
    }
}
```

这类 router 适合作为你的项目层或引擎适配层代码，而不是直接修改本包。

## CoreGrid 里的真实用法线索

当前仓库内，CoreGrid 对本包的使用大致分成三层：

- 配置
  - `CoreGridConfigHost` 使用生成表元数据与 YAML loader 完成配置注册
- 设置与存档
  - `UtilityModule` 注册序列化器、底层存储、`UnifiedSettingsDataRepository`、`SaveRepository<GameSaveData>`
  - `ModelModule` 创建 `SettingsModel<ISettingsDataRepository>` 并注册 applicator
- 路由
  - `SceneRouter` 继承 `SceneRouterBase`
  - `UiRouter` 继承 `UiRouterBase`

这说明本包更适合做“游戏基础设施层”，而不是把所有引擎对象耦死在包内部。

## 文档入口

- 游戏模块总览：[docs/zh-CN/game/index.md](../docs/zh-CN/game/index.md)
- 内容配置系统：[docs/zh-CN/game/config-system.md](../docs/zh-CN/game/config-system.md)
- 数据与存档：[docs/zh-CN/game/data.md](../docs/zh-CN/game/data.md)
- 设置系统：[docs/zh-CN/game/setting.md](../docs/zh-CN/game/setting.md)
- 存储系统：[docs/zh-CN/game/storage.md](../docs/zh-CN/game/storage.md)
- 序列化系统：[docs/zh-CN/game/serialization.md](../docs/zh-CN/game/serialization.md)
- 场景系统：[docs/zh-CN/game/scene.md](../docs/zh-CN/game/scene.md)
- UI 系统：[docs/zh-CN/game/ui.md](../docs/zh-CN/game/ui.md)

## 什么时候不该直接依赖本包

以下场景优先考虑只依赖 `GFramework.Game.Abstractions`：

- 你在做纯领域层、协议层或可复用 feature 包，只想引用接口和数据契约
- 你已经有自己的配置、存储、路由实现，只想复用统一契约
- 你不希望业务程序集带入 `Newtonsoft.Json`、`YamlDotNet` 等运行时依赖

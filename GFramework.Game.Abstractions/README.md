# GFramework.Game.Abstractions

`GFramework.Game.Abstractions` 是 `GFramework` 游戏层的契约包。

它建立在 `GFramework.Core.Abstractions` 之上，只定义接口、枚举、路由上下文、事件契约和少量可直接复用的数据类型，不提供完整运行时实现。它的主要用途是把游戏业务层、引擎适配层、公共 feature 包与具体实现解耦。

如果你需要可直接运行的默认实现，请改为依赖 `GFramework.Game`。

## 包定位

- 为游戏相关子系统提供稳定契约，而不是默认实现。
- 适合被多个程序集共同引用，避免公共业务层直接依赖具体运行时。
- 典型使用场景：
  - 定义 `IScene`、`IUiPage`、`ISettingsData`、`IData` 等业务对象
  - 让 feature 包只感知 `IConfigRegistry`、`ISaveRepository<T>`、`ISettingsModel`、`IUiRouter`、`ISceneRouter`
  - 在引擎适配层之外共享设置、场景参数、UI 参数、存档数据类型

## 与相邻包的关系

- `GFramework.Core.Abstractions`
  - 本包直接依赖它。
  - 提供 `ISystem`、`IModel`、`IUtility`、上下文 utility 等底层抽象。
- `GFramework.Game`
  - 本包的主要实现层。
  - `FileStorage`、`ScopedStorage`、`JsonSerializer`、`SettingsModel<TRepository>`、`SaveRepository<TSaveData>`、`SceneRouterBase`、`UiRouterBase`、`YamlConfigLoader` 等都在实现这里的契约。
- 引擎适配包或项目代码
  - `IUiFactory`、`ISceneFactory`、`IUiRoot`、`ISceneRoot`、资源注册表等通常由引擎适配层或游戏项目自己实现。
  - 常见做法也是这样组织：页面 / 场景 factory、root、registry 在项目层，运行时基类和契约来自 `GFramework.Game` 与本包。

## 子系统地图

### `Config/`

静态内容配置的读取侧契约。

- `IConfigLoader`
- `IConfigRegistry`
- `IConfigTable`
- `ConfigLoadException`
- `ConfigLoadDiagnostic`
- `ConfigLoadFailureKind`

这一层只描述“如何注册与访问配置表”，不关心底层来自 YAML、二进制还是远程源。

### `Data/`

可写数据与存档契约。

- `IData`
- `IVersionedData`
- `IDataLocation`
- `IDataLocationProvider`
- `IDataRepository`
- `ISettingsDataRepository`
- `ISaveRepository<TSaveData>`
- `ISaveMigration<TSaveData>`
- `DataRepositoryOptions`
- `Data/Events/*`

这一层让业务代码不需要知道数据最终按“单文件一项”还是“统一文件多 section”持久化。

### `Setting/`

设置系统契约。

- `ISettingsData`
- `IResetApplyAbleSettings`
- `ISettingsModel`
- `ISettingsSystem`
- `ISettingsMigration`
- `ISettingsChangedEvent`
- `Setting/Data/*`
  - 内置了 `AudioSettings`、`GraphicsSettings`、`LocalizationSettings` 三类常见设置数据

这里定义的是“设置生命周期和应用语义”，不限定具体引擎。

### `Scene/`

场景导航契约。

- `IScene`
- `ISceneBehavior`
- `ISceneFactory`
- `ISceneRoot`
- `ISceneRouter`
- `ISceneTransitionHandler`
- `ISceneAroundTransitionHandler`
- `ISceneRouteGuard`
- `IGameSceneRegistry<T>`

如果你想把场景定义放在公共业务层，通常依赖本包就够了。

### `UI/`

UI 页面与路由契约。

- `IUiPage`
- `IUiPageBehavior`
- `IUiFactory`
- `IUiRoot`
- `IUiRouter`
- `IUiTransitionHandler`
- `IUiAroundTransitionHandler`
- `IUiRouteGuard`
- `UiHandle`
- `UiTransitionHandlerOptions`
- `UiInteractionProfile`

`IUiRouter` 不只覆盖页面栈，还覆盖 Overlay / Modal / Toast / Topmost 等层级 UI 语义。

### `Routing/`

- `IRoute`
- `IRouteContext`
- `IRouteGuard<TRoute>`

Scene 与 UI 路由共享这套基础约定。

### `Storage/`

- `IFileStorage`
- `IScopedStorage`

它们继承自核心层的 `IStorage`，用于表达“文件存储实现”和“带作用域前缀的存储实现”这两个角色。

### `Asset/` 与 `Enums/`

- `Asset/`
  - 资源注册表契约，如 `IAssetRegistry<T>`
- `Enums/`
  - UI/Scene 转场、UI 层级、输入动作、存储类型等公共枚举

## XML 阅读入口

下面这份目录视图用于帮助你建立 `GFramework.Game.Abstractions` 的契约层阅读顺序；更细的参数、返回值、异常和生命周期说明，建议继续回到具体类型与成员确认。

| 契约族 | 代表类型 | 阅读重点 |
| --- | --- | --- |
| `Config/` | `IConfigLoader`、`IConfigRegistry`、`IConfigTable<TKey, TValue>`、`ConfigLoadException` | 看配置表注册、读取约定和失败诊断模型 |
| `Data/` | `IDataRepository`、`ISettingsDataRepository`、`ISaveRepository<TSaveData>`、`DataRepositoryOptions` | 看业务数据、设置持久化、槽位存档和版本迁移契约 |
| `Setting/` | `ISettingsData`、`ISettingsModel`、`ISettingsSystem`、`LocalizationSettings` | 看设置数据、应用语义、迁移接口和内置设置对象 |
| `Scene/` | `IScene`、`ISceneRouter`、`ISceneFactory`、`SceneTransitionEvent` | 看场景行为、路由、工厂 / root 边界与转场事件模型 |
| `UI/` | `IUiPage`、`IUiRouter`、`IUiFactory`、`UiInteractionProfile`、`UiTransitionHandlerOptions` | 看页面栈、层级 UI、输入动作与 UI 转场契约 |
| `Routing/` `Storage/` `Asset/` `Enums/` | `IRoute`、`IRouteContext`、`IFileStorage`、`IAssetRegistry<T>`、`UiLayer`、`SceneTransitionType` | 看公共路由上下文、存储角色、资源注册表与跨层共享枚举 |

## 最小接入路径

### 1. 只想在公共业务层声明游戏对象

直接依赖本包，定义业务数据和交互参数：

```csharp
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;

public sealed class GameSaveData : IVersionedData
{
    public int Version { get; set; } = 1;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public sealed class GameplayEnterParam : ISceneEnterParam
{
    public required string Seed { get; init; }
}

public sealed class PauseMenuParam : IUiPageEnterParam
{
    public bool AllowRestart { get; init; }
}
```

这个阶段不需要把 `GFramework.Game` 一起带进来。

### 2. 只想让 feature 包依赖抽象，不绑定具体实现

直接面向接口编程：

```csharp
public sealed class ContinueGameCommandHandler
{
    private readonly ISaveRepository<GameSaveData> _saveRepository;
    private readonly ISceneRouter _sceneRouter;

    public ContinueGameCommandHandler(
        ISaveRepository<GameSaveData> saveRepository,
        ISceneRouter sceneRouter)
    {
        _saveRepository = saveRepository;
        _sceneRouter = sceneRouter;
    }
}
```

这样 feature 包不必知道底层到底是 `SaveRepository<TSaveData>`、Godot 适配层，还是你自己的实现。

### 3. 什么时候要升级到 `GFramework.Game`

一旦你需要下面任一项，就不该只停留在本包：

- 默认的 JSON 序列化实现
- 文件系统存储实现
- 设置模型与系统实现
- 槽位存档仓库实现
- YAML 配置加载器
- Scene / UI 路由基类

也就是说，本包回答的是“项目各层如何约定”，`GFramework.Game` 回答的是“这些约定默认怎么跑起来”。

## 典型分层方式

典型项目对本包的使用方式，通常能清楚体现它的职责边界：

- 公共脚本广泛引用：
  - `IUiRouter`
  - `ISceneRouter`
  - `ISettingsModel`
  - `ISettingsSystem`
  - `ISaveRepository<GameSaveData>`
  - `IConfigRegistry`
- 业务数据和参数实现引用：
  - `IData` / `IVersionedData`
  - `ISceneEnterParam`
  - `IUiPageEnterParam`
- 真正的实现和装配则放在：
  - `GFramework.Game`
  - `GFramework.Godot.*`
  - 项目自己的模块、factory、root、registry

这正是本包的设计目标：让业务层依赖稳定契约，而不是依赖具体运行时细节。

## 对应文档入口

虽然大部分详细文档写在 `GFramework.Game` 侧，但阅读顺序仍然适用于本包：

- 游戏模块总览：[Game 模块总览](../docs/zh-CN/game/index.md)
- 内容配置系统：[内容配置系统](../docs/zh-CN/game/config-system.md)
- 数据与存档：[数据与存档系统](../docs/zh-CN/game/data.md)
- 设置系统：[设置系统](../docs/zh-CN/game/setting.md)
- 存储系统：[存储系统](../docs/zh-CN/game/storage.md)
- 序列化系统：[序列化系统](../docs/zh-CN/game/serialization.md)
- 场景系统：[场景系统](../docs/zh-CN/game/scene.md)
- UI 系统：[UI 系统](../docs/zh-CN/game/ui.md)

## 选择建议

- 选 `GFramework.Game.Abstractions`
  - 你在写共享业务层、公共 feature 包、纯契约层
- 选 `GFramework.Game`
  - 你需要默认实现、基础设施拼装、运行时启动入口
- 两者一起用
  - 最常见。公共层依赖 abstractions，应用层或引擎层依赖 runtime

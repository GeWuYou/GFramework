---
title: 设置系统
description: 以当前 SettingsModel、SettingsSystem 与相关测试为准，说明设置数据、applicator、迁移和持久化的真实接法。
---

# 设置系统

`GFramework.Game` 的设置系统负责三件事：

- 管理 `ISettingsData` 实例的生命周期
- 管理设置 applicator，并把设置真正作用到运行时环境
- 在初始化时加载、迁移、保存和重置设置

当前默认 owner 是：

- `SettingsModel<TRepository>`
- `SettingsSystem`

而不是旧文档里那种“只靠若干 `Get<T>() / Register(...)` 辅助方法就能自动完成一切”的模型。

## 当前公开入口

### `ISettingsData`

设置数据对象需要同时承担：

- 默认值持有者
- 版本化 section
- 从已加载数据回填到当前实例的入口

当前接口组合是：

```csharp
public interface ISettingsData : IResettable, IVersionedData, ILoadableFrom<ISettingsData>;
```

这意味着一个设置数据类型至少要处理：

- `Reset()`
- `Version`
- `LastModified`
- `LoadFrom(ISettingsData source)`

### `IResetApplyAbleSettings`

applicator 的职责不是保存数据，而是把设置结果作用到实际运行时对象。

它当前需要暴露：

- `Data`
- `DataType`
- `Reset()`
- `ApplyAsync()`

典型场景包括：

- 把音量设置同步到音频系统
- 把画质设置同步到窗口或渲染配置
- 把语言设置同步到本地化服务

### `SettingsModel<TRepository>`

这是当前设置系统的核心编排器。按当前源码，它负责：

- `GetData<T>()`
  - 返回某个设置类型的唯一实例
- `RegisterApplicator(...)`
  - 注册 applicator，并把其 `Data` 一并纳入模型管理
- `RegisterMigration(...)`
  - 注册同一设置类型的前进式迁移链
- `InitializeAsync()`
  - 从 repository 读取所有设置、执行迁移、回填到当前实例
- `SaveAllAsync()`
  - 持久化所有已登记的设置数据
- `ApplyAllAsync()`
  - 依次应用所有 applicator
- `Reset<T>() / ResetAll()`
  - 重置单个或全部设置

### `SettingsSystem`

`SettingsSystem` 是面向业务代码更直接的一层系统封装：

- `ApplyAll()`
- `Apply<T>()`
- `SaveAll()`
- `Reset<T>()`
- `ResetAll()`

它自己不持有独立设置状态，而是把工作委托给 `ISettingsModel`，并在应用时补发 settings 相关事件。

## 初始化与迁移的真实语义

`SettingsModel<TRepository>.InitializeAsync()` 的当前行为，比旧文档里“加载一下就好”更严格一些：

- 它会先调用 `ISettingsDataRepository.LoadAllAsync()`
- 再逐个匹配当前模型里已经登记的设置类型
- 如果读到了旧版本设置，会以“当前内存实例声明的 `Version`”为目标版本执行迁移
- 迁移完成后通过 `LoadFrom(...)` 回填到现有实例，而不是直接替换对象引用

当前测试还确认了几个关键边界：

- 同一设置类型的同一个 `FromVersion` 不能重复注册迁移器
- 注册新迁移器后，类型级迁移缓存会失效并重建，不会继续使用旧快照
- 如果迁移链缺口导致无法安全升级，模型会保留当前内存中的最新实例，而不是把不完整的旧数据覆盖进来
- 单个设置 section 初始化失败时，模型会记录错误并继续处理其他 section

这套语义更接近“尽量保证运行时实例总是可用”，而不是“任意旧设置都必须成功导入”。

## 最小接入路径

当前最常见的接法是：

1. 准备一个 `IStorage`
2. 准备一个 `IRuntimeTypeSerializer`
3. 注册 `ISettingsDataRepository`
4. 注册 `IDataLocationProvider`
5. 创建并注册 `SettingsModel<TRepository>`
6. 注册 applicator
7. 注册 `SettingsSystem`

示意代码：

```csharp
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Setting;
using GFramework.Game.Storage;

var serializer = new JsonSerializer();
var storage = new FileStorage("GameData", serializer, ".json");

var repository = new UnifiedSettingsDataRepository(
    storage,
    serializer,
    new DataRepositoryOptions
    {
        BasePath = "settings",
        AutoBackup = true
    });

architecture.RegisterUtility<IStorage>(storage);
architecture.RegisterUtility<IRuntimeTypeSerializer>(serializer);
architecture.RegisterUtility<ISettingsDataRepository>(repository);
// 此处注册项目侧的 IDataLocationProvider 实现，用于把设置类型映射到 section key。

var settingsModel = new SettingsModel<ISettingsDataRepository>(null, null);
// 在注册到架构前，继续补 applicator 与 migration。

architecture.RegisterModel<ISettingsModel>(settingsModel);
architecture.RegisterSystem<ISettingsSystem>(new SettingsSystem());
```

启动阶段通常是：

```csharp
await settingsModel.InitializeAsync();
await settingsModel.ApplyAllAsync();
```

退出或显式保存时：

```csharp
await settingsModel.SaveAllAsync();
```

## `GetData<T>()` 和 `RegisterApplicator(...)` 的分工

这两个入口经常被混用，但职责不同：

- `GetData<T>()`
  - 只保证某个设置数据实例存在，并在 repository / location provider 已就绪时把类型注册回去
- `RegisterApplicator(...)`
  - 同时注册 applicator 和 applicator 绑定的 `Data`

如果一个设置类型需要真正作用到运行时对象，推荐让它通过 applicator 进入模型；这样 `ApplyAllAsync()`、`ResetAll()` 和
`SettingsSystem` 才能完整覆盖到它。

## 与 repository 的关系

设置系统默认不是直接写文件，而是依赖 `ISettingsDataRepository`。

当前仓库里更推荐的默认实现是 `UnifiedSettingsDataRepository`，原因很直接：

- 多个设置 section 会被聚合到同一份统一文件
- 启动时能一次性 `LoadAllAsync()`
- `AutoBackup` 针对整个统一文件生效，更贴近“设置快照”的真实语义

如果你的项目明确需要“一类设置一个独立文件”，才考虑回到通用 `DataRepository` 路径。

## 当前边界

- `SettingsModel<TRepository>` 负责数据生命周期，`SettingsSystem` 负责系统级调用入口；两者不要混成一个巨型服务
- applicator 决定“怎么把数据应用到宿主”，repository 决定“怎么保存数据”，两层职责不要互相侵入
- 设置迁移和存档迁移是两条不同管线；后者看 [`data.md`](./data.md) 里的 `SaveRepository<TSaveData>`

## 继续阅读

1. [数据与存档系统](./data.md)
2. [存储系统](./storage.md)
3. [Game 入口](./index.md)

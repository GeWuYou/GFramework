# 设置系统

设置系统负责管理 `ISettingsData`、持久化加载/保存，以及把设置真正应用到运行时环境。

当前实现以 `SettingsModel<TRepository>` 和 `SettingsSystem` 为核心，已经不是旧文档中的
`Get<T>() / Register(IApplyAbleSettings)` 接口模型。

## 核心概念

### ISettingsData

设置数据对象负责保存设置值、提供默认值，并在加载后把外部数据回填到当前实例。

```csharp
public interface ISettingsData : IResettable, IVersionedData, ILoadableFrom<ISettingsData>;
```

这意味着一个设置数据类型通常需要实现：

- `Reset()`：恢复默认值
- `Version` / `LastModified`：暴露版本化信息
- `LoadFrom(ISettingsData)`：把已加载或迁移后的数据复制到当前实例

### IResetApplyAbleSettings

应用器负责把设置数据作用到引擎或运行时环境：

```csharp
public interface IResetApplyAbleSettings : IResettable, IApplyAbleSettings
{
    ISettingsData Data { get; }
    Type DataType { get; }
}
```

常见用途包括：

- 把音量设置同步到音频总线
- 把图形设置同步到窗口系统
- 把语言设置同步到本地化管理器

## ISettingsModel

当前 `ISettingsModel` 的主要 API 如下：

```csharp
public interface ISettingsModel : IModel
{
    bool IsInitialized { get; }

    T GetData<T>() where T : class, ISettingsData, new();
    IEnumerable<ISettingsData> AllData();

    ISettingsModel RegisterApplicator<T>(T applicator)
        where T : class, IResetApplyAbleSettings;
    T? GetApplicator<T>() where T : class, IResetApplyAbleSettings;
    IEnumerable<IResetApplyAbleSettings> AllApplicators();

    ISettingsModel RegisterMigration(ISettingsMigration migration);

    Task InitializeAsync();
    Task SaveAllAsync();
    Task ApplyAllAsync();
    void Reset<T>() where T : class, ISettingsData, new();
    void ResetAll();
}
```

行为说明：

- `GetData<T>()` 返回某个设置数据的唯一实例
- `RegisterApplicator<T>()` 注册应用器，并把其 `Data` 纳入模型管理
- `InitializeAsync()` 从 `ISettingsDataRepository` 读取所有已注册设置，并在需要时执行迁移
- `SaveAllAsync()` 持久化当前所有设置数据
- `ApplyAllAsync()` 依次调用所有 applicator 的 `ApplyAsync()`

## SettingsSystem

`SettingsSystem` 是对模型的系统级封装，面向业务代码提供更直接的入口：

```csharp
public interface ISettingsSystem : ISystem
{
    Task ApplyAll();
    Task Apply<T>() where T : class, IResetApplyAbleSettings;
    Task SaveAll();
    Task Reset<T>() where T : class, ISettingsData, IResetApplyAbleSettings, new();
    Task ResetAll();
}
```

它不会自己保存数据，而是把保存、重置和应用逻辑委托给 `ISettingsModel`。

## 基本用法

### 定义设置数据

```csharp
public sealed class GameplaySettings : ISettingsData
{
    public float GameSpeed { get; set; } = 1.0f;

    public int Version { get; private set; } = 1;
    public DateTime LastModified { get; } = DateTime.UtcNow;

    public void Reset()
    {
        GameSpeed = 1.0f;
    }

    public void LoadFrom(ISettingsData source)
    {
        if (source is not GameplaySettings settings)
        {
            return;
        }

        GameSpeed = settings.GameSpeed;
        Version = settings.Version;
    }
}
```

### 定义 applicator

```csharp
public sealed class GameplaySettingsApplicator : IResetApplyAbleSettings
{
    public GameplaySettingsApplicator(GameplaySettings data)
    {
        Data = data;
    }

    public ISettingsData Data { get; }
    public Type DataType => typeof(GameplaySettings);

    public void Reset()
    {
        Data.Reset();
    }

    public Task ApplyAsync()
    {
        var settings = (GameplaySettings)Data;
        TimeScale.Current = settings.GameSpeed;
        return Task.CompletedTask;
    }
}
```

### 使用模型和系统

```csharp
var settingsModel = this.GetModel<ISettingsModel>();

var gameplayData = settingsModel.GetData<GameplaySettings>();
gameplayData.GameSpeed = 1.25f;

settingsModel.RegisterApplicator(new GameplaySettingsApplicator(gameplayData));

await settingsModel.InitializeAsync();
await settingsModel.SaveAllAsync();

var settingsSystem = this.GetSystem<ISettingsSystem>();
await settingsSystem.ApplyAll();
```

## 迁移

设置系统内建了迁移注册入口：

```csharp
public interface ISettingsMigration
{
    Type SettingsType { get; }
    int FromVersion { get; }
    int ToVersion { get; }
    ISettingsData Migrate(ISettingsData oldData);
}
```

当 `InitializeAsync()` 读取到旧版本设置时，会按已注册迁移链逐步升级，再通过 `LoadFrom` 回填到当前实例。

迁移规则如下：

- 同一个设置类型的同一个 `FromVersion` 只能注册一个迁移器
- `ToVersion` 必须严格大于 `FromVersion`
- `InitializeAsync()` 会以当前运行时代码里该设置实例的 `Version` 作为目标版本
- 如果迁移链缺口、迁移结果类型不兼容、迁移结果版本与声明不一致，或者读取到比当前运行时更高的版本，当前设置节不会覆盖内存中的最新实例，并会记录错误日志
- 与 `SaveRepository<TSaveData>` 不同，设置初始化阶段会跳过失败的设置节并继续处理其他设置节，而不是把异常继续向外抛出

## 依赖项

要让设置系统完整工作，通常需要准备：

- `ISettingsDataRepository`
- `IDataLocationProvider`
- 一个具体的存储实现和序列化器

如果使用 `UnifiedSettingsDataRepository`，多个设置节会被合并到单个设置文件中统一保存。

## 当前边界

- 设置迁移是内建能力
- 设置持久化是内建能力
- 设置如何应用到具体引擎由 applicator 决定
- 存档系统也支持内建版本迁移，但入口位于 `ISaveRepository<T>.RegisterMigration(...)`，语义是槽位存档升级而不是设置节初始化

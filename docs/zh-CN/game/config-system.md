# 游戏内容配置系统

> 面向静态游戏内容的 AI-First 配表方案

该配置系统用于管理怪物、物品、技能、任务等静态内容数据。

它与 `GFramework.Core.Configuration` 不同，后者面向运行时键值配置；它也不同于 `GFramework.Game.Setting`，后者面向玩家设置和持久化。

## 当前能力

- YAML 作为配置源文件
- JSON Schema 作为结构描述
- 一对象一文件的目录组织
- 运行时只读查询
- Runtime / Generator / Tooling 共享支持 `enum`、`const`、`not`、`minimum`、`maximum`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`minLength`、`maxLength`、`pattern`、`format`（当前稳定子集：`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`）、`minItems`、`maxItems`、`uniqueItems`、`contains`、`minContains`、`maxContains`、`minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf`
- Source Generator 生成配置类型、表包装、单表注册/访问辅助，以及项目级聚合注册目录
- VS Code 插件提供配置浏览、raw 编辑、schema 打开、递归轻量校验和嵌套对象表单入口

## 推荐目录结构

```text
GameProject/
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
├─ schemas/
│  ├─ monster.schema.json
│  └─ item.schema.json
```

## Schema 示例

```json
{
  "title": "Monster Config",
  "description": "定义怪物静态配置。",
  "x-gframework-config-path": "config/monster",
  "type": "object",
  "required": ["id", "name"],
  "properties": {
    "id": {
      "type": "integer",
      "description": "怪物主键。"
    },
    "name": {
      "type": "string",
      "title": "Monster Name",
      "description": "怪物显示名。",
      "default": "Slime"
    },
    "hp": {
      "type": "integer",
      "default": 10,
      "multipleOf": 5
    },
    "rarity": {
      "type": "string",
      "enum": ["common", "rare", "boss"]
    },
    "dropItems": {
      "type": "array",
      "description": "掉落物品表主键。",
      "uniqueItems": true,
      "items": {
        "type": "string",
        "enum": ["potion", "slime_gel", "bomb"]
      },
      "x-gframework-ref-table": "item"
    }
  }
}
```

顶层可选元数据：

- `x-gframework-config-path`：覆盖生成器默认的配置目录。未声明时，默认使用 schema 基名，例如
  `monster.schema.json -> monster`

例如项目希望继续把 YAML 放在 `config/monster/*.yaml` 下，而不是根目录 `monster/*.yaml`，可以这样声明：

```json
{
  "type": "object",
  "x-gframework-config-path": "config/monster",
  "required": ["id"],
  "properties": {
    "id": { "type": "integer" }
  }
}
```

约束如下：

- 必须是 JSON 字符串
- 必须是相对路径
- 不允许包含 `.` 或 `..` 段，也不能写成绝对路径
- 生成器会把反斜杠标准化为 `/`

## YAML 示例

```yaml
id: 1
name: Slime
hp: 10
dropItems:
  - potion
  - slime_gel
```

## 推荐接入模板

如果你准备在一个真实游戏项目里首次接入这套配置系统，建议直接采用下面这套目录与启动模板，而不是零散拼装。

### 目录模板

```text
GameProject/
├─ GameProject.csproj
├─ Config/
│  ├─ GameConfigHost.cs
│  └─ GameConfigRuntime.cs
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
└─ schemas/
   ├─ monster.schema.json
   └─ item.schema.json
```

推荐约定如下：

- `schemas/` 放所有 `*.schema.json`，由 Source Generator 自动拾取
- `config/` 放运行时加载的 YAML 数据，一对象一文件
- `Config/` 放你自己的接入代码，例如启动注册、热重载句柄和对外读取入口

### `csproj` 模板

如果你在仓库内直接用项目引用，最小模板可以写成下面这样：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\GFramework.Game\GFramework.Game.csproj" />
    <ProjectReference Include="..\GFramework.Core.SourceGenerators.Abstractions\GFramework.Core.SourceGenerators.Abstractions.csproj" />
    <ProjectReference Include="..\GFramework.SourceGenerators.Common\GFramework.SourceGenerators.Common.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\GFramework.Core.SourceGenerators\GFramework.Core.SourceGenerators.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\GFramework.Game.SourceGenerators\GFramework.Game.SourceGenerators.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

  <Import Project="..\GFramework.Game.SourceGenerators\GeWuYou.GFramework.Game.SourceGenerators.targets" />
</Project>
```

这段配置的作用：

- `GFramework.Game` 提供运行时 `YamlConfigLoader`、`ConfigRegistry`、`GameConfigBootstrap`、`GameConfigModule` 和只读表实现
- `GFramework.Core.SourceGenerators.Abstractions` 提供 `Core` 侧 source-generator attributes
- `GFramework.SourceGenerators.Common`、`GFramework.Core.SourceGenerators` 与 `GFramework.Game.SourceGenerators`
  共同把生成器接进当前消费者项目
- `GeWuYou.GFramework.Game.SourceGenerators.targets` 自动把 `schemas/**/*.schema.json` 加入 `AdditionalFiles`

如果你使用打包后的 NuGet，而不是仓库内项目引用，原则保持不变：

- 运行时项目需要引用 `GeWuYou.GFramework.Game`
- 配置 schema 生成器需要引用 `GeWuYou.GFramework.Game.SourceGenerators`
- 如果同一项目还会使用 `[Log]`、`[ContextAware]`、`[GetSystem]` 等 Core 侧生成器特性，再额外引用
  `GeWuYou.GFramework.Core.SourceGenerators`
- schema 目录默认仍然是 `schemas/`

如果你的 schema 不放在默认目录，可以在项目文件里覆盖：

```xml
<PropertyGroup>
  <GFrameworkConfigSchemaDirectory>GameSchemas</GFrameworkConfigSchemaDirectory>
</PropertyGroup>
```

### 官方启动帮助器

`GFramework.Game` 现在内置 `GameConfigBootstrap` 与 `GameConfigBootstrapOptions`，用于把 `ConfigRegistry`、`YamlConfigLoader`、`LoadAsync` 和热重载句柄收敛到一个正式的 C# 入口中。

推荐直接组合这个帮助器，而不是继续在消费者项目里复制文档模板：

```csharp
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var bootstrap = new GameConfigBootstrap(
    new GameConfigBootstrapOptions
    {
        RootPath = configRootPath,
        ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables(),
        EnableHotReload = true,
        HotReloadOptions = new YamlConfigHotReloadOptions
        {
            OnTableReloaded = tableName => Console.WriteLine($"Reloaded config table: {tableName}"),
            OnTableReloadFailed = static (_, exception) =>
            {
                var diagnostic = (exception as ConfigLoadException)?.Diagnostic;
                Console.WriteLine($"Config reload failed: {diagnostic?.FailureKind}");
            }
        }
    });

await bootstrap.InitializeAsync();

var registry = bootstrap.Registry;
var monsterTable = registry.GetMonsterTable();
var slime = monsterTable.Get(1);

bootstrap.Dispose();
```

如果你希望把它继续包装进自己的进程级入口，也建议只包一层生命周期壳，而不是重新拼装底层加载器。为了避免和后面的“运行时读取模板”冲突，推荐明确拆成两类文件：

- `GameConfigHost.cs` 负责生命周期管理、初始化和热重载
- `GameConfigRuntime.cs` 负责把已初始化的 `IConfigRegistry` 封装成业务层读取入口

如果你采用这套双层模板，建议把上面的生命周期壳文件命名为 `GameConfigHost.cs`，并把类型名同步改成 `GameConfigHost`：

```csharp
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

namespace GameProject.Config;

/// <summary>
///     封装当前游戏进程的配置启动生命周期。
/// </summary>
public sealed class GameConfigHost : IDisposable
{
    private readonly GameConfigBootstrap _bootstrap;

    /// <summary>
    ///     使用指定配置根目录创建运行时入口。
    /// </summary>
    /// <param name="configRootPath">配置根目录。</param>
    public GameConfigHost(string configRootPath)
    {
        _bootstrap = new GameConfigBootstrap(
            new GameConfigBootstrapOptions
            {
                RootPath = configRootPath,
                ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables()
            });
    }

    /// <summary>
    ///     获取共享配置注册表。
    /// </summary>
    public IConfigRegistry Registry => _bootstrap.Registry;

    /// <summary>
    ///     执行初次配置加载。
    /// </summary>
    public Task InitializeAsync()
    {
        return _bootstrap.InitializeAsync();
    }

    /// <summary>
    ///     创建业务层使用的只读配置入口。
    /// </summary>
    /// <returns>封装强类型表访问的读取入口。</returns>
    public GameConfigRuntime CreateRuntime()
    {
        return new GameConfigRuntime(_bootstrap.Registry);
    }

    /// <summary>
    ///     释放底层热重载句柄等资源。
    /// </summary>
    public void Dispose()
    {
        _bootstrap.Dispose();
    }
}
```

这个官方帮助器刻意遵循几个约定：

- 优先通过 `ConfigureLoader` 调用生成器产出的 `RegisterAllGeneratedConfigTables()`，把多表注册收敛为一个稳定入口
- 由 `GameConfigBootstrap` 持有 `ConfigRegistry`、`YamlConfigLoader` 和热重载句柄
- `InitializeAsync()` 只在首次加载完整成功后才公开运行时状态，避免半初始化对象泄漏到业务层
- 热重载既可以在初始化时自动启用，也可以在初次加载后显式调用 `StartHotReload(...)`

### Godot 文本配置桥接

如果你的项目运行在 Godot，并且 YAML / schema 文本来自 `res://` 下的原始资源文件，推荐优先使用
`GFramework.Godot.Config.GodotYamlConfigLoader`，而不是在项目侧手写一层
“`res://` 遍历 + `user://` 缓存 + `YamlConfigLoader`”桥接代码。

原因很简单：

- `YamlConfigLoader` 需要普通文件系统根目录
- Godot 编辑器内的 `res://` 可以全局化到项目目录
- Godot 导出后若仍读取原始文本资产，通常需要先把显式声明的 YAML / schema 文件同步到运行时缓存目录

`GodotYamlConfigLoader` 会按环境自动处理这两条路径：

- 编辑器态：直接把 `ProjectSettings.GlobalizePath("res://...")` 交给底层 `YamlConfigLoader`
- 导出态：会将当前注册会访问到的 YAML 配置目录与 schema 文件同步到 `user://` 缓存，再交给底层 `YamlConfigLoader`

推荐搭配生成器元数据一起使用，这样项目不需要再自己维护一份重复的配置目录清单：

```csharp
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;
using GFramework.Godot.Config;

var registrationOptions = new GeneratedConfigRegistrationOptions
{
    IncludedConfigDomains = new[] { "gameplay", "ui" }
};

var tableSources = GeneratedConfigCatalog
    .GetTablesForRegistration(registrationOptions)
    .Select(static metadata => new GodotYamlConfigTableSource(
        metadata.TableName,
        metadata.ConfigRelativePath,
        metadata.SchemaRelativePath))
    .ToArray();

var loader = new GodotYamlConfigLoader(
    new GodotYamlConfigLoaderOptions
    {
        SourceRootPath = "res://",
        RuntimeCacheRootPath = "user://config_cache",
        TableSources = tableSources,
        ConfigureLoader = yamlLoader => yamlLoader.RegisterAllGeneratedConfigTables(registrationOptions)
    });

var registry = new ConfigRegistry();
await loader.LoadAsync(registry);
```

使用这条路径时，还需要注意两点：

- 导出预设必须显式包含 `.yaml`、`.yml`、`.json`、`.schema.json` 等原始文本资产；否则导出包里根本没有这些文件，任何加载器都无法读取
- 只有当源根目录可直接映射到普通文件系统目录时，`EnableHotReload(...)` 才可用；如果当前实例依赖 `user://`
  缓存，热重载会被拒绝，而不是制造“监听了缓存目录却不反映真实源目录”的假象
- 如果你通过 `GodotYamlConfigLoader.Loader` 继续追加表注册，请只把它当作“注册入口”使用；实际加载和热重载必须继续调用
  `GodotYamlConfigLoader.LoadAsync(...)` 与 `GodotYamlConfigLoader.EnableHotReload(...)`

### 运行时读取模板

推荐不要在业务代码里直接散落字符串表名查询，而是统一依赖生成的强类型入口：

```csharp
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config.Generated;

namespace GameProject.Config;

/// <summary>
///     封装游戏内容配置读取入口。
/// </summary>
public sealed class GameConfigRuntime
{
    private readonly IConfigRegistry _registry;

    /// <summary>
    ///     使用已初始化的配置注册表创建读取入口。
    /// </summary>
    /// <param name="registry">配置注册表。</param>
    public GameConfigRuntime(IConfigRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    ///     获取指定怪物配置。
    /// </summary>
    /// <param name="monsterId">怪物主键。</param>
    /// <returns>强类型怪物配置。</returns>
    public MonsterConfig GetMonster(int monsterId)
    {
        return _registry.GetMonsterTable().Get(monsterId);
    }

    /// <summary>
    ///     获取怪物配置表。
    /// </summary>
    /// <returns>生成的强类型表包装。</returns>
    public MonsterTable GetMonsterTable()
    {
        return _registry.GetMonsterTable();
    }
}
```

它通常与上面的 `GameConfigHost` 配合使用：

```csharp
var configHost = new GameConfigHost("config-root");
await configHost.InitializeAsync();

var runtime = configHost.CreateRuntime();
var slime = runtime.GetMonster(1);
```

这样做的收益：

- 配置系统对业务层暴露的是强类型表，而不是 `"monster"` 这类 magic string
- 后续如果你要复用配置域、schema 路径或引用元数据，可以继续依赖 `MonsterConfigBindings.Metadata` 和
  `MonsterConfigBindings.References`
- 如果未来把配置初始化接入 `Architecture` 或 `Module`，迁移成本也更低

### 生成查询辅助

从当前阶段开始，生成的 `*Table` 包装会为“顶层、非主键、非引用的标量字段”额外产出轻量查询辅助。

如果某个字段属于高频精确匹配条件，可以在 schema 中显式声明：

```json
{
  "type": "string",
  "x-gframework-index": true
}
```

当前这个元数据只支持“顶层、必填、非主键、非引用标量字段”。命中该条件时，生成的 `FindBy*` /
`TryFindFirstBy*` API 不会变，但内部会改成按需构建只读精确匹配索引；没有声明的字段仍保持线性扫描。

如果 `monster.schema.json` 包含顶层标量字段 `name`、`faction`，则可以直接这样使用：

```csharp
var monsterTable = registry.GetMonsterTable();

var slime = monsterTable.FindByName("Slime");

if (monsterTable.TryFindFirstByFaction("dungeon", out var firstDungeonMonster))
{
    Console.WriteLine(firstDungeonMonster.Name);
}
```

当前生成规则刻意保持保守：

- 只为顶层标量字段生成 `FindBy*` 与 `TryFindFirstBy*`
- 主键字段继续只走 `Get / TryGet`
- 嵌套对象、对象数组、标量数组和 `x-gframework-ref-table` 字段暂不生成查询辅助
- 只有显式声明 `x-gframework-index: true` 的字段才会生成惰性只读索引
- 未声明索引的字段继续基于 `All()` 做线性扫描，不引入额外运行时索引成本

这意味着它的定位是“减少业务层手写过滤样板”，而不是“替代专门索引结构”。

如果你依赖 `TryFindFirstBy*`，应当把它理解为“返回当前表快照遍历顺序下的第一个匹配项”，而不是固定排序语义。

### Architecture 推荐接入模板

如果你的项目已经基于 `GFramework.Core.Architectures.Architecture` 组织初始化流程，推荐优先使用 `GameConfigModule`，而不是在 `OnInitialize()` 里手动拼装 `GameConfigBootstrap` 的注册、加载和销毁顺序：

```csharp
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

public sealed class GameArchitecture : Architecture
{
    private readonly GameConfigModule _configModule;

    public GameArchitecture(string configRootPath)
    {
        _configModule = new GameConfigModule(
            new GameConfigBootstrapOptions
            {
                RootPath = configRootPath,
                ConfigureLoader = static loader => loader.RegisterAllGeneratedConfigTables()
            });
    }

    protected override void OnInitialize()
    {
        InstallModule(_configModule);
    }
}
```

这个模板里的 `.GetAwaiter().GetResult()` 只是“同步 `OnInitialize()` 到异步配置加载”的桥接写法，不应被理解为无条件推荐：

- 如果宿主已经提供异步组合根、启动器或更早的异步初始化阶段，优先在那里直接 `await _configBootstrap.InitializeAsync()`
- 只有在 `Architecture` 只暴露同步 `OnInitialize()`，且当前线程不存在需要恢复的 `SynchronizationContext` 时，才适合使用这类同步桥接
- 在 UI 线程、ASP.NET Classic 等存在活动 `SynchronizationContext` 的环境中，不要直接阻塞等待异步初始化；应把配置初始化前移到异步入口，或改为由不受该上下文约束的启动线程完成

初始化完成后，业务组件可以继续通过架构上下文读取 utility，再走生成的强类型入口：

```csharp
var registry = Context.GetUtility<IConfigRegistry>();
var monsterTable = registry.GetMonsterTable();
var slime = monsterTable.Get(1);
```

推荐遵循以下顺序：

- 先构造 `GameConfigModule`
- 在 `OnInitialize()` 的较早位置调用 `InstallModule(_configModule)`
- 让模块在 `BeforeUtilityInit` 阶段完成首次加载
- 架构销毁时让模块跟随 utility 生命周期自动释放 `GameConfigBootstrap`
- 初始化完成后只通过注册表和生成表包装访问配置

这样做的收益是：

- `IConfigRegistry` 会在模块安装时立即注册为 utility，后续组件统一从上下文读取
- 首次加载发生在 `BeforeUtilityInit`，因此依赖配置的 utility、model 和 system 在自己的初始化阶段就能直接读取表
- 架构销毁时不再需要手写 `Dispose()` 样板来停止热重载句柄

如果你仍然需要在架构外直接控制 `InitializeAsync()`、`StartHotReload(...)` 或 `StopHotReload()` 的调用时机，继续直接使用 `GameConfigBootstrap` 仍然是合适的；`GameConfigModule` 是面向 `Architecture` 宿主的官方薄封装，而不是替代底层 bootstrap。

### 热重载模板

如果你希望把开发期热重载显式收敛为一个可选能力，在 `Architecture` 场景下可以直接保留上面示例中的 `_configModule` 字段并调用 `GameConfigModule.StartHotReload(...)`；非 `Architecture` 场景则继续直接通过 `GameConfigBootstrap.StartHotReload(...)` 管理，而不是让监听句柄散落在启动层之外：

```csharp
await architecture.InitializeAsync();

_configModule.StartHotReload(
    new YamlConfigHotReloadOptions
    {
        OnTableReloaded = tableName => Console.WriteLine($"Reloaded: {tableName}"),
        OnTableReloadFailed = (tableName, exception) =>
        {
            var diagnostic = (exception as ConfigLoadException)?.Diagnostic;
            Console.WriteLine($"Reload failed: {tableName}");
            Console.WriteLine($"Failure kind: {diagnostic?.FailureKind}");
            Console.WriteLine($"Yaml path: {diagnostic?.YamlPath}");
            Console.WriteLine($"Display path: {diagnostic?.DisplayPath}");
        }
    });
```

建议只在开发期启用这项能力：

- 生产环境默认更适合静态加载和固定生命周期
- 热重载失败时应优先依赖 `ConfigLoadException.Diagnostic` 做稳定日志或 UI 提示
- 如果你的项目已经有统一日志系统，建议在这里把诊断字段转成结构化日志，而不是拼接一整段字符串

如果你确实需要直接控制底层加载器，`YamlConfigLoader.EnableHotReload(...)` 仍然保留；但在一般启动路径下，优先让 `GameConfigBootstrap` 持有并停止监听句柄。

## 运行时接入

当你希望加载后的配置在运行时以只读表形式暴露时，优先使用生成器产出的注册与访问辅助：

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var registry = new ConfigRegistry();

var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables();

await loader.LoadAsync(registry);

var monsterTable = registry.GetMonsterTable();
var slime = monsterTable.Get(1);
```

这里推荐把“注册全部已生成配置表”和“读取单表强类型元数据”分成两层：

- 启动层优先走 `RegisterAllGeneratedConfigTables()`，避免每新增一个 schema 都要回到启动代码继续补链式调用
- 消费层继续通过 `GetMonsterTable()`、`MonsterConfigBindings.Metadata` 这类单表入口读取强类型信息

这组辅助会把以下约定固化到生成代码里：

- 配置域常量，例如 `MonsterConfigBindings.ConfigDomain`
- 表注册名，例如 `monster`
- 配置目录相对路径，例如 `monster`
- schema 相对路径，例如 `schemas/monster.schema.json`
- 主键提取逻辑，例如 `config => config.Id`

如果你希望把这些约定作为一个统一入口传递或复用，也可以优先读取 `MonsterConfigBindings.Metadata` 下的常量：

```csharp
var domain = MonsterConfigBindings.Metadata.ConfigDomain;
var tableName = MonsterConfigBindings.Metadata.TableName;
var configPath = MonsterConfigBindings.Metadata.ConfigRelativePath;
var schemaPath = MonsterConfigBindings.Metadata.SchemaRelativePath;
```

如果你需要在启动或诊断代码里枚举当前消费者项目里有哪些生成表，也可以直接读取项目级目录：

```csharp
foreach (var metadata in GeneratedConfigCatalog.Tables)
{
    Console.WriteLine($"{metadata.TableName} -> {metadata.SchemaRelativePath}");
}
```

也可以按表名回查：

```csharp
if (GeneratedConfigCatalog.TryGetByTableName("monster", out var metadata))
{
    Console.WriteLine(metadata.ConfigRelativePath);
}
```

如果你希望先按配置域聚合出一组候选表，再决定是否进入启动链路，也可以直接查询目录：

```csharp
foreach (var metadata in GeneratedConfigCatalog.GetTablesInConfigDomain(MonsterConfigBindings.ConfigDomain))
{
    Console.WriteLine(metadata.TableName);
}
```

如果你需要为某些表保留自定义 key comparer，也可以继续走聚合注册入口，而不是被迫退回逐表手写：

```csharp
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables(
        new GeneratedConfigRegistrationOptions
        {
            ItemComparer = StringComparer.OrdinalIgnoreCase
        });
```

如果项目已经生成了多张表，但当前场景只想注册其中一部分，也可以直接在聚合入口上加筛选，而不必退回手写逐表注册：

```csharp
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables(
        new GeneratedConfigRegistrationOptions
        {
            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
        });
```

如果你更习惯按表名白名单或自定义谓词裁剪启动集，也可以继续在同一个 options 对象里完成：

```csharp
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables(
        new GeneratedConfigRegistrationOptions
        {
            IncludedTableNames = new[] { MonsterConfigBindings.TableName, ItemConfigBindings.TableName },
            TableFilter = static metadata => metadata.SchemaRelativePath.EndsWith(".schema.json", StringComparison.Ordinal)
        });
```

如果你想在真正调用 `RegisterAllGeneratedConfigTables(...)` 之前，先把“这次会注册哪些表”输出到日志中，推荐直接复用同一份 options 做启动诊断，而不是手写一套平行筛选逻辑：

```csharp
var registrationOptions = new GeneratedConfigRegistrationOptions
{
    IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
};

foreach (var metadata in GeneratedConfigCatalog.GetTablesForRegistration(registrationOptions))
{
    Console.WriteLine($"Registering {metadata.TableName}");
}

var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables(registrationOptions);
```

这里的规则是：

- `IncludedConfigDomains` 与 `IncludedTableNames` 都按 `StringComparison.Ordinal` 做白名单匹配；传 `null` 或空集合表示“不限制”
- `TableFilter` 会在上述白名单通过后执行，适合继续按 schema 路径、配置目录等元数据做更细粒度的启动裁剪
- `GeneratedConfigCatalog.GetTablesForRegistration(...)` 与 `RegisterAllGeneratedConfigTables(...)` 复用同一套筛选规则，便于在启动日志和真实注册之间保持一致
- 未显式配置 comparer 的表，仍然使用各自 `Register{Entity}Table()` 的默认行为
- 需要自定义 comparer 的表，可以通过 `GeneratedConfigRegistrationOptions` 按表覆盖
- 当前 `ConfigDomain` 约定仍与生成表名保持一致，但建议优先引用 `*ConfigBindings.ConfigDomain`，为后续更细的分组策略保留稳定入口
- 如果项目希望继续完全手写某张表的注册流程，逐表 `Register*Table(...)` 入口仍然保留，作为兼容逃生通道

如果你需要自定义目录、表名或 key selector，仍然可以直接调用 `YamlConfigLoader.RegisterTable(...)` 原始重载。

如果你希望把 schema 路径、比较器以及未来扩展开关集中到一个对象里，推荐改用选项对象入口：

```csharp
var loader = new YamlConfigLoader("config-root")
    .RegisterTable(
        new YamlConfigTableRegistrationOptions<int, MonsterConfig>(
            "monster",
            "monster",
            static config => config.Id)
        {
            SchemaRelativePath = "schemas/monster.schema.json"
        });
```

## 运行时校验行为

绑定 schema 的表在加载时会拒绝以下问题：

- 缺失必填字段
- 未在 schema 中声明的未知字段
- 标量类型不匹配
- 数组元素类型不匹配
- 嵌套对象字段类型不匹配
- 对象数组元素结构不匹配
- 数值字段违反 `minimum` / `maximum`
- 数值字段违反 `exclusiveMinimum` / `exclusiveMaximum`
- 数值字段违反 `multipleOf`
- 字符串字段违反 `minLength` / `maxLength`
- 字符串字段违反 `pattern`
- 字符串字段违反 `format`
- 数组字段违反 `minItems` / `maxItems`
- 数组字段违反 `uniqueItems`
- 数组字段违反 `contains` / `minContains` / `maxContains`
- 对象字段违反 `minProperties` / `maxProperties`
- 对象字段违反 `dependentRequired`
- 对象字段违反 `dependentSchemas`
- 对象字段违反 `allOf`
- 标量 / 对象 / 数组字段违反 `const`
- 标量 / 对象 / 数组字段命中 `not`
- 标量 / 对象 / 数组字段违反 `enum`
- 标量数组元素 `enum` 不匹配
- 通过 `x-gframework-ref-table` 声明的跨表引用缺失目标行

跨表引用当前使用最小扩展关键字：

```json
{
  "type": "object",
  "required": ["id", "dropItemId"],
  "properties": {
    "id": { "type": "integer" },
    "dropItemId": {
      "type": "string",
      "x-gframework-ref-table": "item"
    }
  }
}
```

约束如下：

- 仅支持 `string`、`integer` 及其标量数组声明跨表引用
- 引用目标表需要由同一个 `YamlConfigLoader` 注册，或已存在于当前 `IConfigRegistry`
- 热重载中若目标表变更导致依赖表引用失效，会整体回滚受影响表，避免注册表进入不一致状态

如果你希望在消费者代码里复用这些跨表约定，而不是继续手写字段路径或目标表名，生成的 `*ConfigBindings` 还会暴露引用元数据：

```csharp
var allReferences = MonsterConfigBindings.References.All;

if (MonsterConfigBindings.References.TryGetByDisplayPath("dropItems", out var reference))
{
    Console.WriteLine(reference.ReferencedTableName);
    Console.WriteLine(reference.ValueSchemaType);
    Console.WriteLine(reference.IsCollection);
}
```

当 schema 中存在具体引用字段时，还可以直接通过生成成员访问，例如 `MonsterConfigBindings.References.DropItems`。

当前还支持以下“轻量元数据”：

- `title`：供 VS Code 插件表单和批量编辑入口显示更友好的字段标题
- `description`：供表单提示、生成代码 XML 文档和接入说明复用
- `default`：供生成类型属性初始值和工具提示复用
- `const`：供运行时校验、VS Code 校验、表单 hint 和生成代码 XML 文档复用；对象会忽略字段顺序比较，数组保留元素顺序，标量按运行时同一套类型归一化规则比较
- `not`：供运行时校验、VS Code 校验和生成代码 XML 文档复用；`not` 子 schema 会复用同一套递归校验规则，但对象匹配保持主校验链的严格语义，不会像 `contains` 那样把“声明属性子集”视为命中
- `enum`：供运行时校验、VS Code 校验和生成代码 XML 文档复用；当前标量、对象和数组节点都支持 `enum`，其中标量 `enum` 继续驱动表单枚举选择，对象 / 数组 `enum` 当前主要提供校验与文档约束
- `minimum` / `maximum`：供运行时校验、VS Code 校验和生成代码 XML 文档复用
- `exclusiveMinimum` / `exclusiveMaximum`：供运行时校验、VS Code 校验和生成代码 XML 文档复用
- `multipleOf`：供运行时校验、VS Code 校验、表单 hint 和生成代码 XML 文档复用；当前优先按运行时与 JS 共用的十进制精确整倍数判定处理常见十进制步进，并在必要时退回浮点容差兜底
- `minLength` / `maxLength`：供运行时校验、VS Code 校验和生成代码 XML 文档复用
- `pattern`：供运行时校验、VS Code 校验、表单提示和生成代码 XML 文档复用；当前按 C# `CultureInvariant` 与 JS Unicode `u` 模式解释，非法模式会在 schema 解析阶段直接报错
- `format`：当前只支持 Runtime / Generator / Tooling 三端都能稳定对齐的字符串子集 `date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`
- `duration`：当前只支持稳定的 day-time duration 子集，例如 `P2D`、`PT45M`、`P2DT3H4M5.5S`；为了避免跨宿主对日历语义解释漂移，暂不支持 `Y` / `M（月）` / `W`
- `time`：固定要求显式时区偏移（例如 `08:30:00Z` 或 `08:30:00+08:00`），避免不同宿主对 time-only 文本隐式补日期或本地时区
- 对上述共享子集，运行时会拒绝不满足格式的值，VS Code 校验与表单 hint 会同步展示该约束，生成代码 XML 文档也会保留 `format = ...` 说明
- `minItems` / `maxItems`：供运行时校验、VS Code 校验、表单提示和生成代码 XML 文档复用
- `uniqueItems`：供运行时校验、VS Code 校验、表单 hint 和生成代码 XML 文档复用；对象数组会按 schema 归一化后的结构比较重复项，而不是依赖 YAML 字段顺序
- `contains` / `minContains` / `maxContains`：供运行时校验、VS Code 校验、表单 hint 和生成代码 XML 文档复用；当前会按同一套递归 schema 规则统计“有多少数组元素匹配 contains 子 schema”，其中仅声明 `contains` 时默认至少需要 1 个匹配元素
- `minProperties` / `maxProperties`：供运行时校验、VS Code 校验、对象 section 表单 hint 和生成代码 XML 文档复用；根对象与嵌套对象都会按实际属性数量执行同一套约束
- `dependentRequired`：供运行时校验、VS Code 校验、对象 section 表单 hint 和生成代码 XML 文档复用；当前只表达“当对象内某个字段出现时，还必须同时声明哪些同级字段”，不会改变生成类型形状
- `dependentSchemas`：供运行时校验、VS Code 校验、对象 section 表单 hint 和生成代码 XML 文档复用；当前只接受“已声明 sibling 字段触发 object 子 schema”的形状，不改变生成类型形状，并按 focused constraint block 语义允许条件子 schema 未声明的额外同级字段继续存在
- `allOf`：供运行时校验、VS Code 校验、对象 section 表单 hint 和生成代码 XML 文档复用；当前只接受 object 节点上的 object-typed inline schema 数组，并按 focused constraint block 语义把每个条目叠加到当前对象上，不做属性合并，也不改变生成类型形状

`allOf` 的最小可工作示例如下。关键点是：字段形状先在父对象 `properties` 中声明，再用 `allOf` 叠加 `required` 或更细的字段约束；`allOf` 条目不会把新字段并回父对象。

```json
{
  "type": "object",
  "properties": {
    "reward": {
      "type": "object",
      "properties": {
        "itemId": { "type": "string" },
        "itemCount": { "type": "integer" }
      },
      "allOf": [
        {
          "type": "object",
          "required": ["itemCount"]
        },
        {
          "type": "object",
          "properties": {
            "itemCount": {
              "type": "integer",
              "minimum": 2
            }
          }
        }
      ]
    }
  }
}
```

```yaml
reward:
  itemId: potion
  itemCount: 3
```

兼容性说明：如果你以前按标准 JSON Schema `allOf` 的直觉，把新字段只写进 `allOf` 条目的 `properties` 或 `required`，当前实现不会做属性合并，这类 schema 现在会在加载 / 生成 / 工具解析阶段直接被拒绝。请先把字段提升到父对象的 `properties`，再在 `allOf` 里补充 required 或约束。

这样可以避免错误配置被默认值或 `IgnoreUnmatchedProperties` 静默吞掉。

加载失败时，`YamlConfigLoader` 会抛出 `ConfigLoadException`。你可以通过 `exception.Diagnostic` 读取稳定字段，而不必解析消息文本：

```csharp
using GFramework.Game.Abstractions.Config;

try
{
    await loader.LoadAsync(registry);
}
catch (ConfigLoadException exception)
{
    Console.WriteLine(exception.Diagnostic.FailureKind);
    Console.WriteLine(exception.Diagnostic.TableName);
    Console.WriteLine(exception.Diagnostic.YamlPath);
    Console.WriteLine(exception.Diagnostic.SchemaPath);
    Console.WriteLine(exception.Diagnostic.DisplayPath);
}
```

当前诊断对象会优先暴露这些字段：

- `FailureKind`
- `TableName`
- `YamlPath`
- `SchemaPath`
- `DisplayPath`
- `ReferencedTableName`
- `RawValue`

## 开发期热重载

如果你希望在开发期修改配置文件后自动刷新运行时表，可以在初次加载完成后启用热重载：

```csharp
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var registry = new ConfigRegistry();
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables();

await loader.LoadAsync(registry);

var hotReload = loader.EnableHotReload(
    registry,
    onTableReloaded: tableName => Console.WriteLine($"Reloaded: {tableName}"),
    onTableReloadFailed: (tableName, exception) =>
    {
        var diagnostic = (exception as ConfigLoadException)?.Diagnostic;
        Console.WriteLine($"Reload failed: {tableName}, {exception.Message}");
        Console.WriteLine($"Failure kind: {diagnostic?.FailureKind}");
    });
```

当前热重载行为如下：

- 监听已注册表对应的配置目录
- 监听该表绑定的 schema 文件
- 检测到变更后按表粒度重载
- 若变更表被其他表通过跨表引用依赖，会联动重验受影响表
- 重载成功后替换该表在 `IConfigRegistry` 中的注册
- 重载失败时保留旧表，并通过失败回调提供诊断

这项能力默认定位为开发期工具，不承诺生产环境热更新平台语义。

## 生成器接入约定

配置生成器会从 `*.schema.json` 生成以下代码：

- 配置类型
- 表包装类型
- `YamlConfigLoader` 注册辅助
- `IConfigRegistry` 强类型访问辅助

通过已打包的 Source Generator 使用时，默认会自动收集 `schemas/**/*.schema.json` 作为 `AdditionalFiles`。

如果你在仓库内直接使用项目引用而不是打包后的 NuGet，请确认 schema 文件同样被加入 `AdditionalFiles`。

## VS Code 工具

仓库中的 `tools/gframework-config-tool` 当前提供以下能力：

- 浏览 `config/` 目录
- 打开 raw YAML 文件
- 打开匹配的 schema 文件
- 根据 VS Code 当前界面语言在英文和简体中文之间切换主要工具界面文本
- 对嵌套对象中的必填字段、未知字段、基础标量类型、标量数组和对象数组元素做轻量校验
- 对嵌套对象字段、对象数组、顶层标量字段和顶层标量数组提供轻量表单入口
- 在表单中渲染已有 YAML 注释，并允许直接编辑字段级 YAML 注释
- 对带 `x-gframework-ref-table` 的字段提供引用 schema / 配置域 / 引用文件跳转入口
- 对空配置文件提供基于 schema 的示例 YAML 初始化入口
- 对同一配置域内的多份 YAML 文件执行批量字段更新
- 在表单入口中显示 `title / description / default / const / enum / x-gframework-ref-table（UI 中显示为 ref-table） / multipleOf / pattern / format / uniqueItems / contains / minContains / maxContains / minProperties / maxProperties / dependentRequired / dependentSchemas / allOf` 元数据；批量编辑入口当前只暴露顶层可批量改写字段所需的基础信息

当前表单入口适合编辑嵌套对象中的标量字段、标量数组，以及对象数组中的对象项。

对象数组编辑器当前支持：

- 新增和删除对象项
- 编辑对象项中的标量字段
- 编辑对象项中的标量数组
- 编辑对象项中的嵌套对象字段

如果对象数组项内部继续包含对象数组，当前仍建议回退到 raw YAML 完成。

当前批量编辑入口仍刻意限制在“同域文件统一改动顶层标量字段和顶层标量数组”，避免复杂结构批量写回时破坏人工维护的 YAML 排版。

## 当前限制

以下能力尚未完全完成：

- 更完整的 JSON Schema 支持
- VS Code 中更深层对象数组嵌套的安全表单编辑器
- 更强的复杂数组与更深 schema 关键字支持

因此，现阶段更适合作为你游戏项目的“受控试点配表系统”，而不是完全无约束的大规模内容生产平台。

## 独立 Config Studio 评估

当前阶段的结论是：`不建议立即启动独立 Config Studio`，继续以 `VS Code Extension` 作为主工具形态更合适。

当前不单独启动桌面版的原因：

- 当前已落地的能力主要仍围绕 schema 校验、轻量表单、批量编辑和 raw YAML 回退，这些都能在 VS Code 宿主里低成本迭代
- runtime、generator、tooling 之间仍在持续收敛 schema 子集和元数据语义，过早拆出桌面工具会放大版本协同成本
- 当前待补强点仍是更完整 schema 支持和复杂编辑体验，先在插件里验证真实工作流更稳妥
- 仓库当前的主要使用者仍偏开发者和技术策划，独立桌面版带来的“免开发环境”收益还不足以抵消额外维护面

只有在以下条件明显成立时，再建议启动独立 `Config Studio`：

- 主要使用者变成非开发人员，且 VS Code 安装与使用成本成为持续阻力
- 需要更重的表格视图、跨表可视化关系编辑、复杂审批流或离线发布流程
- 插件形态已经频繁受限于 VS Code Webview/Extension API，而不是 schema 与工作流本身
- 已经沉淀出稳定的 schema 元数据约定，能够支撑单独桌面产品的长期维护
